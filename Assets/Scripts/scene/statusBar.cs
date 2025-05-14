using UnityEngine;
using System.Collections;

public class StatusBar : MonoBehaviour
    {
        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        ApplicationChrome.statusBarState = ApplicationChrome.States.TranslucentOverContent;

#endif
    }
}
