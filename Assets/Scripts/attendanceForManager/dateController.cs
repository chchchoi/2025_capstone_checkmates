using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Globalization;

public class dateController : MonoBehaviour
{
    [Header("Dropdowns & Button")]
    public TMP_Dropdown dropdownYear;
    public TMP_Dropdown dropdownMonth;
    public TMP_Dropdown dropdownDay;
    public Button searchButton;

    [Header("Prefabs & Scroll Views")]
    public GameObject studentPrefab;
    public Transform attendanceContent;
    public Transform lateContent;
    public Transform absentContent;

    [Header("Sprites")]
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite sprite2;
    [Header("Attendance Icons")]
    [SerializeField] private Sprite spriteAttendColor;
    [SerializeField] private Sprite spriteAttendBlack;
    [SerializeField] private Sprite spriteLateColor;
    [SerializeField] private Sprite spriteLateBlack;
    [SerializeField] private Sprite spriteAbsentColor;
    [SerializeField] private Sprite spriteAbsentBlack;

    private DateTime createDay;
    private DateTime today;
    private FirebaseFirestore db;
    private string selectedSubject;
    private string subjectDay;

    private Dictionary<string, string> studentNameCache = new();
    private Dictionary<string, GameObject> studentObjectCache = new();
    private Dictionary<string, string> studentCurrentStatus = new();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        today = DateTime.UtcNow.ToLocalTime().Date;
        selectedSubject = PlayerPrefs.GetString("SelectedSubject");

