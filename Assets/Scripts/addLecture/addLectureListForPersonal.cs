using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class addLectureListForPersonal : MonoBehaviour
{
    public GameObject buttonPrefab;  // 버튼 프리팹
    public Transform contentTransform;  // Scroll View의 Content
    public Sprite[] subjectSprites;  // 프리팹 안 Image에 랜덤 적용할 Sprite 배열

    private FirebaseFirestore db;
    private ListenerRegistration subjectListener;
    private string userEmail;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;

        if (!string.IsNullOrEmpty(userEmail))
        {
            FetchSubjects();
        }
        else
        {
            Debug.LogError("❌ 사용자가 로그인되지 않았습니다.");
        }
    }

    async void FetchSubjects()
    {
        var subjectsRef = db.Collection("subjects");
        var subjectsSnapshot = await subjectsRef.GetSnapshotAsync();

        List<DocumentSnapshot> subjectDocs = new List<DocumentSnapshot>();

        foreach (var subjectDoc in subjectsSnapshot.Documents)
        {
            List<string> personalList = subjectDoc.ContainsField("personal")
                ? subjectDoc.GetValue<List<string>>("personal")
                : new List<string>();

            if (personalList.Contains(userEmail))
            {
                subjectDocs.Add(subjectDoc);
            }
        }

        subjectDocs = subjectDocs.OrderBy(doc =>
            doc.TryGetValue("name", out string name) ? name : "").ToList();

        RegisterRealTimeUpdates();
        CreateSubjectButtons(subjectDocs);
    }

    void RegisterRealTimeUpdates()
    {
        var subjectsRef = db.Collection("subjects");

        subjectListener = subjectsRef.Listen(snapshot =>
        {
            List<DocumentSnapshot> subjectDocs = new List<DocumentSnapshot>();

            foreach (var subjectDoc in snapshot.Documents)
            {
                List<string> personalList = subjectDoc.ContainsField("personal")
                    ? subjectDoc.GetValue<List<string>>("personal")
                    : new List<string>();

                if (personalList.Contains(userEmail))
                {
                    subjectDocs.Add(subjectDoc);
                }
            }

            subjectDocs = subjectDocs.OrderBy(doc =>
                doc.TryGetValue("name", out string name) ? name : "").ToList();

            CreateSubjectButtons(subjectDocs);
        });
    }

    void CreateSubjectButtons(List<DocumentSnapshot> subjectDocs)
    {
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (var doc in subjectDocs)
        {
            string subjectId = doc.Id;
            string subjectName = doc.TryGetValue("name", out string name) ? name : subjectId;

            GameObject button = Instantiate(buttonPrefab, contentTransform);

            Image img = button.transform.Find("Image").GetComponent<Image>();
            TMP_Text nameTxt = button.transform.Find("name").GetComponent<TMP_Text>();
            TMP_Text infoTxt = button.transform.Find("info").GetComponent<TMP_Text>();

            if (subjectSprites != null && subjectSprites.Length > 0)
            {
                int randomIndex = Random.Range(0, subjectSprites.Length);
                img.sprite = subjectSprites[randomIndex];
            }

            nameTxt.text = subjectName;

            string day = doc.TryGetValue("day", out string d) ? d : "";
            string startTime = doc.TryGetValue("startTime", out string st) ? st : "";
            string endTime = doc.TryGetValue("endTime", out string et) ? et : "";
            infoTxt.text = $"{day}, {startTime}~{endTime}";

            Button btnComponent = button.GetComponent<Button>();
            btnComponent.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("SelectedSubjectName", subjectName);  // 이름 저장
                PlayerPrefs.SetString("SelectedSubject", subjectId);        // ID 저장 (추가적으로 필요 시)
                Debug.Log($"🎯 선택된 과목: {subjectName}");
                SceneManager.LoadScene("attendanceForPersonal");
            });
        }
    }

    void OnDestroy()
    {
        if (subjectListener != null)
        {
            subjectListener.Stop();
        }
    }
}