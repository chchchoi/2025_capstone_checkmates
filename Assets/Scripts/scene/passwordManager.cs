using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class PasswordManager : MonoBehaviour
{
    public TMP_InputField passwordInput;
    public TMP_Text warningText;           
    public Button toggleVisibilityButton;
    public Sprite eyeOpenIcon;
    public Sprite eyeClosedIcon;

    private bool isPasswordVisible = false;

    void Start()
    {
        passwordInput.onValueChanged.AddListener(OnPasswordChanged);
        toggleVisibilityButton.onClick.AddListener(TogglePasswordVisibility);

        warningText.gameObject.SetActive(false); // 초기 비활성화
    }

    void OnPasswordChanged(string newText)
    {
        string filtered = Regex.Replace(newText, @"[\uAC00-\uD7A3]", "");
        if (filtered != newText)
        {
            warningText.text = "비밀번호는 영문과 숫자만 입력해주세요.";
            warningText.gameObject.SetActive(true);

            passwordInput.text = filtered;
            passwordInput.caretPosition = filtered.Length;
        }
        else
        {
            warningText.gameObject.SetActive(false);
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
}
