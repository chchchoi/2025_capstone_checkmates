using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class addLectureList : MonoBehaviour
{
    public GameObject buttonPrefab;  // 버튼 프리팹
    public Transform contentTransform;  // Scroll View의 Content

    private Image buttonImage;         // 버튼 안 Image 객체 (Inspector에서 연결)
    private TMP_Text nameText;          // 버튼 안 name 텍스트 (Inspector에서 연결)
    private TMP_Text infoText;          // 버튼 안 info 텍스트 (Inspector에서 연결)
    public Sprite[] subjectSprites;    // 랜덤으로 적용할 Sprite 배열 (Inspector에서 3개 끌어오기)

    private FirebaseFirestore db;
    private ListenerRegistration subjectListener;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        string userEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;  // 로그인된 사용자 이메일 가져오기

        if (!string.IsNullOrEmpty(userEmail))
        {
            FetchSubjects(userEmail);  // 이메일이 존재하면 과목 데이터 가져오기
        }
        else
        {
            Debug.LogError("❌ 사용자가 로그인되지 않았습니다.");
        }
    }

    async void FetchSubjects(string userEmail)
    {
        var subjectsRef = db.Collection("subjects");
        var subjectsSnapshot = await subjectsRef.GetSnapshotAsync();

        List<DocumentSnapshot> subjectDocs = new List<DocumentSnapshot>();

        foreach (var subjectDoc in subjectsSnapshot.Documents)
        {
            if (subjectDoc.TryGetValue("manager", out string managerEmail) && managerEmail == userEmail)
            {
                subjectDocs.Add(subjectDoc);
            }
        }

        subjectDocs.Sort((a, b) => string.Compare(a.Id, b.Id)); // 오름차순 정렬
        CreateSubjectButtons(subjectDocs);

        RegisterRealTimeUpdates(userEmail);
    }

    void RegisterRealTimeUpdates(string userEmail)
    {
        var subjectsRef = db.Collection("subjects");

        subjectListener = subjectsRef.Listen(snapshot =>
        {
            List<DocumentSnapshot> subjectDocs = new List<DocumentSnapshot>();

            foreach (var subjectDoc in snapshot.Documents)
            {
                if (subjectDoc.TryGetValue("manager", out string managerEmail) && managerEmail == userEmail)
                {
                    subjectDocs.Add(subjectDoc);
                }
            }

            subjectDocs.Sort((a, b) =>
            {
                string nameA = a.TryGetValue("name", out string nA) ? nA : "";
                string nameB = b.TryGetValue("name", out string nB) ? nB : "";
                return string.Compare(nameA, nameB);
            });

            CreateSubjectButtons(subjectDocs);
        });
    }

    void CreateSubjectButtons(List<DocumentSnapshot> subjectDocs)
    {
        // 기존 버튼 모두 제거
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (var subjectDoc in subjectDocs)
        {
            string currentSubjectId = subjectDoc.Id;  // 문서 ID (subject 이름)

            GameObject button = Instantiate(buttonPrefab, contentTransform);

            // 버튼 안에서 Image, name, info 찾기
            Image img = button.transform.Find("Image").GetComponent<Image>();
            TMP_Text nameTxt = button.transform.Find("name").GetComponent<TMP_Text>();
            TMP_Text infoTxt = button.transform.Find("info").GetComponent<TMP_Text>();

            // Image 랜덤 설정
            if (subjectSprites != null && subjectSprites.Length > 0)
            {
                int randomIndex = Random.Range(0, subjectSprites.Length);
                img.sprite = subjectSprites[randomIndex];
            }

            // name 텍스트 설정 (name 필드 가져오기)
            if (subjectDoc.TryGetValue("name", out string subjectName))
            {
                nameTxt.text = subjectName;
            }
            else
            {
                nameTxt.text = currentSubjectId;  // fallback
            }

            // info 텍스트 설정 (day + startTime~endTime)
            string day = subjectDoc.TryGetValue("day", out string d) ? d : "";
            string startTime = subjectDoc.TryGetValue("startTime", out string st) ? st : "";
            string endTime = subjectDoc.TryGetValue("endTime", out string et) ? et : "";

            infoTxt.text = $"{day}, {startTime}~{endTime}";

            // 버튼 클릭 이벤트 연결
            Button btn = button.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSubjectButtonClick(currentSubjectId));
        }
    }

    void OnSubjectButtonClick(string selectedSubjectName)
    {
        Debug.Log($"✅ 클릭한 과목: {selectedSubjectName}");

        // 동일한 키로 저장
        PlayerPrefs.SetString("SelectedSubject", selectedSubjectName);
        PlayerPrefs.Save();

        // 씬 전환
        SceneManager.LoadScene("attendanceforManager");
    }

    void OnDestroy()
    {
        subjectListener?.Stop();
    }
}

