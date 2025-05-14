using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class codeManager : MonoBehaviour
{
    public TMP_Text codeText; // 코드 값을 표시할 TMP 텍스트
    public GameObject panel; // 닫기 버튼이 제어할 패널
    public Button closeButton; // 창 닫기 버튼
    public Button copyButton; // 복사 버튼
    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Firestore 인스턴스 초기화
        closeButton.onClick.AddListener(ClosePanel);
        copyButton.onClick.AddListener(CopyCodeToClipboard);

        // 저장된 과목으로 코드 불러오기
        string selectedSubject = PlayerPrefs.GetString("SelectedSubject", "");
        if (!string.IsNullOrEmpty(selectedSubject))
        {
            FetchAndDisplayCode(selectedSubject);
        }
    }

    public void FetchAndDisplayCode(string selectedSubject)
    {
        DocumentReference subjectDocRef = db.Collection("subjects").Document(selectedSubject);

        subjectDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("lectureCode"))
                {
                    string code = snapshot.GetValue<string>("lectureCode");
                    codeText.text = "Code: " + code; // TMP 텍스트에 코드 값 표시
                    Debug.Log("불러온 코드: " + code);
                }
                else
                {
                    codeText.text = "Code: 없음";
                    Debug.Log("선택한 과목의 코드 필드가 없습니다.");
                }
            }
            else
            {
                Debug.LogError("과목 코드 가져오기 실패: " + task.Exception);
            }
        });
    }

    void ClosePanel()
    {
        panel.SetActive(false);
    }

    void CopyCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = codeText.text;
        Debug.Log("코드 복사됨: " + codeText.text);
    }
}
