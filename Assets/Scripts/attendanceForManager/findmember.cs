using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindMember : MonoBehaviour
{
    public TMP_InputField searchInputField;
    public memberManage memberManager;

    void Start()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
        }
        else
        {
            Debug.LogError("searchInputField가 할당되지 않았습니다.");
        }
    }

    public void OnSearchButtonClicked()
    {
    if (searchInputField != null)
    {
        string text = searchInputField.text;
        OnSearchValueChanged(text);
    }
    }

    void OnSearchValueChanged(string searchText)
    {
    if (memberManager == null)
    {
        Debug.LogError("memberManager가 연결되지 않았습니다.");
        return;
    }

    string lowerSearchText = searchText.ToLower();

    foreach (KeyValuePair<string, GameObject> pair in memberManager.EmailToObject)
    {
        GameObject obj = pair.Value;
        if (obj == null) continue;

        TMP_Text userText = obj.transform.Find("userText")?.GetComponent<TMP_Text>();
        Button deleteButton = obj.GetComponentInChildren<Button>();

        if (userText != null)
        {
            string studentName = userText.text.ToLower();
            bool shouldShow = studentName.Contains(lowerSearchText);
            obj.SetActive(shouldShow);

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
                if (memberManager.ObjectToEmail.TryGetValue(obj, out string email))
                {
                    deleteButton.onClick.AddListener(() => memberManager.DeleteStudentByEmail(email));
                }
            }
        }
    }
    }
}