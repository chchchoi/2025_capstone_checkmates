using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class faceCheckManager : MonoBehaviour
{
    public Button faceRegisterButton;
    public Sprite registeredSprite;
    public Sprite defaultSprite;

    private int foundImageCount = 0;
    private int checkedCount = 0;

    void Start()
    {
        CheckFaceRegistration();
    }

    void CheckFaceRegistration()
    {
        string email = FirebaseAuth.DefaultInstance.CurrentUser.Email;

        for (int i = 1; i <= 3; i++)  // ✅ 최대 3개만 확인
        {
            string filePath = $"faces/{email}_{i}.jpg";

            FirebaseStorage.DefaultInstance
                .GetReference(filePath)
                .GetDownloadUrlAsync()
                .ContinueWithOnMainThread(task =>
                {
                    checkedCount++;

                    if (task.IsCompletedSuccessfully)
                    {
                        foundImageCount++;
                    }

                    // ✅ 모든 확인 끝났을 때만 판단
                    if (checkedCount == 3)
                    {
                        if (foundImageCount >= 3)
                        {
                            // 등록 완료 처리
                            faceRegisterButton.image.sprite = registeredSprite;
                            faceRegisterButton.onClick.RemoveAllListeners();
                            // 버튼은 계속 활성화 상태로 유지 (그래야 흐려지지 않음)
                            faceRegisterButton.interactable = true;  
                        }
                        else
                        {
                            // 등록 안됨 처리
                            SetupRegisterButton();
                        }
                    }
                });
        }
    }

    void SetupRegisterButton()
    {
        faceRegisterButton.image.sprite = defaultSprite;
        faceRegisterButton.interactable = true;
        faceRegisterButton.onClick.RemoveAllListeners();
        faceRegisterButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("FaceRegister");
        });
    }
}
