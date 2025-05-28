using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileTouchCaret : MonoBehaviour, IPointerDownHandler
{
    public TMP_InputField inputField;

    public void OnPointerDown(PointerEventData eventData)
    {
        // TMP_InputField 내부의 텍스트 영역에서 터치 위치 계산
        Camera cam = null; // UI가 Overlay라면 null, Camera라면 해당 카메라 넣기
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inputField.textComponent.rectTransform,
            eventData.position,
            cam,
            out localMousePos
        );

        int index = TMP_TextUtilities.GetCursorIndexFromPosition(inputField.textComponent, localMousePos, cam);
        inputField.caretPosition = index;
    }
}
