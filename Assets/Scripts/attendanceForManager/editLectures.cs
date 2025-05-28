using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Text.RegularExpressions;
using TMPro;

public class editLectures : MonoBehaviour
{
    public TMP_InputField subjectNameInput;
    //public TMP_Dropdown dayDropdown;
    public TMP_InputField startHourInput;
    public TMP_InputField startMinuteInput;
    public TMP_InputField endHourInput;
    public TMP_InputField endMinuteInput;
    public Button closeButton;
    public Button editButton;  // 수정 버튼
    public Button deleteButton;  // 삭제 버튼
    public GameObject infoPanel; // 정보 판넬 (이게 보이도록 설정할 것임)

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userEmail;
    private string userType = "manager"; // 변경 가능
    private string selectedSubjectName; // 선택된 과목 이름

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            userEmail = auth.CurrentUser.Email;
            Debug.Log("현재 로그인한 사용자: " + userEmail);
        }
        else
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            return;
        }

        // 버튼에 리스너 추가
        editButton.onClick.AddListener(EditLecture);
        deleteButton.onClick.AddListener(DeleteLecture);
        closeButton.onClick.AddListener(CloseDialog);
    }

    // 과목 정보 수정
    void EditLecture()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            return;
        }

        string subjectName = subjectNameInput.text;
        //string day = dayDropdown.options[dayDropdown.value].text;
        string startHour = startHourInput.text;
        string startMinute = startMinuteInput.text;
        string endHour = endHourInput.text;
        string endMinute = endMinuteInput.text;

        if (string.IsNullOrEmpty(subjectName))
        {
            Debug.LogError("과목 이름을 입력하세요.");
            return;
        }

        // users > manager > email > lectures > 선택된 과목 이름 > Info 수정
        DocumentReference lectureRef = db.Collection("users").Document(userType)
            .Collection(userEmail)
            .Document("lectures")
            .Collection(selectedSubjectName)
            .Document("Info");

        Dictionary<string, object> lectureData = new Dictionary<string, object>
        {
            {"name", subjectName},
            //{"day", day},
            {"startTime", startHour + ":" + startMinute},
            {"endTime", endHour + ":" + endMinute}
        };

        lectureRef.SetAsync(lectureData).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("과목 정보 수정 완료: " + subjectName);
                // 수정 후 필드 초기화
                ResetInputFields();
            }
            else
            {
                Debug.LogError("과목 정보 수정 실패: " + task.Exception);
            }
        });

        // subjects > 과목 이름 > manager 필드 수정
        DocumentReference subjectRef = db.Collection("subjects").Document(subjectName);
        Dictionary<string, object> subjectData = new Dictionary<string, object>
        {
            {"name", subjectName},
            //{"day", day},
            {"startTime", startHour + ":" + startMinute},
            {"endTime", endHour + ":" + endMinute},
            {"manager", userEmail}
        };

        subjectRef.SetAsync(subjectData).ContinueWith(subjectTask =>
        {
            if (subjectTask.IsCompleted)
            {
                Debug.Log("과목 정보 수정 완료 (subjects): " + subjectName);
            }
            else
            {
                Debug.LogError("과목 정보 수정 실패 (subjects): " + subjectTask.Exception);
            }
        });
    }

    // 과목 정보 삭제
    void DeleteLecture()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            return;
        }

        // users > manager > email > lectures > 선택된 과목 이름 삭제
        DocumentReference lectureRef = db.Collection("users").Document(userType)
            .Collection(userEmail)
            .Document("lectures")
            .Collection(selectedSubjectName)
            .Document("Info");

        lectureRef.DeleteAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("과목 정보 삭제 완료: " + selectedSubjectName);
                // 삭제 후 필드 초기화
                ResetInputFields();
            }
            else
            {
                Debug.LogError("과목 정보 삭제 실패: " + task.Exception);
            }
        });

        // subjects > 과목 이름 삭제
        DocumentReference subjectRef = db.Collection("subjects").Document(selectedSubjectName);
        subjectRef.DeleteAsync().ContinueWith(subjectTask =>
        {
            if (subjectTask.IsCompleted)
            {
                Debug.Log("과목 삭제 완료 (subjects): " + selectedSubjectName);
            }
            else
            {
                Debug.LogError("과목 삭제 실패 (subjects): " + subjectTask.Exception);
            }
        });
    }

    // 과목 정보 판넬 열기
    public void ShowInfoPanel(string subjectName)
    {
        selectedSubjectName = subjectName; // 선택된 과목 이름 저장
        infoPanel.SetActive(true);

        // 과목 이름, 시간, 요일을 입력 필드에 표시
        DocumentReference lectureRef = db.Collection("users").Document(userType)
            .Collection(userEmail)
            .Document("lectures")
            .Collection(selectedSubjectName)
            .Document("Info");

        lectureRef.GetSnapshotAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                var doc = task.Result;
                if (doc.Exists)
                {
                    var subjectData = doc.ToDictionary();
                    subjectNameInput.text = subjectData["name"].ToString();
                    //dayDropdown.value = GetDayDropdownIndex(subjectData["day"].ToString());
                    startHourInput.text = subjectData["startTime"].ToString().Split(':')[0];
                    startMinuteInput.text = subjectData["startTime"].ToString().Split(':')[1];
                    endHourInput.text = subjectData["endTime"].ToString().Split(':')[0];
                    endMinuteInput.text = subjectData["endTime"].ToString().Split(':')[1];
                }
                else
                {
                    Debug.LogError("과목 정보를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogError("과목 정보 불러오기 실패: " + task.Exception);
            }
        });
    }

    /*
    int GetDayDropdownIndex(string day)
    {
        switch (day)
        {
            case "Monday": return 0;
            case "Tuesday": return 1;
            case "Wednesday": return 2;
            case "Thursday": return 3;
            case "Friday": return 4;
            default: return 0;
        }
    }
    */

    // 입력 필드를 초기화하는 메서드
    void ResetInputFields()
    {
        subjectNameInput.text = "";
        startHourInput.text = "";
        startMinuteInput.text = "";
        endHourInput.text = "";
        endMinuteInput.text = "";
        //dayDropdown.value = 0; // 첫 번째 옵션으로 초기화 (예: 월요일)
    }

    // 닫기 버튼
    void CloseDialog()
    {
        infoPanel.SetActive(false);
    }
}
