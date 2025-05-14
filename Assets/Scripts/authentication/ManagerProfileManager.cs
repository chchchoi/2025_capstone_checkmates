using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
public class ProfileManager : MonoBehaviour
{
    public TMP_Text nameText;  // 사용자 이름을 표시할 Text UI
    public TMP_Text affiliationText;  // 사용자 소속을 표시할 Text UI

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        // Firebase 인스턴스를 초기화
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // 로그인된 사용자의 프로필 데이터를 로드
        LoadUserProfile();
    }

    async void LoadUserProfile()
    {
        // 현재 로그인된 사용자
        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            string userEmail = user.Email;  // 로그인한 사용자 이메일
            string userType = "manager";  // userType을 "manager"로 고정

            // userType에 맞는 서브컬렉션 검색
            DocumentSnapshot userDocSnapshot = await db.Collection("users")
                .Document(userType)  // userType이 "manager"
                .Collection(userEmail)  // 이메일을 문서 ID로 사용
                .Document("profile")  // "profile" 문서 조회
                .GetSnapshotAsync();

            if (userDocSnapshot.Exists)
            {
                // 프로필 데이터를 찾았다면 이름과 소속을 UI에 설정
                string name = userDocSnapshot.GetValue<string>("name");
                string affiliation = userDocSnapshot.GetValue<string>("affiliation");

                nameText.text = name;
                affiliationText.text = affiliation;
            }
            else
            {
                Debug.LogError("프로필 데이터를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
        }
    }
}
