using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform[] _cameraPoint;

    [Header("Cameras")]
    [SerializeField] private Camera _mainCam;           
    [SerializeField] private Camera _uiCam;           
    [SerializeField] private Camera _captureCamera;   

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool moveCaptureCamTransform = true; 

    [Header("Capture Settings")]
    [SerializeField] private int overrideWidth = 0;   
    [SerializeField] private int overrideHeight = 0;  
    [SerializeField] private bool transparentBackground = false;

    [Header("Events")]
    public UnityEvent<Texture2D> onImageReady;    
    public UnityEvent<string> onImageSaved;       

    private int _levelIndex;
    public static CameraController instance { get; private set; }
    public string LastFilePath { get; private set; }

    public Camera UICam
    {
        get => _uiCam;
        set => _uiCam = value;
    }
    
    public Camera CapCam
    {
        get => _captureCamera;
        set => _captureCamera = value;
    }
    public Camera MainCam
    {
        get => _mainCam;
    }

    #region Unity
    private void Awake()
    {
        instance = this;
        if (_uiCam == null) _uiCam = Camera.main;
        if (_captureCamera == null) _captureCamera = Camera.main;
    }
    #endregion

    private void Start()
    {
    }

    public void SetLevelIndex(int levelIndex) => _levelIndex = levelIndex;

    public void MoveCamera(Transform mover, CAMERA_POINT cameraPoint, bool isAnimate = true)
    {
        int index = (int)cameraPoint;
        if (_cameraPoint == null || index < 0 || index >= _cameraPoint.Length || _cameraPoint[index] == null)
        {
            Debug.LogWarning("[CameraController] FocalPoint không hợp lệ.");
            return;
        }
        if(cameraPoint == CAMERA_POINT.ONTABLE)
            _mainCam.fieldOfView = 63;
        else
            _mainCam.fieldOfView = 60;
            
        var target = _cameraPoint[index];

        if (!isAnimate)
        {
            mover.SetPositionAndRotation(target.position, target.rotation);
            return;
        }
        
        StopCoroutine(nameof(CoMove));
        StartCoroutine(CoMove(mover, target, moveDuration));
    }

    private IEnumerator CoMove(Transform mover, Transform target, float duration)
    {
        Vector3 startPos = mover.position;
        Quaternion startRot = mover.rotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = moveCurve.Evaluate(Mathf.Clamp01(t / duration));
            mover.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPos, target.position, k),
                Quaternion.SlerpUnclamped(startRot, target.rotation, k)
            );
            yield return null;
        }
        mover.SetPositionAndRotation(target.position, target.rotation);
    }

    public void CaptureImage()
    {
        if (_captureCamera == null)
        {
            Debug.LogError("[CameraController] _captureCamera chưa gán.");
            return;
        }
        StartCoroutine(GenerateImage());
    }

    private void LogWinLevelEvent()
    {
        Debug.Log($"[CameraController] Win level logged: level={_levelIndex}");
    }

    private IEnumerator GenerateImage()
    {
        yield return new WaitForEndOfFrame();

        int w = overrideWidth > 0 ? overrideWidth : _captureCamera.pixelWidth;
        int h = overrideHeight > 0 ? overrideHeight : _captureCamera.pixelHeight;

        var prevActiveRT = RenderTexture.active;
        var prevTargetRT = _captureCamera.targetTexture;

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        try
        {
            _captureCamera.clearFlags = transparentBackground ? CameraClearFlags.SolidColor : _captureCamera.clearFlags;
            if (transparentBackground)
            {
                var bg = _captureCamera.backgroundColor;
                _captureCamera.backgroundColor = new Color(bg.r, bg.g, bg.b, 0f);
            }

            _captureCamera.targetTexture = rt;
            _captureCamera.Render();

            RenderTexture.active = rt;

            var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            texture.Apply(false, false);

            onImageReady?.Invoke(texture);

            // Lưu PNG
            CheckvalidFolder();
            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"DIY_{_levelIndex:D2}_{time}.png";
            string folder = Path.Combine(Application.persistentDataPath, "Screenshots");
            string path = Path.Combine(folder, fileName);

            var png = texture.EncodeToPNG();
            File.WriteAllBytes(path, png);
            LastFilePath = path;
            onImageSaved?.Invoke(path);
            Debug.Log($"[CameraController] Saved screenshot: {path}");

            LogWinLevelEvent();
        }
        finally
        {
            // Revert & cleanup
            _captureCamera.targetTexture = prevTargetRT;
            RenderTexture.active = prevActiveRT;
            if (rt) rt.Release();
            Destroy(rt);
        }
    }

    private void CheckvalidFolder()
    {
        string folder = Path.Combine(Application.persistentDataPath, "Screenshots");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }
}