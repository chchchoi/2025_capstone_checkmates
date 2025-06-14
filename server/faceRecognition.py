from flask import Flask, request, jsonify, render_template
import numpy as np
import cv2
import os
import re  # (파일 상단에 한 번만 추가하면 됨)
import firebase_admin
from firebase_admin import credentials, firestore, storage
from insightface.app import FaceAnalysis
from datetime import datetime
import json
import time

# Firebase 초기화
cred = credentials.Certificate("")
firebase_admin.initialize_app(cred, {
    ''
})

# Firebase Firestore와 Storage 클라이언트 초기화
db = firestore.client()
bucket = storage.bucket()

# InsightFace 초기화
app = FaceAnalysis(name='buffalo_l', providers=['CPUExecutionProvider'])
app.prepare(ctx_id=0)

flask_app = Flask(__name__)

# 측정 데이터 저장용 JSON
METRICS_FILE = "metrics.json"

def load_metrics():
    try:
        with open(METRICS_FILE, 'r') as f:
            data = json.load(f)
    except FileNotFoundError:
        data = {}

    # 누락된 키 보완
    data.setdefault("register_times", [])
    data.setdefault("recognition_times", [])
    data.setdefault("recognition_results", [])

    return data

def save_metrics(data):
    with open(METRICS_FILE, 'w') as f:
        json.dump(data, f, indent=4)

def cosine_similarity(vec1, vec2):
    if vec1 is None or vec2 is None:
        return 0.0
    return float(np.dot(vec1, vec2) / (np.linalg.norm(vec1) * np.linalg.norm(vec2)))

def get_embedding(image):
    faces = app.get(image)
    if faces:
        return faces[0].embedding
    return None

@flask_app.route('/register', methods=['POST'])
def register_face():
    start_time = time.time()
    print("==== POST /register 요청 도착 ====")

    email = request.form.get("email")
    image_file = request.files.get("image")

    if not email or not image_file:
        return jsonify({"error": "이메일과 이미지가 필요합니다."}), 400

    image_bytes = image_file.read()
    img = cv2.imdecode(np.frombuffer(image_bytes, np.uint8), cv2.IMREAD_COLOR)

    if img is None:
        return jsonify({"error": "이미지 디코딩 실패"}), 400

    # 이미지 개수 세기: storage에 같은 이메일로 저장된 개수 파악
    existing_blobs = list(bucket.list_blobs(prefix=f"faces/{email}_"))
    image_count = len([b for b in existing_blobs if b.name.endswith(".jpg")])
    new_index = image_count + 1

    filename = f"{email}_{new_index}.jpg"
    blob_path = f"faces/{filename}"
    blob = bucket.blob(blob_path)
    blob.upload_from_string(image_bytes, content_type="image/jpeg")
    blob.make_public()

    print(f"[INFO] 저장 완료: {blob.public_url}")

    # Firestore에 image_urls 리스트로 누적 저장
    info_ref = db.collection("users").document("personal").collection(email).document("Info")
    existing_doc = info_ref.get()

    if existing_doc.exists:
        existing_data = existing_doc.to_dict()
        image_urls = existing_data.get("image_urls", [])
    else:
        image_urls = []

    image_urls.append(blob.public_url)

    info_ref.set({
        "email": email,
        "image_urls": image_urls,
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    })

    elapsed = int((time.time() - start_time) * 1000)
    metrics = load_metrics()
    metrics["register_times"].append(elapsed)
    save_metrics(metrics)

    return jsonify({
        "message": f"{email} 등록 완료 (총 {new_index}장)",
        "image_url": blob.public_url,
        "register_time_ms": elapsed
    }), 200


