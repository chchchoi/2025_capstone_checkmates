using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI; // ✅ 버튼 사용을 위해 추가

public class Schedule : MonoBehaviour
{
    public TextMeshProUGUI subjectText;
    public Button yourButton; // ✅ 버튼 변수 추가

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserEmail;

    private string currentStatus; // 현재 상태 (출석, 지각, 결석)
    private string currentSubject; // 현재 과목

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        currentUserEmail = auth.CurrentUser.Email;

        GetServerTime();
    }

    public async void GetServerTime()
    {
        DateTime currentServerTime = DateTime.Now;

        string currentDay = currentServerTime.ToString("ddd", new CultureInfo("en-US"));
        string currentTime = currentServerTime.ToString("HH:mm");

        Debug.Log("Current Day: " + currentDay + " | Time: " + currentTime);

        GetSubjectsForCurrentTime(currentDay, currentTime);
    }

    public async void GetSubjectsForCurrentTime(string currentDay, string currentTime)
    {
        Query subjectsQuery = db.Collection("subjects");
        QuerySnapshot subjectsSnapshot = await subjectsQuery.GetSnapshotAsync();

        foreach (DocumentSnapshot subjectSnapshot in subjectsSnapshot.Documents)
        {
            if (!subjectSnapshot.TryGetValue("manager", out string managerEmail) || managerEmail != currentUserEmail) continue;

            if (!subjectSnapshot.TryGetValue("name", out string subjectName)) continue;

            Dictionary<string, object> subjectData = subjectSnapshot.ToDictionary();
            if (subjectData.ContainsKey("day") && subjectData.ContainsKey("startTime") && subjectData.ContainsKey("endTime"))
            {
                string day = subjectData["day"].ToString().Trim();
                string startTimeStr = subjectData["startTime"].ToString();
                string endTimeStr = subjectData["endTime"].ToString();

                DateTime startTime = DateTime.Parse(startTimeStr);
                DateTime endTime = DateTime.Parse(endTimeStr);

                if (day.Equals(currentDay, StringComparison.OrdinalIgnoreCase) && IsTimeInRange(currentTime, startTime, endTime))
                {
                    currentStatus = CalculateCurrentStatus(startTime, currentTime);
                    currentSubject = subjectName;

                    subjectText.text = $"{subjectName}";
                    yourButton.interactable = true; // ✅ 과목 있을 때 버튼 활성화

                    SaveToPlayerPrefs(currentSubject, currentStatus);
                    return;
                }
            }
        }

        subjectText.text = "현재 과목 없음";
        yourButton.interactable = false; // ✅ 과목 없을 때 버튼 비활성화
    }

    private bool IsTimeInRange(string currentTimeStr, DateTime startTime, DateTime endTime)
    {
        TimeSpan current = TimeSpan.Parse(currentTimeStr);
        TimeSpan start = new TimeSpan(startTime.Hour, startTime.Minute, 0);
        TimeSpan end = new TimeSpan(endTime.Hour, endTime.Minute, 0);

        TimeSpan earlyStart = start - TimeSpan.FromMinutes(10);
        TimeSpan earlyEnd = end - TimeSpan.FromMinutes(10);

        return current >= earlyStart && current <= earlyEnd;
    }

    private string CalculateCurrentStatus(DateTime startTime, string currentTimeStr)
    {
        TimeSpan current = TimeSpan.Parse(currentTimeStr);
        TimeSpan start = new TimeSpan(startTime.Hour, startTime.Minute, 0);
        TimeSpan diff = current - start;

        if (diff.TotalMinutes <= 5) return "출석";
        if (diff.TotalMinutes <= 15) return "지각";
        return "결석";
    }

    private void SaveToPlayerPrefs(string subject, string status)
    {
        PlayerPrefs.SetString("currentSubject", subject);
        PlayerPrefs.SetString("currentStatus", status);

        Debug.Log($"PlayerPrefs 저장 - 과목: {subject}, 상태: {status}");
    }
}
