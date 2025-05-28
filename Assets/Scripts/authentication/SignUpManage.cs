using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
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

    public Button toggleVisibilityButton;
    public Sprite eyeOpenIcon;
    public Sprite eyeClosedIcon;
    private bool isPasswordVisible = false;

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

        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(OnPasswordChanged);

        if (toggleVisibilityButton != null)
            toggleVisibilityButton.onClick.AddListener(TogglePasswordVisibility);
    }

    void OnPasswordChanged(string newText)
    {
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
        else if (errorText.text == "비밀번호는 영문과 숫자의 조합으로 입력해주세요.")
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
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
            newUser.SendEmailVerificationAsync().ContinueWithOnMainThread(verifyTask =>
            {
                if (verifyTask.IsCompleted)
                {
                    errorText.text = "인증 메일이 전송되었습니다.";
                    StartCoroutine(CheckVerificationLoop());
                }
                else
                {
                    errorText.text = "인증 메일 전송에 실패했습니다.";
                }
            });
        });
    }

    IEnumerator CheckVerificationLoop()
    {
        int maxTries = 60; // 3초 x 60 = 3분
        for (int i = 0; i < maxTries; i++)
        {
            FirebaseUser user = auth.CurrentUser;
            if (user != null)
            {
                yield return new WaitForSeconds(3f);

                var task = user.ReloadAsync();
                yield return new WaitUntil(() => task.IsCompleted);

                if (user.IsEmailVerified)
                {
                    Debug.Log("이메일 인증 완료됨");
                    SaveUserToFirestore();
                    yield break;
                }
            }
            else
            {
                yield return null;
            }
        }

        errorText.text = "이메일 인증 시간이 초과되었습니다. 다시 시도해주세요.";
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

        // 1. users/{userType}/{email}/profile 저장
        db.Collection("users").Document(userType)
          .Collection(emailInput.text)
          .Document("profile")
          .SetAsync(userData).ContinueWithOnMainThread(storeTask =>
          {
              if (storeTask.IsCompleted)
              {
                  Debug.Log("users 경로 저장 완료");
                  SceneManager.LoadScene("loginScene");
              }
              else
              {
                  errorText.text = "Firestore 저장에 실패했습니다.";
              }
          });

        // 2. people/{name} 저장
        db.Collection("people").Document(nameInput.text.Trim())
          .SetAsync(userData).ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  Debug.Log("people 경로 저장 완료");
              }
              else
              {
                  Debug.LogError("people 저장 실패: " + task.Exception);
              }
          });
    }
}
