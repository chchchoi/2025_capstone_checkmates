using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;

public class FindPWManager: MonoBehaviour
{
    public TMP_InputField emailInput, nameInput;
    public GameObject personalButton, managerButton;
    public Image displayImage; // 변경할 이미지
    public Sprite personalSprite, managerSprite;
    public TextMeshProUGUI personalButtonText, managerButtonText;
    public TextMeshProUGUI errorText;
    public Button resetButton;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userType = "manager"; // Default: manager

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Button event listeners
        if (resetButton != null)
            resetButton.onClick.AddListener(() => SendPasswordReset());

        if (personalButton != null)
            personalButton.GetComponent<Button>().onClick.AddListener(SelectPersonal);

        if (managerButton != null)
            managerButton.GetComponent<Button>().onClick.AddListener(SelectManager);

        // Default selection
        SelectManager();
    }

    public void SelectPersonal()
    {
        userType = "personal";
        displayImage.sprite = personalSprite;
        Debug.Log("Selected: Personal");
        UpdateButtonTextColor();
    }

    public void SelectManager()
    {
        userType = "manager";
        displayImage.sprite = managerSprite;
        Debug.Log("Selected: Manager");
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
        return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public async void SendPasswordReset()
    {
        string email = emailInput.text.Trim();
        string name = nameInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
        {
            errorText.text = "이메일과 이름 둘 다 입력해주세요.";
            return;
        }

        if (!IsValidEmail(email))
        {
            errorText.text = "올바르지 않은 이메일 형식입니다.";
            return;
        }

        DocumentReference profileRef = db.Collection("users")
            .Document(userType)
            .Collection(email)
            .Document("profile");

        DocumentSnapshot snapshot = await profileRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            errorText.text = "이메일을 찾을 수 없습니다.";
            return;
        }

        var userData = snapshot.ToDictionary();
        if (userData.ContainsKey("name") && userData["name"].ToString() == name)
        {
            await auth.SendPasswordResetEmailAsync(email);
            Debug.Log("Password reset email sent: " + email);
            errorText.text = "이메일로 비밀번호 초기화 메일을 전송하였습니다.";
        }
        else
        {
            errorText.text = "입력한 이름이 기록과 일치하지 않습니다.";
        }
    }
}
