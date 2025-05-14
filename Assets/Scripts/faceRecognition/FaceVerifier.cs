// Azure Face API ���, PersonGroup ���� Detect + Verify ��� ���� ����
// Unity���� �� �⼮ üũ �뵵

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public class FaceDetectResponse
{
    public string faceId;
}

[Serializable]
public class VerifyRequest
{
    public string faceId1;
    public string faceId2;
}

[Serializable]
public class VerifyResponse
{
    public bool isIdentical;
    public float confidence;
}

public class FaceVerifier : MonoBehaviour
{
    public string subscriptionKey = "CIR6bdUlpQ4eaIkUdOZKrCvGC31q2Tmn9gIjK34T5ilFtCafpQOVJQQJ99BDACYeBjFXJ3w3AAAKACOGDz5V";
    public string endpoint = "https://ganadi-face.cognitiveservices.azure.com/";

    private string detectUrl => endpoint + "/face/v1.0/detect?returnFaceId=true";
    private string verifyUrl => endpoint + "/face/v1.0/verify";

    public IEnumerator DetectFace(byte[] imageBytes, Action<string> onSuccess)
    {
        UnityWebRequest request = new UnityWebRequest(detectUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(imageBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/octet-stream");
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", subscriptionKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            FaceDetectResponse[] faces = JsonHelper.FromJsonArray<FaceDetectResponse>(json);
            if (faces.Length > 0)
            {
                Debug.Log("Detect ����: faceId=" + faces[0].faceId);
                onSuccess?.Invoke(faces[0].faceId);
            }
            else
            {
                Debug.LogWarning("�� ����");
                onSuccess?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError("Detect ����: " + request.responseCode + " / " + request.error);
            onSuccess?.Invoke(null);
        }
    }

    public IEnumerator VerifyFaces(string faceId1, string faceId2, Action<VerifyResponse> onResult)
    {
        VerifyRequest body = new VerifyRequest { faceId1 = faceId1, faceId2 = faceId2 };
        string bodyJson = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        UnityWebRequest request = new UnityWebRequest(verifyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", subscriptionKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            VerifyResponse result = JsonUtility.FromJson<VerifyResponse>(request.downloadHandler.text);
            Debug.Log("Verify ����: ��ġ ���� = " + result.isIdentical + ", �ŷڵ� = " + result.confidence);
            onResult?.Invoke(result);
        }
        else
        {
            Debug.LogError("Verify ����: " + request.responseCode + " / " + request.error);
            onResult?.Invoke(null);
        }
    }
}

// Json �迭 �Ľ� ���� Ŭ����
public static class JsonHelper
{
    public static T[] FromJsonArray<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
