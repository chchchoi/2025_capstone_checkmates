using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Text.RegularExpressions;
using TMPro;

public class addLectureForManager : MonoBehaviour
{
    public GameObject addPanel;
    public TMP_InputField subjectNameInput;
    public TMP_Dropdown dayDropdown;
    public TMP_InputField startHourInput;
    public TMP_InputField startMinuteInput;
    public TMP_InputField endHourInput;
    public TMP_InputField endMinuteInput;
    public Button saveButton;
    public Button closeButton;
    public TMP_Text InfoMessage;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userEmail;

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

        saveButton.onClick.AddListener(SaveLecture);
        closeButton.onClick.AddListener(CloseDialog);

        // 숫자 입력 제한 적용
        startHourInput.onValueChanged.AddListener(delegate { ValidateNumberInput(startHourInput); });
        startMinuteInput.onValueChanged.AddListener(delegate { ValidateNumberInput(startMinuteInput); });
        endHourInput.onValueChanged.AddListener(delegate { ValidateNumberInput(endHourInput); });
        endMinuteInput.onValueChanged.AddListener(delegate { ValidateNumberInput(endMinuteInput); });
    }

    void SaveLecture()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            return;
        }

        string subjectName = subjectNameInput.text.Trim();
        string day = dayDropdown.options[dayDropdown.value].text;
        string startHour = startHourInput.text.Trim();
        string startMinute = startMinuteInput.text.Trim();
        string endHour = endHourInput.text.Trim();
        string endMinute = endMinuteInput.text.Trim();
        string lectureCode = GenerateLectureCode();
        string createDate = DateTime.Now.ToString("yyyy-MM-dd");

        if (string.IsNullOrEmpty(subjectName))
        {
            Debug.LogError("과목 이름을 입력하세요.");
            return;
        }

        db.Collection("subjects")
          .WhereEqualTo("manager", userEmail)
          .WhereEqualTo("day", day)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(queryTask =>
          {
              if (queryTask.IsCompletedSuccessfully)
              {
                  bool conflict = false;
                  string newStart = $"{startHour}:{startMinute}";
                  string newEnd = $"{endHour}:{endMinute}";

                  foreach (DocumentSnapshot doc in queryTask.Result.Documents)
                  {
                      string existStart = doc.GetValue<string>("startTime");
                      string existEnd = doc.GetValue<string>("endTime");

                      if (IsTimeOverlap(newStart, newEnd, existStart, existEnd))
                      {
                          conflict = true;
                          break;
                      }
                  }

                  if (conflict)
                  {
                      Debug.LogWarning("❌ 해당 요일에 겹치는 시간대의 과목이 이미 존재합니다.");
                      InfoMessage.text = "선택한 날짜에 시간대가 겹칩니다.";
                      return;
                  }

                  // 문서 ID 자동 생성 방식으로 저장
                  Dictionary<string, object> subjectData = new Dictionary<string, object>
                  {
                      {"name", subjectName},
                      {"day", day},
                      {"startTime", newStart},
                      {"endTime", newEnd},
                      {"lectureCode", lectureCode},
                      {"manager", userEmail},
                      {"createDate", createDate}
                  };

                  db.Collection("subjects").AddAsync(subjectData).ContinueWithOnMainThread(task =>
                  {
                      if (task.IsCompletedSuccessfully)
                      {
                          Debug.Log($"과목 저장 완료: {subjectName}");
                          ResetInputFields();

                          if (addPanel != null)
                          {
                              addPanel.SetActive(false);
                          }
                          else
                          {
                              Debug.LogWarning("⚠️ addPanel이 null입니다. 인스펙터에서 연결됐는지 확인하세요.");
                          }
                      }
                      else
                      {
                          Debug.LogError($"과목 저장 실패: {task.Exception}");
                      }
                  });
              }
              else
              {
                  Debug.LogError("기존 과목 조회 실패: " + queryTask.Exception);
              }
          });
    }

    bool IsTimeOverlap(string newStart, string newEnd, string existStart, string existEnd)
    {
        TimeSpan ns = TimeSpan.Parse(newStart);
        TimeSpan ne = TimeSpan.Parse(newEnd);
        TimeSpan es = TimeSpan.Parse(existStart);
        TimeSpan ee = TimeSpan.Parse(existEnd);

        return ns < ee && ne > es;
    }

    void CloseDialog()
    {
        gameObject.SetActive(false);
        ResetInputFields();
    }

    string GenerateLectureCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] codeArray = new char[8];
        for (int i = 0; i < 8; i++)
        {
            codeArray[i] = chars[random.Next(chars.Length)];
        }
        return new string(codeArray);
    }

    void ValidateNumberInput(TMP_InputField inputField)
    {
        string value = inputField.text;
        value = Regex.Replace(value, "[^0-9]", "");
        if (value.Length > 2)
        {
            value = value.Substring(0, 2);
        }
        inputField.text = value;
    }

    void ResetInputFields()
    {
        subjectNameInput.text = "";
        startHourInput.text = "";
        startMinuteInput.text = "";
        endHourInput.text = "";
        endMinuteInput.text = "";
        dayDropdown.value = 0;
        InfoMessage.text = ""; // error message
    }
}
