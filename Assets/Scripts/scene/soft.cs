using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class soft : MonoBehaviour
{
    public TMP_InputField field;

    void Start()
    {
        ApplyKeyboardSettings();
    }

    void ApplyKeyboardSettings()
    {
        bool usingPhysicalKeyboard = Keyboard.current != null && Keyboard.current.enabled;

        // 블루투스 키보드 연결됐으면 가상 키보드 숨김
        field.shouldHideMobileInput = usingPhysicalKeyboard;

        // 최신 Unity + TMP 버전에서만 작동 (없으면 제외)
#if UNITY_2023_1_OR_NEWER
        try
        {
            field.GetType().GetProperty("hideSoftKeyboard")?.SetValue(field, usingPhysicalKeyboard);
        }
        catch { Debug.Log("hideSoftKeyboard not available in this TMP version."); }
#endif

        Debug.Log("물리 키보드 감지됨: " + usingPhysicalKeyboard);
    }
}

