using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class adminPopPanel : MonoBehaviour
{
    [Header("UI 연결")]
    public Button backButton;
    public GameObject popupPanel;
    public TMP_InputField passwordInput;
    public Button okButton;
    public Button cancelButton;
    public TextMeshProUGUI messageText;

    [Header("비밀번호 토글")]
    public Button toggleVisibilityButton;   // 버튼 자체가 눈 아이콘
    public Sprite eyeOpenSprite;            // 뜬 눈
    public Sprite eyeClosedSprite;          // 감은 눈

    private FirebaseAuth auth;
    private bool isHidden = true;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        popupPanel.SetActive(false);

        backButton.onClick.AddListener(OnBackPressed);
        okButton.onClick.AddListener(OnOkClicked);
        cancelButton.onClick.AddListener(() => popupPanel.SetActive(false));
        toggleVisibilityButton.onClick.AddListener(TogglePasswordVisibility);

        SetHidden(true); // 시작은 감은 눈 + 숨김
    }

    void OnBackPressed()
    {
        popupPanel.SetActive(true);
        passwordInput.text = "";
        messageText.text = "";
        passwordInput.Select();
    }

    void OnOkClicked()
    {
        string password = passwordInput.text;
        FirebaseUser user = auth.CurrentUser;

        if (user != null && !string.IsNullOrEmpty(password))
        {
            var credential = EmailAuthProvider.GetCredential(user.Email, password);
            user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("비밀번호 인증 성공");
                    SceneManager.LoadScene("ManagerProfile");
                }
                else
                {
                    Debug.LogWarning("인증 실패");
                    messageText.text = "비밀번호가 올바르지 않습니다.";
                    passwordInput.text = "";
                }
            });
        }
        else
        {
            messageText.text = "비밀번호를 입력하세요.";
        }
    }

    void TogglePasswordVisibility()
    {
        SetHidden(!isHidden);
    }

    void SetHidden(bool hide)
    {
        isHidden = hide;

        if (isHidden)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            toggleVisibilityButton.GetComponent<Image>().sprite = eyeClosedSprite;
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
            toggleVisibilityButton.GetComponent<Image>().sprite = eyeOpenSprite;
        }

        passwordInput.ForceLabelUpdate();
    }
}
