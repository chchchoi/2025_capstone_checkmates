using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Linq;


public class FaceRegister : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject statusPanel;

    private string url = "http://127.0.0.1:5050/register"; // server adress
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    public IEnumerator RegisterStudent(byte[] imageData)
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인된 사용자가 없습니다.");
            ShowMessage("로그인이 필요합니다.");
            yield break;
        }

        string email = user.Email;
        Debug.Log("등록 요청: 이메일=" + email + ", 이미지 크기=" + imageData.Length);

        // 1. 사람 이름 Firestore에서 가져오기
        Task<string> nameTask = GetUserNameFromFirestore(email);
        yield return new WaitUntil(() => nameTask.IsCompleted);

        string userName = nameTask.Result ?? email; // 실패 시 이메일 표시

        // 2. 서버에 얼굴 등록 요청
        WWWForm form = new WWWForm();
        form.AddField("email", email);
        form.AddBinaryData("image", imageData, "face.jpg", "image/jpeg");

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.useHttpContinue = false;
            yield return www.SendWebRequest();

            string json = www.downloadHandler.text;
            Debug.Log("서버 응답: " + json);

            if (www.result == UnityWebRequest.Result.Success)
            {
                ShowMessage($"{userName}님, 얼굴 등록에 성공하였습니다.");
            }
            else
            {
                Debug.LogError("등록 실패: " + www.error);
                Debug.LogError("서버 응답 내용: " + json);

                string errorMessage = www.error;

                try
                {
                    ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);
                    if (!string.IsNullOrEmpty(response?.error))
                    {
                        errorMessage = response.error;
                    }
                }
                catch
                {
                    Debug.LogWarning("[RegisterStudent] JSON 파싱 실패");
                }

                ShowMessage(errorMessage);
            }
        }

        yield return null;
    }

    // Firestore에서 이름 조회
    private async Task<string> GetUserNameFromFirestore(string email)
    {
        try
        {
            Query query = db.Collection("people").WhereEqualTo("email", email);
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            var documents = querySnapshot.Documents.ToList();

            if (documents.Count == 0)
            {
                Debug.LogWarning($"[Firestore] email={email} 에 해당하는 문서가 없습니다.");
                return null;
            }

            if (documents.Count > 1)
            {
                Debug.LogError($"[Firestore] email={email} 이 중복된 문서 {documents.Count}개에 존재합니다.");
                return null;
            }

            DocumentSnapshot doc = documents[0];

            if (doc.ContainsField("name"))
            {
                return doc.GetValue<string>("name");
            }
            else
            {
                Debug.LogWarning($"[Firestore] email={email} 문서에 name 필드가 없습니다.");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] 이름 조회 중 오류: {e.Message}");
            return null;
        }
    }

    private void ShowMessage(string message)
    {
        statusText.text = message;

        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
            StartCoroutine(HidePanelAfterSeconds(5f));
        }
    }

    private IEnumerator HidePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        statusPanel.SetActive(false);
    }
}
