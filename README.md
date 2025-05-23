# 2025_capstone_checkmates
# [팀명]
### 듀가나디
* 팀원
  * 문지선 (팀장)
  * 남유진
  * 정서인
  * 최하정
    
# [목적]
### __Checkmates는 얼굴 인식을 기반으로 출석을 관리할 수 있는 어플리케이션입니다.__
✔️ 사용자의 얼굴 이미지를 등록할 수 있습니다. <br>
✔️ 출석 시 실시간으로 이미지를 찍어 등록된 얼굴 이미지와 비교해 출석 관리가 가능합니다. <br>
✔️ 원하는 과목을 수정 및 삭제 등의 관리를 자유롭게 할 수 있습니다. <br>
✔️ 사용자는 언제 어디서든 출결 확인이 가능합니다. <br>
✔️ 관리자는 자신의 수업 및 매장에 사용자를 추가 및 삭제하거나, 사용자의 출결 현황을 변경할 수 있습니다.<br>
# [배경]
수기 출석 방식은 번거롭고, 대리 출석과 같은 신뢰성 문제가 발생하기 쉽습니다. 특히 학교나 학원 등 교육기관에서는 출석 관리의 정확성과 효율성이 중요해지고 있습니다. 이러한 흐름에 맞춰 Checkmates는 얼굴 인식을 활용해 출석 과정을 자동화함으로써 사용자 편의성을 높이고, 출석의 신뢰성과 관리 효율성을 동시에 향상시키고자 개발되었습니다. <br>
# [시스템 구성]
이 시스템은 Unity, Firebase, Flask 서버(OpenCV + InsightFace 포함) 간의 연동을 통해 얼굴 인식 기반 출석 관리를 수행하는 구조입니다.

* Unity (클라이언트 앱)
  * 사용자 인터페이스를 담당하며, 회원가입, 과목 등록/관리, 출결 확인 등의 기능을 제공합니다.
  * 사용자가 촬영한 얼굴 이미지를 Flask 서버로 전송합니다.
  * Flask 서버로부터 얼굴 인식 결과(출석 여부)를 받아 사용자에게 보여줍니다.
  * 사용자의 출결 정보를 Firebase에 전송합니다.

* Flask 서버 (백엔드 + 얼굴 인식)
  * OpenCV와 InsightFace를 통해 전송된 얼굴 이미지의 임베딩을 계산하고, 기존 등록된 이미지와 비교하여 출석 여부 판단을 수행합니다.
  * 판단 결과 및 이미지 데이터를 Firebase에 업로드하거나, Unity로 다시 전달합니다.

* Firebase (클라우드 DB 및 스토리지)
  *  Firestore: 사용자 정보, 과목 정보, 출결 기록 등 구조화된 데이터를 저장합니다.
  *  Storage: 얼굴 이미지 파일을 저장합니다.
  *  Flask와 Unity 양쪽에서 데이터를 주고받으며, 중앙 저장소 역할을 합니다.
    
* 데이터 흐름 요약
  1. Unity → Flask: 얼굴 이미지 전송
  2. Flask → OpenCV/InsightFace: 얼굴 인식 및 유사도 분석
  3. Flask → Firebase: 출석 결과 및 이미지 저장
  4. Unity → Firebase: 사용자 및 과목 정보 등록
  5.	Firebase → Unity: 출결 정보 확인
  
<p align="center">
<img src="https://github.com/user-attachments/assets/5c7ba701-640f-41d2-bb04-e7aa66cedf4a" width="70%">
</p>

# [적용 기술]
### 핵심 기술
* OpenCV: 얼굴 이미지 전처리 및 기본 이미지 처리 기능
* InsightFace: 고성능 얼굴 인식 및 임베딩 비교
* Flask: RESTful API 서버 구축 및 Unity와의 통신 처리
* Firebase Firestore: 사용자, 과목, 출결 정보 저장
* Firebase Storage: 얼굴 이미지 저장 및 관리
* Firebase Authentication: 사용자 로그인/회원가입 관리
  
