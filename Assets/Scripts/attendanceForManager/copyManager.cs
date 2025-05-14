using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class copyManager : MonoBehaviour
{
    public TMP_Text textToCopy; // 복사할 텍스트
    public Button copyButton;   // 복사 버튼

    void Start()
    {
        copyButton.onClick.AddListener(CopyText);
    }

    void CopyText()
    {
        GUIUtility.systemCopyBuffer = textToCopy.text;
        Debug.Log("복사됨: " + textToCopy.text);
    }
}