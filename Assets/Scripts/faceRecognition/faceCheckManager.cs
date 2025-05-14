using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using Firebase.Auth;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class faceCheckManager : MonoBehaviour
{
    public Button faceRegisterButton;
    public Sprite registeredSprite;
    public Sprite defaultSprite;

    void Start()
    {
        CheckFaceRegistration();
    }

    void CheckFaceRegistration()
    {
        string email = FirebaseAuth.DefaultInstance.CurrentUser.Email;
        string filePath = $"faces/{email}.jpg";

        FirebaseStorage.DefaultInstance
            .GetReference(filePath)
            .GetDownloadUrlAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    // ✅ 등록됨: 버튼 비활성화 + 등록 이미지
                    faceRegisterButton.image.sprite = registeredSprite;
                    faceRegisterButton.onClick.RemoveAllListeners();  // 클릭 이벤트 제거
                    // 버튼 비활성화 X → 이미지 투명도 유지

                }
                else
                {
                    // ❌ 등록 안됨: 버튼 활성화 + 기본 이미지 + 버튼 눌렀을 때 FaceRegister 씬으로 이동
                    faceRegisterButton.image.sprite = defaultSprite;
                    faceRegisterButton.interactable = true;
                    faceRegisterButton.onClick.RemoveAllListeners();
                    faceRegisterButton.onClick.AddListener(() =>
                    {
                        SceneManager.LoadScene("FaceRegister");
                    });
                }
            });
    }
}