### 개발 환경
<img src="https://img.shields.io/badge/Windows_11-0078D4?style=for-the-badge&logo=windows11&logoColor=white"/><img src="https://img.shields.io/badge/macOS-000000?style=for-the-badge&logo=apple&logoColor=white"/>

### 개발 도구
<img src="https://img.shields.io/badge/Unity-000000?style=for-the-badge&logo=unity&logoColor=white"/><img src="https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=white"/><img src="https://img.shields.io/badge/Flask-000000?style=for-the-badge&logo=flask&logoColor=white"/><img src="https://img.shields.io/badge/OpenCV-5C3EE8?style=for-the-badge&logo=opencv&logoColor=white"/><img src="https://img.shields.io/badge/Visual%20Studio-5C2D91?style=for-the-badge&logo=visual-studio&logoColor=white"/><img src="https://img.shields.io/badge/Visual%20Studio%20Code-007ACC?style=for-the-badge&logo=Visual-Studio-Code&logoColor=white"/> 

### 개발 언어 및 프레임워크
<img src="https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white"/><img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white"/><img src="https://img.shields.io/badge/InsightFace-1E90FF?style=for-the-badge"/>

# [프로젝트 결과]
### 초기화면
<table>
  <tr>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/7461133f-d22c-490d-a774-13dadf42014d" width="1350px"><br>
      <b>시작 화면
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/c9adcc46-6d7f-4f75-ae28-791d125e9084" width="1250px"><br>
      <b>로그인
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/acc4579b-1d56-47cd-97f3-7282153017e4" width="1240px"><br>
      <b>회원가입
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/4be9cb82-ba91-4073-ae8c-b410d9419497" width="1250px"><br>
      <b>이메일 찾기
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/87970809-c48b-4c9c-a885-01a3a97669b1" width="1250px"><br>
      <b>비밀번호 찾기
    </td>
  </tr>
</table>

### 관리자 화면
<table>
  <tr>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/de6400ba-0a5e-4d4d-a529-1ac0a16f363c" width="1280px"><br>
      <b>관리자 과목 리스트
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/5975caf1-f253-44f0-9268-be58da3fb2fd" width="1310px"><br>
      <b>강의 과목 생성
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/56681a57-9941-476f-a01b-3b33ab0e4091" width="1250px"><br>
      <b>과목별 출석 현황
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/4e9a7357-7dee-4a76-ab43-1d262b2d75d1" width="1250px"><br>
      <b>과목 상태 편집
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/a70961fe-f10a-49f3-b7df-c906d73e906e" width="1330px"><br>
      <b>출석 학생 얼굴 인식
    </td>
  </tr>
</table>

### 개인 화면
<table>
  <tr>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/99550753-70c6-40b5-89f6-280fa7cf47a7" width="1230px"><br>
      <b>사용자 수강 과목 리스트
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/91f3ffa0-dff9-45ff-a1e2-31957d061466" width="1220px"><br>
      <b>수강 과목 추가
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/d2e31d4f-f65b-4272-805b-500e05b7ffb0" width="1210px"><br>
      <b>해당 과목의 출결 조회
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/f437e711-2417-45d3-9736-dc71834c860d" width="1280px"><br>
      <b>사용자 얼굴 등록
    </td>
  </tr>
</table>

# [기대 효과]
* __사용자 측면__ <br>
별도의 수기 입력 없이 하나의 휴대폰만으로 간편하게 출석 체크가 가능합니다. 얼굴 인식을 기반으로 하기 때문에, 대리 출석과 같은 기존 출석 방식의 문제를 방지하므로 보다 정확하고 철저한 출석 관리가 가능합니다. 사용자는 언제 어디서든 자신의 출결 현황을 확인할 수 있어 출석 정보와 접근성과 투명성도 높아집니다. 
* __비즈니스 측면__ <br>
학원, 교육 기관뿐만 아니라 병원, 피트니스 센터, 사무실, 상업 매장 등 다양한 분야에서 고객 관리 및 인원 체크를 자동화하는 데 활용될 수 있어 전반적인 업무 효율성을 크게 높일 수 있습니다.

