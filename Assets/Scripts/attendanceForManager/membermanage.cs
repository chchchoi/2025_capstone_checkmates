using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class memberManage : MonoBehaviour
{
    public TMP_Dropdown subjectDropdown;
    public GameObject textPrefab;
    public Transform contentTransform;
    public Sprite sprite1;
    public Sprite sprite2;

    private FirebaseFirestore db;
    private ListenerRegistration subjectListener;
    private string selectedSubjectId;
    private string userEmail;
    private List<string> currentStudentEmails = new List<string>();
    public IReadOnlyDictionary<GameObject, string> ObjectToEmail => emailToObject.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    private Dictionary<string, GameObject> emailToObject = new();
    public IReadOnlyDictionary<string, GameObject> EmailToObject => emailToObject;
    private Dictionary<string, string> subjectIdToName = new();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;

        if (!string.IsNullOrEmpty(userEmail))
        {
            Debug.Log("✅ 로그인된 사용자: " + userEmail);
            RegisterRealTimeUpdates();
        }
        else
        {
            Debug.LogError("❌ 로그인된 사용자가 없음.");
        }
    }

    void RegisterRealTimeUpdates()
    {
        var subjectsRef = db.Collection("subjects").WhereEqualTo("manager", userEmail);

        subjectListener = subjectsRef.Listen(snapshot =>
        {
            subjectIdToName.Clear();
            List<string> subjectNames = new List<string>();

            foreach (var subjectDoc in snapshot.Documents)
            {
                if (subjectDoc.TryGetValue("name", out string subjectName))
                {
                    subjectIdToName[subjectDoc.Id] = subjectName;
                    subjectNames.Add(subjectName);
                }
            }

            subjectNames.Sort();
            Debug.Log($"가져온 과목명 개수: {subjectNames.Count}");

            if (subjectNames.Count > 0)
            {
                UpdateDropdown(subjectNames);
            }
            else
            {
                Debug.LogWarning("매니저가 관리하는 과병이 없음!");
                subjectDropdown.ClearOptions();
                ClearStudentListUI();
            }
        });
    }

    void UpdateDropdown(List<string> subjectNames)
    {
        subjectDropdown.ClearOptions();
        subjectDropdown.AddOptions(subjectNames);

        string previousSubjectId = PlayerPrefs.GetString("SelectedSubject", "");
        string previousName = subjectIdToName.ContainsKey(previousSubjectId) ? subjectIdToName[previousSubjectId] : subjectNames[0];
        int index = subjectNames.IndexOf(previousName);
        subjectDropdown.value = index != -1 ? index : 0;
        subjectDropdown.RefreshShownValue();

        subjectDropdown.onValueChanged.RemoveAllListeners();
        subjectDropdown.onValueChanged.AddListener(OnSubjectChanged);

        if (subjectNames.Count > 0)
        {
            OnSubjectChanged(subjectDropdown.value);
        }
    }

    void OnSubjectChanged(int index)
    {
        if (index < 0 || index >= subjectDropdown.options.Count)
        {
            Debug.LogWarning("유효하지 않은 드론다운 선택");
            return;
        }

        string selectedName = subjectDropdown.options[index].text;

        foreach (var pair in subjectIdToName)
        {
            if (pair.Value == selectedName)
            {
                selectedSubjectId = pair.Key;
                break;
            }
        }

        PlayerPrefs.SetString("SelectedSubject", selectedSubjectId);
        PlayerPrefs.Save();

        if (subjectListener != null)
        {
            subjectListener.Stop();
        }

        ListenToStudentEmails(selectedSubjectId);
    }

    void ListenToStudentEmails(string subjectId)
    {
        var subjectRef = db.Collection("subjects").Document(subjectId);

        subjectRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                List<string> personalEmails = new List<string>();

                if (snapshot.TryGetValue("personal", out personalEmails) && personalEmails != null)
                {
                    Debug.Log($"학생 이메일 리스트 받음! 총 {personalEmails.Count}명");
                    UpdateStudentList(personalEmails);
                }
                else
                {
                    Debug.Log("학생 없음! 리스트 초기화");
                    ClearStudentListUI();
                }
            }
            else
            {
                Debug.LogWarning("해당 과병 문서가 존재하지 않음.");
                ClearStudentListUI();
            }
        });
    }

    void UpdateStudentList(List<string> personalEmails)
    {
    HashSet<string> incomingSet = new HashSet<string>(personalEmails);

    // 제거된 이메일에 해당하는 GameObject 삭제
    var toRemove = emailToObject.Keys.Except(incomingSet).ToList();
    foreach (var email in toRemove)
    {
        if (emailToObject[email] != null)
            Destroy(emailToObject[email]);
        emailToObject.Remove(email);
    }

    // 새로 추가 혹은 누락된 객체 재생성
    foreach (var email in personalEmails)
    {
        if (!emailToObject.ContainsKey(email) || emailToObject[email] == null)
        {
            AddUserEmailToUI(email);
        }
    }

    currentStudentEmails = new List<string>(personalEmails);
    }

    void ClearStudentListUI()
    {
        foreach (var obj in emailToObject.Values)
        {
            Destroy(obj);
        }
        emailToObject.Clear();
        currentStudentEmails.Clear();
    }

    void AddUserEmailToUI(string email)
    {
        if (textPrefab == null)
        {
            Debug.LogError("textPrefab이 Inspector에서 할당되지 않음!");
            return;
        }

        GameObject textObject = Instantiate(textPrefab, contentTransform);
        emailToObject[email] = textObject;

        TMP_Text userText = textObject.transform.Find("userText")?.GetComponent<TMP_Text>();

        if (userText == null)
        {
            Debug.LogError("userText가 프리파에서 없음!");
            return;
        }

        var userRef = db.Collection("users").Document("personal").Collection(email).Document("profile");

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                DocumentSnapshot userSnapshot = task.Result;
                if (userSnapshot.Exists)
                {
                    string studentName = userSnapshot.GetValue<string>("name");
                    userText.text = studentName;
                    Debug.Log($"UI에 학생 이름 추가됨: {studentName}");
                }
                else
                {
                    Debug.LogWarning("해당 이메일로 학생 정보가 없음.");
                }
            }
            else
            {
                Debug.LogError($"학생 이름 가져오기 실패: {task.Exception}");
            }
        });

        Image studentImage = textObject.GetComponentInChildren<Image>();
        if (studentImage != null)
        {
            studentImage.sprite = Random.Range(0, 2) == 0 ? sprite1 : sprite2;
        }

        Button deleteButton = textObject.GetComponentInChildren<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => DeleteStudent(email));
        }
        else
        {
            Debug.LogError("삭제 버튼 없음!");
        }

        Debug.Log($"UI에 학생 추가됨: {email}");
    }

    private void DeleteStudent(string email)
    {
        var subjectRef = db.Collection("subjects").Document(selectedSubjectId);

        subjectRef.UpdateAsync("personal", FieldValue.ArrayRemove(email)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"학생 이메일 삭제됨: {email}");

                if (emailToObject.ContainsKey(email))
                {
                    Destroy(emailToObject[email]);
                    emailToObject.Remove(email);
                }

                currentStudentEmails.Remove(email);
            }
            else
            {
                Debug.LogError($"학생 이메일 삭제 실패: {task.Exception}");
            }
        });
    }
    public void DeleteStudentByEmail(string email)
    {
    DeleteStudent(email);
    }

}
