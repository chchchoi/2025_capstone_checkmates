using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Storage;
using Firebase.Auth;
using Firebase.Extensions;

public class CameraCapture : MonoBehaviour
{
    public RawImage rawImage;
    public AspectRatioFitter aspectRatioFitter;
    public FaceRegister faceRegister;
    public Button switchCameraButton;

    private WebCamTexture webcamTexture;
    private WebCamDevice[] devices;
    private int currentCameraIndex = 0;

    void Start()
    {
        devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("사용 가능한 카메라가 없습니다.");
            return;
        }

        // 전면 카메라 우선 선택
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                currentCameraIndex = i;
                break;
            }
        }

        StartCoroutine(StartCamera(currentCameraIndex));

        if (switchCameraButton != null)
        {
            switchCameraButton.onClick.AddListener(SwitchCamera);
        }

        // ✅ 시작 시 스토리지 확인
        StartCoroutine(CheckImageCountAndMoveScene());
    }

    public void OnRegisterButtonClicked()
    {
        StartCoroutine(CaptureAndRegisterFace());
    }

    private IEnumerator LoadNextSceneWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene("PersonalProfile");
    }

    private IEnumerator StartCamera(int index)
    {
        rawImage.rectTransform.anchorMin = Vector2.zero;
        rawImage.rectTransform.anchorMax = Vector2.one;
        rawImage.rectTransform.offsetMin = Vector2.zero;
        rawImage.rectTransform.offsetMax = Vector2.zero;

        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        webcamTexture = new WebCamTexture(devices[index].name, 1920, 1080, 30);
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        yield return new WaitUntil(() => webcamTexture.width > 100);

        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectRatio = (float)webcamTexture.width / webcamTexture.height;
        }

        rawImage.rectTransform.sizeDelta = new Vector2(1500, 100);

        int rotation = webcamTexture.videoRotationAngle;
        rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);

        bool isFrontFacing = devices[index].isFrontFacing;
        bool isMirrored = webcamTexture.videoVerticallyMirrored;

        if (isFrontFacing)
        {
            rawImage.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            float scaleY = (rotation == 0 || rotation == 180) ? -1f : 1f;
            rawImage.rectTransform.localScale = new Vector3(1, scaleY, 1f);
            rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -rotation);
        }

        Debug.Log($"{devices[index].name}, front: {isFrontFacing}, rotation: {rotation}, mirrored: {isMirrored}");
    }

    private Texture2D RotateTexture90(Texture2D original)
    {
        int width = original.width;
        int height = original.height;

        Texture2D rotated = new Texture2D(height, width);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                rotated.SetPixel(j, width - i - 1, original.GetPixel(i, j));
            }
        }

        rotated.Apply();
        return rotated;
    }

    private IEnumerator CaptureAndRegisterFace()
    {
        yield return new WaitUntil(() => webcamTexture.width > 100);

        Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        Texture2D rotatedPhoto = RotateTexture90(photo);
        byte[] imageBytes = rotatedPhoto.EncodeToJPG();

        yield return StartCoroutine(faceRegister.RegisterStudent(imageBytes));

        // 등록 후 스토리지 이미지 개수 확인
        StartCoroutine(CheckImageCountAndMoveScene());
    }

    private void SwitchCamera()
    {
        if (devices.Length < 2) return;

        currentCameraIndex = (currentCameraIndex + 1) % devices.Length;
        StartCoroutine(StartCamera(currentCameraIndex));
    }

    // 추가된 기능: 스토리지에서 이미지 3개 이상일 때 씬 전환
    private IEnumerator CheckImageCountAndMoveScene()
    {
        string email = FirebaseAuth.DefaultInstance.CurrentUser.Email;
        int count = 0;

        for (int i = 1; i <= 5; i++) // 최대 5장까지 확인
        {
            string path = $"faces/{email}_{i}.jpg";
            var task = FirebaseStorage.DefaultInstance.GetReference(path).GetDownloadUrlAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsCompletedSuccessfully)
            {
                count++;
            }
        }

        if (count >= 3)
        {
            yield return new WaitForSeconds(3f);
            SceneManager.LoadScene("PersonalProfile");
        }
    }
}
