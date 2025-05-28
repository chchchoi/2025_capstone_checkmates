using UnityEngine;

public class keyborad : MonoBehaviour
{
    public RectTransform canvasRectTransform; // 캔버스의 RectTransform
    private Vector2 originalPosition;         // 초기 위치 저장
    private bool isKeyboardVisible = false;   // 키보드 활성화 상태

    void Start()
    {
        // RectTransform이 연결되지 않았을 경우 자동 할당
        if (canvasRectTransform == null)
            canvasRectTransform = GetComponent<RectTransform>();

        // 초기 위치 저장
        originalPosition = canvasRectTransform.anchoredPosition;
    }

    void Update()
    {
        // TouchScreenKeyboard.area로 키보드 영역 확인
        if (TouchScreenKeyboard.visible)
        {
            if (!isKeyboardVisible)
            {
                isKeyboardVisible = true;
                MoveCanvasUp();
            }
        }
        else
        {
            if (isKeyboardVisible)
            {
                isKeyboardVisible = false;
                ResetCanvasPosition();
            }
        }
    }

    private void MoveCanvasUp()
    {
        // 키보드 높이를 기반으로 캔버스를 위로 이동
        Rect keyboardArea = TouchScreenKeyboard.area;
        float keyboardHeight = keyboardArea.height;

        if (keyboardHeight > 0)
        {
            canvasRectTransform.anchoredPosition = originalPosition + new Vector2(0, keyboardHeight / 2 + 100f);
        }
        else
        {
            // 키보드 높이를 가져올 수 없는 경우, 임의의 값으로 이동
            canvasRectTransform.anchoredPosition = originalPosition + new Vector2(0, 400f);
        }
    }

    private void ResetCanvasPosition()
    {
        // 키보드가 사라지면 원래 위치로 복원
        canvasRectTransform.anchoredPosition = originalPosition;
    }
}
