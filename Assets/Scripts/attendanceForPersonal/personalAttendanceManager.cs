using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Linq;

public class personalAttendanceManager : MonoBehaviour
{
    public TMP_Text subjectName;
    public TMP_Text subjectInfo;

    private FirebaseFirestore db;
    private string selectedSubject;

    void Start()
    {
        selectedSubject = PlayerPrefs.GetString("SelectedSubjectName", "없음");
        subjectName.text = selectedSubject;

        db = FirebaseFirestore.DefaultInstance;

        if (selectedSubject != "없음")
        {
            LoadSubjectInfo();
        }
        else
        {
            Debug.LogError("선택된 과목 이름이 없습니다.");
        }
    }

    void LoadSubjectInfo()
    {
        string userEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;

        Query subjectQuery = db.Collection("subjects")
            .WhereEqualTo("name", selectedSubject)
            .WhereArrayContains("personal", userEmail);

        subjectQuery.GetSnapshotAsync().ContinueWithOnMainThread(subjectTask =>
        {
            if (subjectTask.IsCompleted && subjectTask.Result.Count > 0)
            {
                DocumentSnapshot subjectDoc = subjectTask.Result.Documents.FirstOrDefault();

                string day = subjectDoc.ContainsField("day") ? subjectDoc.GetValue<string>("day") : "요일 없음";
                string startTime = subjectDoc.ContainsField("startTime") ? subjectDoc.GetValue<string>("startTime") : "시작 시간 없음";
                string endTime = subjectDoc.ContainsField("endTime") ? subjectDoc.GetValue<string>("endTime") : "종료 시간 없음";
                string managerEmail = subjectDoc.ContainsField("manager") ? subjectDoc.GetValue<string>("manager") : "매니저 없음";

                DocumentReference managerRef = db.Collection("users").Document("manager")
                                                .Collection(managerEmail).Document("profile");

                managerRef.GetSnapshotAsync().ContinueWithOnMainThread(managerTask =>
                {
                    if (managerTask.IsCompleted && managerTask.Result.Exists)
                    {
                        string managerName = managerTask.Result.ContainsField("name") ? managerTask.Result.GetValue<string>("name") : "이름 없음";
                        subjectInfo.text = $"{managerName} | {day} {startTime} ~ {endTime}";
                    }
                    else
                    {
                        Debug.LogError("매니저 프로필을 찾을 수 없습니다.");
                        subjectInfo.text = $"{managerEmail} | {day} {startTime} ~ {endTime}";
                    }
                });
            }
            else
            {
                Debug.LogError("해당 과목 정보를 찾을 수 없습니다.");
                subjectInfo.text = "과목 정보를 불러올 수 없습니다.";
            }
        });
    }
}
