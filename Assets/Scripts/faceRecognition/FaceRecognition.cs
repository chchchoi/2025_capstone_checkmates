using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Firebase.Auth;

[System.Serializable]
public class ServerResponse
{
    public string message;
    public string error;
    public string email;
    public string status;
    public float similarity;
}

public class FaceRecognition : MonoBehaviour
{
    private string serverUrl = "http://223.194.131.148:5050/check";

    public TextMeshProUGUI resultText;
    public TMP_Text subjectText;
    public GameObject statusPanel;  // ✅ 패널 오브젝트

    public IEnumerator CheckAttendance(byte[] imageBytes)
    {
        Debug.Log("[CheckAttendance] 호출됨");
        string managerEmail = FirebaseAuth.DefaultInstance.CurrentUser?.Email;
        string currentSubject = PlayerPrefs.GetString("currentSubject", "");
        string currentStatus = PlayerPrefs.GetString("currentStatus", "");

        if (string.IsNullOrEmpty(managerEmail) || string.IsNullOrEmpty(currentSubject) || string.IsNullOrEmpty(currentStatus))
        {
            ShowMessage("필수 정보 없음");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("manager", managerEmail);
        form.AddField("currentSubject", currentSubject);
        form.AddField("currentStatus", currentStatus);
        form.AddBinaryData("image", imageBytes, "face.jpg", "image/jpeg");

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            yield return request.SendWebRequest();

            string json = request.downloadHandler.text;
            Debug.Log("서버 응답: " + json);

            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);

            // ✅ 서버 연결은 성공했지만 HTTP 에러라면 서버가 준 error 메시지 사용
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = !string.IsNullOrEmpty(response?.error) ? response.error : request.error;
                ShowMessage(errorMsg);
                yield break;
            }

            // ✅ 성공 응답 처리
            string message = !string.IsNullOrEmpty(response.message) ? response.message : response.error;
            ShowMessage(message);
        }

    }

    private void ShowMessage(string message)
    {
        resultText.text = message;

        if (statusPanel != null)
        {
            statusPanel.SetActive(true);  // ✅ 패널 보이기
            StartCoroutine(HidePanelAfterSeconds(5f));  // ✅ 5초 후 숨기기
        }
    }

    private IEnumerator HidePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        statusPanel.SetActive(false);  // ✅ 패널 숨기기
    }
}
