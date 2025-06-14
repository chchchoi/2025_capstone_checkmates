using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LectureDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TMP_Text codeText;
    public TMP_Text infoText; // 시간 중복 메시지 출력용 텍스트
    public GameObject Codepanel;
    public GameObject editPanel;
    public Button closeButton, copyButton, editButton, deleteButton, updateButton;
    public TMP_InputField nameInput, startHourInput, startMinuteInput, endHourInput, endMinuteInput;

    private FirebaseFirestore db;
    private ListenerRegistration subjectListener;
    private string selectedSubjectId = "";
    private Dictionary<string, string> subjectIdNameMap = new();
    private string userEmail;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;

        closeButton.onClick.AddListener(ClosePanel);
        copyButton.onClick.AddListener(CopyCodeToClipboard);
        editButton.onClick.AddListener(OpenEditPanel);
        updateButton.onClick.AddListener(UpdateLecture);
        deleteButton.onClick.AddListener(DeleteLecture);

        editButton.interactable = false;
        deleteButton.interactable = false;

        if (!string.IsNullOrEmpty(userEmail))
        {
            RegisterRealTimeUpdates();
        }
        else
        {
            Debug.LogError("사용자가 로그인되지 않았습니다.");
        }
    }

    void RegisterRealTimeUpdates()
    {
        var subjectsRef = db.Collection("subjects").WhereEqualTo("manager", userEmail);

        subjectListener = subjectsRef.Listen(snapshot =>
        {
            subjectIdNameMap.Clear();
            List<string> nameList = new();

            foreach (var doc in snapshot.Documents)
            {
                string name = doc.TryGetValue("name", out string n) ? n : "Unnamed";
                subjectIdNameMap[doc.Id] = name;
                nameList.Add(name);
            }

            nameList.Sort();
            UpdateDropdown(nameList);
        });
    }

    void UpdateDropdown(List<string> names)
    {
        dropdown.ClearOptions();

        if (names == null || names.Count == 0)
        {
            Debug.LogWarning("과목 리스트가 비어 있습니다. ManagerProfile 씬으로 이동합니다.");
            SceneManager.LoadScene("ManagerProfile"); 
            return;
        }

        dropdown.AddOptions(names);

        string previousId = PlayerPrefs.GetString("SelectedSubject", "");
        string previousName = subjectIdNameMap.ContainsKey(previousId) ? subjectIdNameMap[previousId] : names[0];
        int index = names.IndexOf(previousName);
        dropdown.value = index != -1 ? index : 0;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnSubjectChanged);
        OnSubjectChanged(dropdown.value);
    }


    void OnSubjectChanged(int index)
    {
        string selectedName = dropdown.options[index].text;

        foreach (var pair in subjectIdNameMap)
        {
            if (pair.Value == selectedName)
            {
                selectedSubjectId = pair.Key;
                break;
            }
        }

        PlayerPrefs.SetString("SelectedSubject", selectedSubjectId);
        PlayerPrefs.Save();

        FetchAndDisplayCode();
        editButton.interactable = true;
        deleteButton.interactable = true;
        FindFirstObjectByType<dateController>()?.RefreshSubject();
    }

    void FetchAndDisplayCode()
    {
        if (string.IsNullOrEmpty(selectedSubjectId)) return;

        var docRef = db.Collection("subjects").Document(selectedSubjectId);
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result.Exists && task.Result.TryGetValue("lectureCode", out string code))
            {
                codeText.text = code ?? "Code: 없음";
            }
            else
            {
                codeText.text = "Code: 없음";
            }
        });
    }

    void OpenEditPanel()
    {
        if (string.IsNullOrEmpty(selectedSubjectId)) return;

        db.Collection("subjects").Document(selectedSubjectId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result.Exists)
            {
                nameInput.text = task.Result.TryGetValue("name", out string name) ? name : "";

                string startTime = task.Result.TryGetValue("startTime", out string st) ? st : "00:00";
                string[] startSplit = startTime.Split(':');
                startHourInput.text = startSplit.Length > 0 ? startSplit[0] : "00";
                startMinuteInput.text = startSplit.Length > 1 ? startSplit[1] : "00";

                string endTime = task.Result.TryGetValue("endTime", out string et) ? et : "00:00";
                string[] endSplit = endTime.Split(':');
                endHourInput.text = endSplit.Length > 0 ? endSplit[0] : "00";
                endMinuteInput.text = endSplit.Length > 1 ? endSplit[1] : "00";

                infoText.text = "";
                editPanel.SetActive(true);
            }
        });
    }

    void UpdateLecture()
    {
        if (string.IsNullOrEmpty(selectedSubjectId)) return;

        string newName = nameInput.text.Trim();
        int newStartHour = int.Parse(startHourInput.text);
        int newStartMinute = int.Parse(startMinuteInput.text);
        int newEndHour = int.Parse(endHourInput.text);
        int newEndMinute = int.Parse(endMinuteInput.text);

        string newStart = $"{newStartHour:D2}:{newStartMinute:D2}";
        string newEnd = $"{newEndHour:D2}:{newEndMinute:D2}";

        // 현재 수정하려는 과목의 정보 가져오기
        db.Collection("subjects").Document(selectedSubjectId).GetSnapshotAsync().ContinueWithOnMainThread(docTask =>
        {
            if (!docTask.IsCompletedSuccessfully || !docTask.Result.Exists)
            {
                infoText.text = "과목 정보를 불러올 수 없습니다.";
                return;
            }

            string currentDay = docTask.Result.TryGetValue("day", out string d) ? d : "";

            // 🔍 과목 이름 중복 검사 (동일 매니저 기준)
            db.Collection("subjects")
                .WhereEqualTo("manager", userEmail)
                .WhereEqualTo("name", newName)
                .GetSnapshotAsync().ContinueWithOnMainThread(nameTask =>
                {
                    if (!nameTask.IsCompletedSuccessfully)
                    {
                        infoText.text = "과목 이름 중복 검사를 실패했습니다.";
                        return;
                    }

                    foreach (var doc in nameTask.Result.Documents)
                    {
                        if (doc.Id != selectedSubjectId)
                        {
                            infoText.text = "같은 이름의 과목이 이미 존재합니다.";
                            return;
                        }
                    }

                // 이름 중복 없음 → 시간 겹침 검사
                db.Collection("subjects")
                  .WhereEqualTo("manager", userEmail)
                  .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                  {
                      if (task.IsCompletedSuccessfully)
                      {
                          foreach (var doc in task.Result.Documents)
                          {
                              if (doc.Id == selectedSubjectId) continue;

                              string existDay = doc.TryGetValue("day", out string d) ? d : "";
                              string existStart = doc.TryGetValue("startTime", out string st) ? st : "00:00";
                              string existEnd = doc.TryGetValue("endTime", out string et) ? et : "00:00";

                              if (existDay == currentDay && IsTimeOverlap(newStart, newEnd, existStart, existEnd))
                              {
                                  infoText.text = "같은 요일에 겹치는 시간대의 과목이 존재합니다.";
                                  Debug.LogWarning("시간 겹침 발생");
                                  return;
                              }
                          }

                      // 이름/시간 모두 문제 없음 → 업데이트 진행
                      var update = new Dictionary<string, object>
                          {
                              ["name"] = newName,
                              ["startTime"] = newStart,
                              ["endTime"] = newEnd
                          };

                          db.Collection("subjects").Document(selectedSubjectId).UpdateAsync(update).ContinueWithOnMainThread(updateTask =>
                          {
                              if (!updateTask.IsFaulted)
                              {
                                  Debug.Log("과목 정보 수정 완료");
                                  infoText.text = "";
                                  editPanel.SetActive(false);
                              }
                              else
                              {
                                  infoText.text = "과목 정보 수정에 실패했습니다.";
                              }
                          });
                      }
                      else
                      {
                          infoText.text = "시간 겹침 확인 중 오류가 발생했습니다.";
                      }
                  });
                });
        });
    }




    bool IsTimeOverlap(string newStart, string newEnd, string existStart, string existEnd)
    {
        System.TimeSpan ns = System.TimeSpan.Parse(newStart);
        System.TimeSpan ne = System.TimeSpan.Parse(newEnd);
        System.TimeSpan es = System.TimeSpan.Parse(existStart);
        System.TimeSpan ee = System.TimeSpan.Parse(existEnd);

        return ns < ee && ne > es;
    }

    void DeleteLecture()
    {
        if (string.IsNullOrEmpty(selectedSubjectId)) return;

        db.Collection("subjects").Document(selectedSubjectId).DeleteAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted)
            {
                Debug.Log("삭제 성공");
                selectedSubjectId = "";
                editPanel.SetActive(false);
                RegisterRealTimeUpdates();
            }
        });
    }

    void ClosePanel()
    {
        Codepanel.SetActive(false);
    }

    void CopyCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = codeText.text;
        Debug.Log("복사됨: " + codeText.text);
        ClosePanel();
    }

    void OnDestroy()
    {
        subjectListener?.Stop();
    }
}