using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLogoutManager : MonoBehaviour
{
    public Button logoutButton;  // 로그아웃 버튼을 연결할 변수

    void Start()
    {
        // 로그아웃 버튼 클릭 시 로그아웃 함수 호출
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(() =>
            {
                Debug.Log("로그아웃 버튼 클릭됨");
                Logout();
            });
        }
        else
        {
            Debug.LogError("로그아웃 버튼이 할당되지 않았습니다!");
        }
    }

    void Logout()
    {
        // 로그아웃을 위한 Firebase 인증 상태 확인
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            FirebaseAuth.DefaultInstance.SignOut();
            Debug.Log("Firebase 로그아웃 완료");

            // 로그아웃 후 로그인 화면으로 이동
            SceneManager.LoadScene("loginScene");  // 로그인 씬 이름 확인 필수
        }
        else
        {
            Debug.Log("이미 로그아웃 상태입니다.");
        }
    }
}
