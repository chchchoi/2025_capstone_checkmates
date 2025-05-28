using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputTest : MonoBehaviour
{
    private static KeyboardInputTest instance;

    void Awake()
    {
        // 중복 방지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);  // 씬 전환 시에도 파괴되지 않음
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Debug.Log("엔터 키 눌림");
            }

            if (Keyboard.current.aKey.isPressed)
            {
                Debug.Log("A 키 누르고 있음");
            }
        }
        else
        {
            Debug.Log("키보드 없음");
        }
    }
}
