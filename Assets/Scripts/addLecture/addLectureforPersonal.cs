using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using Firebase.Extensions;

public class addLectureforPersonal : MonoBehaviour
{
    public TMP_InputField subjectCodeInput;  // 과목 코드 입력 필드
    public Button addButton;  // 추가 버튼
    public Button closeButton;  // 닫기 버튼
    public GameObject dialogPanel;  // 닫을 대상 패널 오브젝트 

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

        addButton.onClick.AddListener(AddLectureToStudent);
        closeButton.onClick.AddListener(CloseDialog);
    }

    void AddLectureToStudent()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            return;
        }

        string subjectCode = subjectCodeInput.text;

        db.Collection("subjects")
            .WhereEqualTo("lectureCode", subjectCode)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Count > 0)
                {
                    foreach (var subjectDoc in task.Result.Documents)
                    {
                        string subjectId = subjectDoc.Id;

                        DocumentReference subjectRef = db.Collection("subjects").Document(subjectId);

                        subjectRef.UpdateAsync(new Dictionary<string, object>
                        {
                            { "personal", FieldValue.ArrayUnion(userEmail) }
                        }).ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.IsCompletedSuccessfully)
                            {
                                DocumentReference dummyDocRef = db
                                    .Collection("subjects")
                                    .Document(subjectId)
                                    .Collection(userEmail)
                                    .Document("init");

                                dummyDocRef.SetAsync(new Dictionary<string, object>())
                                    .ContinueWithOnMainThread(dummyTask =>
                                    {
                                        if (dummyTask.IsCompletedSuccessfully)
                                        {
                                            Debug.Log($"{subjectId} 등록 성공");
                                            CloseDialog();  // 정상적으로 판넬 닫기
                                        }
                                        else
                                        {
                                            Debug.LogError("init 문서 생성 실패");
                                        }
                                    });
                            }
                            else
                            {
                                Debug.LogError("이메일 추가 실패");
                            }
                        });

                        break;  // 첫 과목만 처리
                    }
                }
                else
                {
                    Debug.LogError("과목 코드 일치 항목 없음");
                }
            });
    }

    void CloseDialog()
    {
        Debug.Log("CloseDialog 호출됨");
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("dialogPanel이 할당되지 않았습니다.");
        }
    }
}
