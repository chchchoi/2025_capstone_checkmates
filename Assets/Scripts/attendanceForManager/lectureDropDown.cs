// ✅ LectureDropdown.cs - 고유 ID 사용 + 드롭다운은 name 필드 기반 + 요일 필드 제거 + 시간 중복 방지

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
    public TMP_Text infoText; // 🔹 시간 중복 메시지 출력용 텍스트
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
            Debug.LogError("❌ 사용자가 로그인되지 않았습니다.");
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
            Debug.LogWarning("❗ 과목 리스트가 비어 있습니다. ManagerProfile 씬으로 이동합니다.");
            SceneManager.LoadScene("ManagerProfile"); // ✅ 씬 이름 정확히 기입
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

        db.Collection("subjects").WhereEqualTo("manager", userEmail).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                foreach (var doc in task.Result.Documents)
                {
                    if (doc.Id == selectedSubjectId) continue;

                    string existStart = doc.TryGetValue("startTime", out string st) ? st : "00:00";
                    string existEnd = doc.TryGetValue("endTime", out string et) ? et : "00:00";

                    if (IsTimeOverlap(newStart, newEnd, existStart, existEnd))
                    {
                        infoText.text = "이미 겹치는 시간대의 과목이 존재합니다.";
                        Debug.LogWarning("시간 겹침 감지됨");
                        return;
                    }
                }

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
                        Debug.Log("✅ 수정 성공");
                        infoText.text = "";
                        editPanel.SetActive(false);
                    }
                });
            }
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
                Debug.Log("✅ 삭제 성공");
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