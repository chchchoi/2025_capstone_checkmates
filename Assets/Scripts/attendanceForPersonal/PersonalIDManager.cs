using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class PersonalIDManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    public TMP_Text IDText;  // 사용자 이메일을 표시할 Text UI

    // Start는 MonoBehaviour가 생성된 후 첫 번째 Update 이전에 한 번 호출됩니다.
    void Start()
    {
        // Firebase 인스턴스 초기화
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // 로그인된 사용자의 ID 불러오기
        LoadUserID();
    }

    // 사용자 이메일 정보를 Firestore에서 불러오는 비동기 함수
    async void LoadUserID()
    {
        // 현재 로그인된 사용자 정보 가져오기
        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            string userEmail = user.Email;  // 로그인된 사용자의 이메일
            string userType = "personal";   // 사용자 유형 (예: "manager" 또는 "personal")

            // Firestore에서 해당 사용자 문서 조회
            DocumentSnapshot userDocSnapshot = await db.Collection("users")
                .Document(userType)               // 사용자 유형 하위 문서 접근
                .Collection(userEmail)            // 이메일을 기준으로 한 서브 컬렉션 접근
                .Document("profile")              // "profile" 문서 접근
                .GetSnapshotAsync();

            if (userDocSnapshot.Exists)
            {
                // 문서가 존재하면 이메일 값을 가져와 UI에 표시
                string email = userDocSnapshot.GetValue<string>("email");
                IDText.text = email;
            }
            else
            {
                Debug.LogError("사용자 프로필 문서를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
        }
    }

    // 매 프레임마다 호출되지만 현재는 사용하지 않음
    void Update()
    {
        
    }
}