        GetSubjectInfoFromFirestore(); // 이 안에서만 드롭다운 생성하도록
        searchButton.onClick.AddListener(OnSearchClicked);
    }

    public void RefreshSubject()
    {
        selectedSubject = PlayerPrefs.GetString("SelectedSubject");
        GetSubjectInfoFromFirestore();
    }

    void GetSubjectInfoFromFirestore()
    {
        db.Collection("subjects").Document(selectedSubject).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("과목 정보 가져오기 실패");
                return;
            }

            var snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogWarning("과목 문서가 존재하지 않습니다.");
                return;
            }

            // Firestore에서 createDay와 subjectDay 받아오기
            string createDateStr = snapshot.GetValue<string>("createDate");
            subjectDay = snapshot.GetValue<string>("day");

            // createDay를 제대로 파싱
            try
            {
                createDay = DateTime.ParseExact(createDateStr, "yyyy-MM-dd", null);
            }
            catch (Exception e)
            {
                Debug.LogError($"createDate 파싱 실패: {createDateStr}, 에러: {e.Message}");
                return;
            }

            // 반드시 createDay 설정 이후 드롭다운 호출
            PopulateYearDropdown();

            // 오늘이 수업 요일인 경우에만 출결 데이터 가져오기
            string selectedDate = $"{today:yyyy-MM-dd}";
            if (dropdownDay.options.Count > 0 && dropdownDay.options[0].text != "없음")
            {
                GetAttendanceData(selectedDate);
            }
            else
            {
                Debug.Log("오늘은 수업 요일이 아님! 프리팹 생성 생략");
                ClearContentViews();
            }
        });
    }


    void PopulateYearDropdown()
    {
        dropdownYear.ClearOptions();
        dropdownMonth.ClearOptions();
        dropdownDay.ClearOptions();

        List<string> years = new();
        for (int year = createDay.Year; year <= today.Year; year++)
        {
            years.Add(year.ToString());
        }

        dropdownYear.AddOptions(years);
        dropdownYear.onValueChanged.AddListener(delegate { PopulateMonthDropdown(); });

        PopulateMonthDropdown();
    }

    void PopulateMonthDropdown()
    {
        dropdownMonth.ClearOptions();
        dropdownDay.ClearOptions();

        int selectedYear = int.Parse(dropdownYear.options[dropdownYear.value].text);
        List<string> months = new();

        int startMonth = (selectedYear == createDay.Year) ? createDay.Month : 1;
        int endMonth = (selectedYear == today.Year) ? today.Month : 12;

        for (int month = startMonth; month <= endMonth; month++)
        {
            months.Add(month.ToString("D2"));
        }

        dropdownMonth.AddOptions(months);

        dropdownMonth.value = months.Count - 1;
        dropdownMonth.RefreshShownValue();

        dropdownMonth.onValueChanged.AddListener(delegate { PopulateDayDropdown(); });

        PopulateDayDropdown();
    }


    void PopulateDayDropdown()
    {
        dropdownDay.ClearOptions();

        int selectedYear = int.Parse(dropdownYear.options[dropdownYear.value].text);
        int selectedMonth = int.Parse(dropdownMonth.options[dropdownMonth.value].text);

        int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
        List<string> validDays = new();

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime current = new DateTime(selectedYear, selectedMonth, day);
            string currentDayStr = current.ToString("ddd", CultureInfo.InvariantCulture);

            if (current.Date >= createDay.Date && current.Date <= today.Date && currentDayStr == subjectDay)
            {
                validDays.Add(day.ToString("D2"));
            }
        }

        if (validDays.Count == 0)
        {
            validDays.Add("없음");
            dropdownDay.interactable = false;
            ClearContentViews();
        }
        else
        {
            dropdownDay.interactable = true;
            dropdownDay.AddOptions(validDays);

            dropdownDay.value = validDays.Count - 1;
            dropdownDay.RefreshShownValue();
        }
    }


    void OnSearchClicked()
    {
        string selectedYear = dropdownYear.options[dropdownYear.value].text;
        string selectedMonth = dropdownMonth.options[dropdownMonth.value].text;
        string selectedDay = dropdownDay.options[dropdownDay.value].text;

        if (selectedDay == "없음")
        {
            Debug.LogWarning("선택 가능한 날짜가 없습니다.");
            return;
        }

        string selectedDate = $"{selectedYear}-{selectedMonth}-{selectedDay}";
        Debug.Log($"검색 날짜: {selectedDate}");
        GetAttendanceData(selectedDate);
    }

    void ClearContentViews()
    {
        foreach (Transform child in attendanceContent) Destroy(child.gameObject);
        foreach (Transform child in lateContent) Destroy(child.gameObject);
        foreach (Transform child in absentContent) Destroy(child.gameObject);
        studentObjectCache.Clear();
        studentCurrentStatus.Clear();
    }

    void GetAttendanceData(string selectedDate)
    {
        ClearContentViews();
        studentNameCache.Clear();

        DocumentReference subjectRef = db.Collection("subjects").Document(selectedSubject);

        subjectRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("과목 정보 가져오기 실패");
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (!snapshot.Exists) return;

            List<string> emails = snapshot.GetValue<List<string>>("personal");

            foreach (string email in emails)
            {
                db.Collection("users").Document("personal").Collection(email).Document("profile")
                    .GetSnapshotAsync().ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.Result.Exists)
                        {
                            string name = profileTask.Result.GetValue<string>("name");
                            studentNameCache[email] = name;
                        }
                        else
                        {
                            studentNameCache[email] = "이름 없음";
                        }

                        if (studentNameCache.Count == emails.Count)
                        {
                            LoadAttendanceStatus(emails, selectedDate);
                        }
                    });
            }
        });
    }

    void LoadAttendanceStatus(List<string> emails, string selectedDate)
    {
        DocumentReference subjectRef = db.Collection("subjects").Document(selectedSubject);

        foreach (string email in emails)
        {
            DocumentReference statusDoc = subjectRef.Collection(email).Document(selectedDate);

            statusDoc.GetSnapshotAsync().ContinueWithOnMainThread(statusTask =>
            {
                string status = "결석";

                if (statusTask.Result.Exists)
                {
                    status = statusTask.Result.GetValue<string>("status");
                }

                studentCurrentStatus[email] = status;

                if (studentObjectCache.ContainsKey(email))
                {
                    Destroy(studentObjectCache[email]);
                    studentObjectCache.Remove(email);
                }

                GameObject studentObj = Instantiate(studentPrefab);
                studentObjectCache[email] = studentObj;
                studentObj.GetComponent<RectTransform>().localScale = Vector3.one;

                studentObj.GetComponentInChildren<TMP_Text>().text = studentNameCache[email];

                Image img = studentObj.GetComponentInChildren<Image>();
                if (img != null) img.sprite = UnityEngine.Random.Range(0, 2) == 0 ? sprite1 : sprite2;

                switch (status)
                {
                    case "출석":
                        studentObj.transform.SetParent(attendanceContent, false);
                        break;
                    case "지각":
                        studentObj.transform.SetParent(lateContent, false);
                        break;
                    case "결석":
                        studentObj.transform.SetParent(absentContent, false);
                        break;
                    default:
                        Destroy(studentObj);
                        return;
                }

                studentObj.transform.Find("gut").GetComponent<Button>().onClick
                    .AddListener(() => UpdateSingleStudentStatus(email, selectedDate, "출석", studentObj));
                studentObj.transform.Find("notBad").GetComponent<Button>().onClick
                    .AddListener(() => UpdateSingleStudentStatus(email, selectedDate, "지각", studentObj));
                studentObj.transform.Find("bad").GetComponent<Button>().onClick
                    .AddListener(() => UpdateSingleStudentStatus(email, selectedDate, "결석", studentObj));

                Image attendImg = studentObj.transform.Find("gut").GetComponent<Image>();
                Image lateImg = studentObj.transform.Find("notBad").GetComponent<Image>();
                Image absentImg = studentObj.transform.Find("bad").GetComponent<Image>();

                switch (status)
                {
                    case "출석":
                        attendImg.sprite = spriteAttendColor;
                        lateImg.sprite = spriteLateBlack;
                        absentImg.sprite = spriteAbsentBlack;
                        break;
                    case "지각":
                        attendImg.sprite = spriteAttendBlack;
                        lateImg.sprite = spriteLateColor;
                        absentImg.sprite = spriteAbsentBlack;
                        break;
                    case "결석":
                        attendImg.sprite = spriteAttendBlack;
                        lateImg.sprite = spriteLateBlack;
                        absentImg.sprite = spriteAbsentColor;
                        break;
                }
            });
        }
    }

    void UpdateSingleStudentStatus(string email, string selectedDate, string status, GameObject studentObj)
    {
        if (studentCurrentStatus.ContainsKey(email) && studentCurrentStatus[email] == status)
        {
            Debug.Log($"{email}는 이미 {status} 상태입니다. Firestore 업데이트 생략");
            return;
        }

        DocumentReference subjectRef = db.Collection("subjects").Document(selectedSubject);
        DocumentReference statusDoc = subjectRef.Collection(email).Document(selectedDate);

        statusDoc.SetAsync(new Dictionary<string, object> { { "status", status } }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"{email}의 상태가 {status}로 업데이트됨.");
                studentCurrentStatus[email] = status;

                switch (status)
                {
                    case "출석":
                        studentObj.transform.SetParent(attendanceContent, false);
                        break;
                    case "지각":
                        studentObj.transform.SetParent(lateContent, false);
                        break;
                    case "결석":
                        studentObj.transform.SetParent(absentContent, false);
                        break;
                }

                Image attendImg = studentObj.transform.Find("gut").GetComponent<Image>();
                Image lateImg = studentObj.transform.Find("notBad").GetComponent<Image>();
                Image absentImg = studentObj.transform.Find("bad").GetComponent<Image>();

                switch (status)
                {
                    case "출석":
                        attendImg.sprite = spriteAttendColor;
                        lateImg.sprite = spriteLateBlack;
                        absentImg.sprite = spriteAbsentBlack;
                        break;
                    case "지각":
                        attendImg.sprite = spriteAttendBlack;
                        lateImg.sprite = spriteLateColor;
                        absentImg.sprite = spriteAbsentBlack;
                        break;
                    case "결석":
                        attendImg.sprite = spriteAttendBlack;
                        lateImg.sprite = spriteLateBlack;
                        absentImg.sprite = spriteAbsentColor;
                        break;
                }
            }
            else
            {
                Debug.LogError($"상태 업데이트 실패: {task.Exception}");
            }
        });
    }
}
