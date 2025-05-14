using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            Debug.LogError("❌ 사용 가능한 카메라가 없습니다.");
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
    }

    private IEnumerator StartCamera(int index)
    {

        // 화면 좀 줄이기 ( 테스트 후 삭제 혹은 추가 예정 )  
        rawImage.rectTransform.anchorMin = Vector2.zero;
        rawImage.rectTransform.anchorMax = Vector2.one;
        rawImage.rectTransform.offsetMin = Vector2.zero;
        rawImage.rectTransform.offsetMax = Vector2.zero;
        //

        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        //webcamTexture = new WebCamTexture(devices[index].name);
        webcamTexture = new WebCamTexture(devices[index].name, 1920, 1080, 30); // 가로 1280, 세로 720, 프레임 30

        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        yield return new WaitUntil(() => webcamTexture.width > 100);

        // ✅ 비율 설정
        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectRatio = (float)webcamTexture.width / webcamTexture.height;
        }

        // ✅ 크기 고정
        rawImage.rectTransform.sizeDelta = new Vector2(1500, 100);

        // ✅ 회전 보정 (기기에서 제공하는 회전값 사용)
        int rotation = webcamTexture.videoRotationAngle;
        rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);

        // ✅ 반전 보정
        bool isFrontFacing = devices[index].isFrontFacing;
        bool isMirrored = webcamTexture.videoVerticallyMirrored;
        if (isFrontFacing)
        {
            int rotations = 90; // 🔥 테스트용으로 90으로 강제 설정
            float scaleY = (rotations == 0 || rotations == 180) ? -1f : 1f;
            // 전면 카메라는 좌우 반전이 기본이므로 반전 처리
            rawImage.rectTransform.localScale = new Vector3(-1, 1, 1); // 좌우 반전
        }
        else
        {
            // ✅ 후면 카메라: 회전 적용 + 상하 반전 조건
            float scaleY = (rotation == 0 || rotation == 180) ? -1f : 1f;
            rawImage.rectTransform.localScale = new Vector3(1, scaleY, 1f);
            rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -rotation); // 🔥 후면만 적용
        }

        // ✅ 디버깅 로그 (원하면 지워도 됨)
        Debug.Log($"📷 {devices[index].name}, front: {isFrontFacing}, rotation: {rotation}, mirrored: {isMirrored}");
    }

    public void OnRegisterButtonClicked()
    {
        StartCoroutine(CaptureAndRegisterFace());
    }

    // 사진 로테이션 후 저장 ( 삭제 혹은 추가 예정 ) 
    private Texture2D RotateTexture90(Texture2D original)
    {
        int width = original.width;
        int height = original.height;

        Texture2D rotated = new Texture2D(height, width); // 너비/높이 바뀜

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

        // 🔁 90도 오른쪽으로 회전
        Texture2D rotatedPhoto = RotateTexture90(photo);

        // ✅ 회전된 텍스처를 JPG로 변환해야 회전된 이미지가 저장됨!
        byte[] imageBytes = rotatedPhoto.EncodeToJPG();

        yield return StartCoroutine(faceRegister.RegisterStudent(imageBytes));
    }

    private void SwitchCamera()
    {
        if (devices.Length < 2) return;

        currentCameraIndex = (currentCameraIndex + 1) % devices.Length;
        StartCoroutine(StartCamera(currentCameraIndex));
    }
}