# 출석 확인 API
@flask_app.route('/check', methods=['POST'])
def check_attendance():
    start_time = time.time()
    print("==== POST /check 요청 도착 ====")

    currentSubject = request.form.get("currentSubject")
    currentStatus = request.form.get("currentStatus")
    managerEmail = request.form.get("manager")

    if not currentSubject or not currentStatus or not managerEmail:
        return jsonify({"error": "과목 이름, 상태, 관리자 이메일이 필요합니다."}), 400

    if 'image' not in request.files:
        return jsonify({"error": "이미지가 포함되어야 합니다."}), 400

    file = request.files['image']
    image_bytes = file.read()
    np_arr = np.frombuffer(image_bytes, np.uint8)

    try:
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    except Exception as e:
        print(f"[ERROR] 이미지 디코딩 실패: {str(e)}")
        return jsonify({"error": "이미지 디코딩 실패"}), 400

    if img is None:
        print("[ERROR] 이미지 디코딩 결과 None")
        return jsonify({"error": "이미지를 열 수 없습니다."}), 400

    input_embedding = get_embedding(img)
    if input_embedding is None:
        print("[ERROR] 얼굴을 감지하지 못했습니다.")
        return jsonify({"error": "얼굴을 감지하지 못했습니다."}), 400

    max_similarity = 0.0
    matched_email = None

    blobs = bucket.list_blobs(prefix="faces/")
    for blob in blobs:
        if not blob.name.lower().endswith(('.jpg', '.jpeg', '.png')):
            continue

        ref_bytes = blob.download_as_bytes()
        ref_np = np.frombuffer(ref_bytes, np.uint8)
        ref_img = cv2.imdecode(ref_np, cv2.IMREAD_COLOR)
        ref_embedding = get_embedding(ref_img)

        similarity = cosine_similarity(input_embedding, ref_embedding)
        print(f"[DEBUG] 비교 대상: {blob.name}, 유사도: {similarity:.4f}")

        if similarity > max_similarity and similarity > 0.3:
            max_similarity = similarity
            
            filename_base = os.path.basename(blob.name).rsplit('.', 1)[0]
            matched_email = re.sub(r'_\d+$', '', filename_base)  # ✅ "_숫자" 제거

    if matched_email:
        now = datetime.now()
        today_str = now.strftime("%Y-%m-%d")

        subject_query = db.collection("subjects")\
            .where("name", "==", currentSubject)\
            .where("manager", "==", managerEmail)\
            .get()
        subject_docs = list(subject_query)

        if not subject_docs:
            return jsonify({"error": f"과목 '{currentSubject}'을(를) 찾을 수 없습니다."}), 404

        subject_doc = subject_docs[0]
        subject_ref = subject_doc.reference
        subject_data = subject_doc.to_dict()

        personal_list = subject_data.get("personal", [])
        if matched_email not in personal_list:
            print(f"[INFO] {matched_email}은(는) '{currentSubject}'에 등록되지 않은 사용자입니다.")
            return jsonify({"error": "등록되지 않은 사용자입니다."}), 403

        attendance_doc = subject_ref.collection(matched_email).document(today_str)
        attendance_data = attendance_doc.get()

        if attendance_data.exists:
            return jsonify({"message": "오늘 이미 출석 처리된 사용자입니다."}), 200

        attendance_doc.set({
            "timestamp": now.strftime("%Y-%m-%d %H:%M:%S"),
            "status": currentStatus
        })

        try:
            person_query = db.collection("people").where("email", "==", matched_email).get()
            person_docs = list(person_query)
            if person_docs:
                user_name = person_docs[0].to_dict().get("name", "사용자")
            else:
                print(f"[INFO] Firestore에서 '{matched_email}' 사용자 문서 없음")
                user_name = "사용자"
        except Exception as e:
            print(f"[ERROR] 이름 조회 중 오류: {e}")
            user_name = "사용자"

        status_message = {
            "출석": f"{user_name}님, 출석 처리되었습니다.",
            "지각": f"{user_name}님, 지각 처리되었습니다.",
            "결석": f"{user_name}님, 결석 처리되었습니다."
        }.get(currentStatus, f"{user_name}님, 출석 처리되었습니다.")

        print(f"[INFO] {matched_email} 출석 정보 업로드 완료 (과목: {currentSubject})")

        elapsed = int((time.time() - start_time) * 1000)
        metrics = load_metrics()
        metrics["recognition_times"].append(elapsed)
 
        save_metrics(metrics)

        return jsonify({
            "message": status_message,
            "email": matched_email,
            "status": currentStatus,
            "similarity": max_similarity,
            "recognition_time_ms": elapsed
        }), 200

    else:
        elapsed = int((time.time() - start_time) * 1000)
        metrics = load_metrics()
        metrics["recognition_times"].append(elapsed)
        
        save_metrics(metrics)

        return jsonify({
            "error": "일치하는 얼굴을 찾을 수 없습니다.",
            "recognition_time_ms": elapsed
        }), 404

# 측정 결과 확인용 API
@flask_app.route('/metrics', methods=['GET'])
def get_metrics():
    return jsonify(load_metrics())

# 서버 실행
if __name__ == '__main__':
    flask_app.run(host='0.0.0.0', port=5050, debug=True)
