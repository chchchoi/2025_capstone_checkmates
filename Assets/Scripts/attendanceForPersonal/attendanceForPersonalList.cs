using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;

public class AttendanceForPersonalList : MonoBehaviour
{
    private string selectedSubject;
    public Transform scrollViewContent;
    public GameObject attendanceButtonPrefab;

    public Sprite[] randomSprites; // 🎲 랜덤 이미지용 3개 스프라이트
    public Sprite presentSprite;
    public Sprite lateSprite;
    public Sprite absentSprite;

    private FirebaseFirestore db;
    private string currentEmail;

    private DateTime currentDate;
    private List<string> weekDays;

    // 랜덤 이미지를 적용할 Image 오브젝트
    public Image randomImageObject;

    void Start()
    {
        Debug.Log("✅ Start 실행됨");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Exception != null)
            {
                Debug.LogError("🔥 Firebase 초기화 실패: " + task.Exception);
                return;
            }

            Debug.Log("✅ Firebase 초기화 성공");

            db = FirebaseFirestore.DefaultInstance;
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user != null)
            {
                currentEmail = user.Email;
                selectedSubject = PlayerPrefs.GetString("SelectedSubjectName");

                Debug.Log("👤 현재 사용자 이메일: " + currentEmail);
                Debug.Log("📚 선택된 과목: " + selectedSubject);

                currentDate = DateTime.Now;
                weekDays = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

                CheckAttendance();
            }
            else
            {
                Debug.LogError("❌ 로그인된 사용자가 없습니다.");
            }
        });
    }

    void CheckAttendance()
    {
        Debug.Log("🔍 CheckAttendance 시작");

        db.Collection("subjects")
          .WhereEqualTo("name", selectedSubject)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task => {
              if (task.IsCompletedSuccessfully && task.Result.Count > 0)
              {
                  DocumentSnapshot snapshot = task.Result.Documents.First();

                  if (snapshot.Exists && snapshot.ContainsField("personal"))
                  {
                      List<string> emails = snapshot.GetValue<List<string>>("personal");
                      Debug.Log("📧 personal 이메일 목록 로드됨");

                      if (emails.Contains(currentEmail))
                      {
                          Debug.Log("✅ 이메일이 personal 목록에 포함됨");
                          GetSubjectDetails(snapshot);
                      }
                      else
                      {
                          Debug.LogWarning("⚠️ 이메일이 personal 목록에 없음");
                          DisplayNoAttendanceInfo("출석 정보가 없습니다. (이메일 미포함)");
                      }
                  }
                  else
                  {
                      Debug.LogWarning("⚠️ 문서에 personal 필드 없음");
                      DisplayNoAttendanceInfo("과목 정보가 없거나 personal 필드가 없습니다.");
                  }
              }
              else
              {
                  Debug.LogError("❌ 과목 문서 가져오기 실패: " + task.Exception);
                  DisplayNoAttendanceInfo("출석 데이터를 가져오는 데 실패했습니다.");
              }
          });
    }

    void GetSubjectDetails(DocumentSnapshot snapshot)
    {
        Debug.Log("🧾 GetSubjectDetails 실행");

        if (snapshot.ContainsField("createDate") && snapshot.ContainsField("day"))
        {
            string createDateStr = snapshot.GetValue<string>("createDate");
            string dayStr = snapshot.GetValue<string>("day");

            Debug.Log("🗓️ createDate: " + createDateStr);
            Debug.Log("📆 시작 요일: " + dayStr);

            DateTime createDate = DateTime.Parse(createDateStr);
            int startDayIndex = weekDays.IndexOf(dayStr);

            if (startDayIndex != -1)
            {
                Debug.Log("✅ 요일 인덱스 파싱 성공: " + startDayIndex);
                CreateAttendanceButtons(createDate, startDayIndex, snapshot.Id);
            }
            else
            {
                Debug.LogError("❌ 요일 파싱 실패: " + dayStr);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ createDate 또는 day 필드 없음");
            DisplayNoAttendanceInfo("과목 정보에 날짜나 요일 정보가 없습니다.");
        }
    }

    void CreateAttendanceButtons(DateTime createDate, int startDayIndex, string subjectId)
    {
        Debug.Log("🎯 CreateAttendanceButtons 시작");

        List<DateTime> dateList = new List<DateTime>();
        DateTime tempDate = createDate;

        while (tempDate <= currentDate)
        {
            string dayOfWeek = tempDate.ToString("ddd", new CultureInfo("en-US"));
            if (weekDays[startDayIndex] == dayOfWeek)
            {
                dateList.Add(tempDate);
            }
            tempDate = tempDate.AddDays(1);
        }

        dateList.Sort();

        List<string> dateStrings = new List<string>();
        foreach (var date in dateList)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            dateStrings.Add(dateStr);
            Debug.Log($"📅 버튼 생성 대상 날짜: {dateStr}");
        }

        db.Collection("subjects")
          .Document(subjectId)
          .Collection(currentEmail)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task => {
              if (task.IsCompleted)
              {
                  Debug.Log("📂 출석 문서 가져오기 성공");

                  QuerySnapshot allDocs = task.Result;
                  Dictionary<string, string> attendanceMap = new Dictionary<string, string>();

                  foreach (DocumentSnapshot doc in allDocs.Documents)
                  {
                      if (doc.ContainsField("status"))
                      {
                          string date = doc.Id;
                          string status = doc.GetValue<string>("status");
                          attendanceMap[date] = status;
                      }
                  }

                  foreach (string dateStr in dateStrings)
                  {
                      string status = attendanceMap.ContainsKey(dateStr) ? attendanceMap[dateStr] : "결석";
                      CreateAttendanceButton(dateStr, status);
                  }
              }
              else
              {
                  Debug.LogError("❌ 출석 문서 가져오기 실패: " + task.Exception);
                  DisplayNoAttendanceInfo("출석 문서를 가져오는 데 실패했습니다.");
              }
          });
    }

    void CreateAttendanceButton(string date, string status)
    {
        Debug.Log($"🧷 버튼 생성 중: {date} - {status}");

        if (attendanceButtonPrefab == null)
        {
            Debug.LogError("❌ attendanceButtonPrefab이 null입니다. 인스펙터에서 할당해주세요!");
            return;
        }

        GameObject buttonObject = Instantiate(attendanceButtonPrefab, scrollViewContent);
        if (buttonObject == null)
        {
            Debug.LogError("❌ 버튼 프리팹 인스턴스화 실패!");
            return;
        }

        // 날짜 텍스트
        TMP_Text dateText = buttonObject.transform.Find("dateMessage")?.GetComponent<TMP_Text>();
        if (dateText != null) dateText.text = date;

        // 상태 텍스트
        TMP_Text statusText = buttonObject.transform.Find("statusMessage")?.GetComponent<TMP_Text>();
        if (statusText != null) statusText.text = status;

        // 출석 상태 이미지
        Image attendanceImage = buttonObject.transform.Find("attendanceImage")?.GetComponent<Image>();
        if (attendanceImage != null)
        {
            switch (status)
            {
                case "출석":
                    attendanceImage.sprite = presentSprite;
                    break;
                case "지각":
                    attendanceImage.sprite = lateSprite;
                    break;
                case "결석":
                    attendanceImage.sprite = absentSprite;
                    break;
            }
        }

        // 랜덤 이미지 넣기 (이미지가 randomImageObject로 지정된 Image에서 랜덤으로 변경됨)
        if (randomImageObject != null && randomSprites != null && randomSprites.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, randomSprites.Length);
            randomImageObject.sprite = randomSprites[randomIndex];
        }

        // 버튼 클릭 이벤트
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnAttendanceButtonClick(date, status));
        }

        Debug.Log($"✅ 출석 버튼 생성됨 - 날짜: {date}, 상태: {status}");
    }

    void OnAttendanceButtonClick(string date, string status)
    {
        Debug.Log($"👆 클릭됨: 출석 날짜: {date}, 상태: {status}");
    }

    void DisplayNoAttendanceInfo(string message)
    {
        Debug.Log("🚫 DisplayNoAttendanceInfo 호출됨: " + message);

        foreach (Transform child in scrollViewContent)
        {
            Destroy(child.gameObject);
        }

        GameObject noInfoText = new GameObject("NoInfoText");
        Text text = noInfoText.AddComponent<Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.color = Color.black;
        noInfoText.transform.SetParent(scrollViewContent);
    }
}
