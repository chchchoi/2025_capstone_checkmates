using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchKeyManager : MonoBehaviour, IPointerDownHandler
{
    public TMP_InputField inputField;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inputField != null)
        {
            inputField.ActivateInputField();
        }
    }
}
