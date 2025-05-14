using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;
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

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userType = "manager"; // 기본값: 기관

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
        return System.Text.RegularExpressions.Regex.IsMatch(trimmedEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public async void LoginUser()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            errorText.text = "이메일과 비밀번호 둘 다 입력해주세요.";
            return;
        }

        if (!IsValidEmail(email))
        {
            errorText.text = "올바르지 않은 이메일 형식입니다.";
            return;
        }

        errorText.text = "";

        // Firebase Authentication으로 이메일 로그인 시도
        try
        {
            var authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = authResult.User; // FirebaseUser 객체 가져오기
            Debug.Log("Logged in as: " + user.Email);

            // Firestore에서 users 컬렉션을 검색하여 userType에 맞는 서브컬렉션을 찾음
            bool userFound = false;

            // userType에 맞는 서브컬렉션 검색
            var userDocSnapshot = await db.Collection("users")
                .Document(userType)  // 선택한 userType에 해당하는 서브컬렉션 (personal 또는 manager)
                .Collection(user.Email)  // 이메일을 문서 ID로 하여 해당 문서 확인
                .Document("profile")  // profile 문서 조회
                .GetSnapshotAsync();

            if (userDocSnapshot.Exists)
            {
                // 해당 문서에서 userType을 확인하여 씬 전환
                var userData = userDocSnapshot.ToDictionary();
                string storedUserType = userData["userType"].ToString();

                if (storedUserType == "manager")
                {
                    SceneManager.LoadScene("ManagerProfile");
                }
                else if (storedUserType == "personal")
                {
                    SceneManager.LoadScene("PersonalProfile");
                }
                else
                {
                    errorText.text = "사용자 타입을 확인해주세요.";
                }

                userFound = true;
            }

            if (!userFound)
            {
                errorText.text = "사용자를 찾을 수 없거나 알맞은 타입인지 확인해주세요.";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Login Failed: " + ex.Message);
            errorText.text = "이메일 혹은 비밀번호가 올바르지 않습니다.";
        }
    }
}
