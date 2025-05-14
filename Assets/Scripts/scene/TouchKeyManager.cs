using UnityEngine;
using UnityEngine.EventSystems;

public class TouchKeyManager : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
