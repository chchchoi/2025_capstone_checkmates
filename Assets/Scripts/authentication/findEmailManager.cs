using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;

public class FindEmailManager : MonoBehaviour
{
    public TMP_InputField nameInput, numberInput;
    public GameObject personalButton, managerButton;
    public Image displayImage; // 변경할 이미지
    public Sprite personalSprite, managerSprite;
    public TextMeshProUGUI personalButtonText, managerButtonText;
    public TextMeshProUGUI errorText;

    // 이메일을 표시할 새로운 게임 오브젝트
    public GameObject findObject, foundObject;
    public TextMeshProUGUI emailUserText, emailText;

    // Find Email 버튼을 public으로 선언
    public Button findEmailButton; // 이 버튼을 에디터에서 연결할 수 있게

    private FirebaseFirestore db;
    private string userType = "manager"; // Default: manager

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Button event listeners
        if (personalButton != null)
            personalButton.GetComponent<Button>().onClick.AddListener(SelectPersonal);

        if (managerButton != null)
            managerButton.GetComponent<Button>().onClick.AddListener(SelectManager);

        // Find Email 버튼 클릭 이벤트 연결
        if (findEmailButton != null)
            findEmailButton.onClick.AddListener(FindEmail);

        // Default selection
        SelectManager();
    }

    public void SelectPersonal()
    {
        userType = "personal";
        displayImage.sprite = personalSprite;
        Debug.Log("Selected: Personal");
        UpdateButtonTextColor();
    }

    public void SelectManager()
    {
        userType = "manager";
        displayImage.sprite = managerSprite;
        Debug.Log("Selected: Manager");
        UpdateButtonTextColor();
    }

    void UpdateButtonTextColor()
    {
        if (userType == "personal")
        {
            personalButtonText.color = Color.white;
            managerButtonText.color = Color.black;
        }
        else
        {
            personalButtonText.color = Color.black;
            managerButtonText.color = Color.white;
        }
    }

    public async void FindEmail()
    {
        string name = nameInput.text.Trim();
        string number = numberInput.text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(number))
        {
            errorText.text = "이름과 전화번호 둘다 입력해주세요.";
            return;
        }

        // Firestore에서 "people" 컬렉션을 가져와서 선택된 유저 타입에 맞는 데이터를 찾기
        DocumentReference profileRef = db.Collection("people")
            .Document(name); // userType에 맞는 도큐먼트로 가져오지 않고 이름으로 가져오는 방식

        DocumentSnapshot snapshot = await profileRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            errorText.text = "이름을 찾을 수 없습니다.";
            return;
        }

        var userData = snapshot.ToDictionary();
        string userTypeFromDB = userData.ContainsKey("userType") ? userData["userType"].ToString() : "";

        // 선택한 userType과 데이터베이스에서 가져온 userType이 다르면 에러 메시지
        if (userType != userTypeFromDB)
        {
            errorText.text = "잘못된 사용자 타입입니다."; // 타입이 다르면 에러 메시지
            return;
        }

        if (userData.ContainsKey("phoneNumber") && userData["phoneNumber"].ToString() == number)
        {
            // 이메일 값이 없으면 처리
            if (!userData.ContainsKey("email") || string.IsNullOrEmpty(userData["email"].ToString()))
            {
                errorText.text = "이 사용자에 대한 이메일을 찾을 수 없습니다.";
                return;
            }

            string email = userData["email"].ToString(); // 이메일을 문자열로 가져오기

            // 이메일 형식 검사 없이 그냥 표시
            if (string.IsNullOrEmpty(email))
            {
                errorText.text = "이 사용자의 이메일이 비어 있습니다.";
                return;
            }

            Debug.Log("Email found: " + email);

            // 이메일을 찾았다면, 기존 오브젝트 비활성화하고 새로운 오브젝트 표시
            findObject.SetActive(false);
            foundObject.SetActive(true);

            emailText.text = email;
        }
        else
        {
            errorText.text = "입력한 전화번호가 기록과 일치하지 않습니다.";
        }
    }
}
