using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SignUpManage : MonoBehaviour
{
    public TMP_InputField emailInput, passwordInput, nameInput, affiliationInput, phoneNumberInput;
    public GameObject personalButton, managerButton;
    public Image displayImage;
    public Sprite personalSprite, managerSprite;
    public TextMeshProUGUI personalButtonText, managerButtonText;
    public TextMeshProUGUI errorText;
    public Button registerButton;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userType = "manager";

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        displayImage.sprite = managerSprite;
        UpdateButtonTextColor();
        errorText.text = "";

        if (registerButton != null)
            registerButton.onClick.AddListener(RegisterUser);

        if (personalButton != null)
            personalButton.GetComponent<Button>().onClick.AddListener(SelectPersonal);

        if (managerButton != null)
            managerButton.GetComponent<Button>().onClick.AddListener(SelectManager);
    }

    public void SelectPersonal()
    {
        userType = "personal";
        displayImage.sprite = personalSprite;
        UpdateButtonTextColor();
    }

    public void SelectManager()
    {
        userType = "manager";
        displayImage.sprite = managerSprite;
        UpdateButtonTextColor();
    }

    void UpdateButtonTextColor()
    {
        personalButtonText.color = (userType == "personal") ? Color.gray : Color.white;
        managerButtonText.color = (userType == "manager") ? Color.gray : Color.white;
    }

    bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    bool IsValidPhoneNumber(string phone)
    {
        return Regex.IsMatch(phone, @"^\d+$");
    }

    bool IsAnyFieldEmpty()
    {
        return string.IsNullOrEmpty(emailInput.text) ||
               string.IsNullOrEmpty(passwordInput.text) ||
               string.IsNullOrEmpty(nameInput.text) ||
               string.IsNullOrEmpty(affiliationInput.text) ||
               string.IsNullOrEmpty(phoneNumberInput.text);
    }

    public void RegisterUser()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (IsAnyFieldEmpty())
        {
            errorText.text = "모든 항목을 입력해야 합니다.";
            return;
        }

        if (!IsValidEmail(email))
        {
            errorText.text = "올바른 이메일 형식이 아닙니다.";
            return;
        }

        if (!IsValidPhoneNumber(phoneNumberInput.text))
        {
            errorText.text = "전화번호는 숫자만 입력하세요.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                errorText.text = "회원가입에 실패했습니다.";
                return;
            }

            FirebaseUser newUser = task.Result.User;

            // 이메일 인증 없이 바로 저장
            errorText.text = "회원가입이 완료되었습니다.";
            SaveUserToFirestore();
        });
    }

    void SaveUserToFirestore()
    {
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "userType", userType },
            { "email", emailInput.text },
            { "name", nameInput.text },
            { "affiliation", affiliationInput.text },
            { "phoneNumber", phoneNumberInput.text }
        };

        // ✅ 1. users/{userType}/{email}/profile 저장
        db.Collection("users").Document(userType)
          .Collection(emailInput.text)
          .Document("profile")
          .SetAsync(userData).ContinueWithOnMainThread(storeTask =>
          {
              if (storeTask.IsCompleted)
              {
                  Debug.Log("✅ users 경로 저장 완료");
                  SceneManager.LoadScene("loginScene");
              }
              else
              {
                  errorText.text = "Firestore 저장에 실패했습니다.";
              }
          });

        // ✅ 2. people/{name} 저장
        db.Collection("people").Document(nameInput.text.Trim())
          .SetAsync(userData).ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  Debug.Log("✅ people 경로 저장 완료");
              }
              else
              {
                  Debug.LogError("❌ people 저장 실패: " + task.Exception);
              }
          });
    }
}
