using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class LoginManage : MonoBehaviour
{
    public TMP_InputField emailInput, passwordInput;
    public GameObject personalButton, managerButton;
    public Image displayImage;
    public Sprite personalSprite, managerSprite;
    public TextMeshProUGUI personalButtonText, managerButtonText;
    public TextMeshProUGUI errorText;
    public Button loginButton;
    public Button toggleVisibilityButton; // 👁️ 아이콘 버튼
    public Sprite eyeOpenIcon;
    public Sprite eyeClosedIcon;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userType = "manager"; // 기본값: 기관
    private bool isPasswordVisible = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        displayImage.sprite = managerSprite;
        UpdateButtonTextColor();
        errorText.text = "";

        if (loginButton != null)
            loginButton.onClick.AddListener(() => LoginUser());

        if (personalButton != null)
            personalButton.GetComponent<Button>().onClick.AddListener(SelectPersonal);

        if (managerButton != null)
            managerButton.GetComponent<Button>().onClick.AddListener(SelectManager);

        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(OnPasswordChanged);

        if (toggleVisibilityButton != null)
            toggleVisibilityButton.onClick.AddListener(TogglePasswordVisibility);
    }

    void OnPasswordChanged(string newText)
    {
        // 한글이 포함되었는지 검사 (완성형 + 초성 포함 전체 검사)
        if (Regex.IsMatch(newText, @"[\u1100-\u11FF\u3130-\u318F\uAC00-\uD7A3]"))
        {
            errorText.text = "비밀번호는 영문과 숫자의 조합으로 입력해주세요.";
            errorText.gameObject.SetActive(true);

            string filtered = Regex.Replace(newText, @"[\u1100-\u11FF\u3130-\u318F\uAC00-\uD7A3]", "");
            passwordInput.onValueChanged.RemoveListener(OnPasswordChanged);
            passwordInput.text = filtered;
            passwordInput.caretPosition = filtered.Length;
            passwordInput.onValueChanged.AddListener(OnPasswordChanged);
        }
        else
        {
            if (errorText.text == "비밀번호는 영문과 숫자의 조합으로 입력해주세요.")
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }
    }

    void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        ApplyPasswordMask();
    }

    void ApplyPasswordMask()
    {
        passwordInput.contentType = isPasswordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        if (toggleVisibilityButton.image != null)
        {
            toggleVisibilityButton.image.sprite = isPasswordVisible ? eyeOpenIcon : eyeClosedIcon;
        }
    }

    public void SelectPersonal()
    {
        userType = "personal";
        displayImage.sprite = personalSprite;
        Debug.Log("You chose Individual");
        UpdateButtonTextColor();
    }

    public void SelectManager()
    {
        userType = "manager";
        displayImage.sprite = managerSprite;
        Debug.Log("You chose Institution");
        UpdateButtonTextColor();
    }

    void UpdateButtonTextColor()
    {
        if (userType == "personal")
        {
            personalButtonText.color = Color.white;
            managerButtonText.color = Color.black;
        }
        else
        {
            personalButtonText.color = Color.black;
            managerButtonText.color = Color.white;
        }
    }

    bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();
        return Regex.IsMatch(trimmedEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public async void LoginUser()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            errorText.text = "이메일과 비밀번호 둘 다 입력해주세요.";
            errorText.gameObject.SetActive(true);
            return;
        }

        if (!IsValidEmail(email))
        {
            errorText.text = "올바르지 않은 이메일 형식입니다.";
            errorText.gameObject.SetActive(true);
            return;
        }

        errorText.text = "";
        errorText.gameObject.SetActive(false);

        try
        {
            var authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = authResult.User;
            Debug.Log("Logged in as: " + user.Email);

            bool userFound = false;

            var userDocSnapshot = await db.Collection("users")
                .Document(userType)
                .Collection(user.Email)
                .Document("profile")
                .GetSnapshotAsync();

            if (userDocSnapshot.Exists)
            {
                var userData = userDocSnapshot.ToDictionary();
                string storedUserType = userData["userType"].ToString();

                if (storedUserType == "manager")
                    SceneManager.LoadScene("ManagerProfile");
                else if (storedUserType == "personal")
                    SceneManager.LoadScene("PersonalProfile");
                else
                    errorText.text = "사용자 타입을 확인해주세요.";

                errorText.gameObject.SetActive(true);
                userFound = true;
            }

            if (!userFound)
            {
                errorText.text = "사용자를 찾을 수 없거나 알맞은 타입인지 확인해주세요.";
                errorText.gameObject.SetActive(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Login Failed: " + ex.Message);
            errorText.text = "이메일 혹은 비밀번호가 올바르지 않습니다.";
            errorText.gameObject.SetActive(true);
        }
    }
}
