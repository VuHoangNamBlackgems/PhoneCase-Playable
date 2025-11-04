using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GlueStep : StepBase
{
    // ===============================================================
    //  REFS
    // ===============================================================

    #region REFS

    [Header("Refs")] public Camera worldCam;
    public Camera glueCam;
    public Transform glueTool;
    public Transform glueToolParent;
    public Transform brush;
    public MeshCollider caseCollider;
    public Collider toolCollider;

    #endregion

    #region DefaultTool

    private Transform _defaultTool;
    private Transform _defaultChildRayOrigin;
    private Collider _defaultStartDragCollider;
    private bool _defaultToolActive;

    #endregion

    // ===============================================================
    //  OPTIONS / FEEL
    // ===============================================================

    #region OPTIONS_FEEL

    [Header("Options")] [SerializeField] private float brushDepthFromGlueCam = 1f;
    [SerializeField] private float toolSurfaceOffset = 0.002f;
    [SerializeField] private bool flipU = false, flipV = false, swapUV = false;
    [SerializeField] private bool clampUVToRT = true;
    [SerializeField] private bool hideBrushWhenOffCase = false;

    [Header("Feel")] [SerializeField] private float followSpeed = 18f;

    #endregion

    // ===============================================================
    //  INPUT
    // ===============================================================

    #region INPUT

    [Header("Input Rules")] [SerializeField]
    private bool ignoreWhenPointerOverUI = true;

    [SerializeField] private bool requireStartOnTool = false;
    [SerializeField] private Collider startDragCollider;
    [SerializeField] private LayerMask startRayMask = ~0;

    #endregion

    // ===============================================================
    //  UV SAMPLING (CHILD RAY)
    // ===============================================================

    #region UV_SAMPLING

    [Header("UV Sampling From Child")] [SerializeField]
    private bool useChildRayForUV = true;

    [SerializeField] private Transform childRayOrigin;
    [SerializeField] private float childRayDistance = 2f;
    [SerializeField] private LayerMask caseLayer = ~0;
    [SerializeField] private bool fallbackToPointerWhenChildMiss = true;
    [SerializeField] private bool stickToolWhenUsingChildRay = false;

    #endregion

    // ===============================================================
    //  DRAG PLANE LOCK (GIỮ 1 TRỤC CỐ ĐỊNH)
    // ===============================================================

    #region DRAG_PLANE_LOCK

    [Header("Drag Plane Lock")] [Tooltip("Khóa kéo trên một mặt phẳng cố định để tool không chìm/lên")] [SerializeField]
    private bool lockAxis = true;

    [Tooltip("Null = dùng world; gán Transform để dùng trục up của object đó")] [SerializeField]
    private Transform dragSpace;

    [Tooltip("Tọa độ dọc theo 'up' của dragSpace (hoặc Y world nếu null)")] [SerializeField]
    private float axisPosition = 7.3f;

    private Vector3 DragUp => dragSpace ? dragSpace.up : Vector3.up;
    private Vector3 DragPlanePoint => (dragSpace ? dragSpace.position : Vector3.zero) + DragUp * axisPosition;

    private Vector3 ProjectToDragPlane(Vector3 p)
    {
        Vector3 n = DragUp;
        Vector3 p0 = DragPlanePoint;
        return p - n * Vector3.Dot(p - p0, n);
    }

    #endregion

    // ===============================================================
    //  PROGRESS
    // ===============================================================

    #region PROGRESS

    [Header("=== Progress ===")] [SerializeField]
    private RenderTexture glueRT; // trống -> dùng glueCam.targetTexture

    [SerializeField] private float progressCheckInterval = 0.2f;
    [SerializeField] private int probeSize = 128;
    [SerializeField, Range(0, 1)] private float completeThreshold = 0.995f;
    [SerializeField] private bool smoothProgress = true;
    [SerializeField] private float progressSmoothSpeed = 6f;

    private enum ProgressSource
    {
        Alpha,
        Luminance,
        MaxRGBA
    }

    [SerializeField] private ProgressSource progressSource = ProgressSource.MaxRGBA;
    [SerializeField, Range(0f, 1f)] private float pixelOnThreshold = 0.08f;
    [SerializeField] private bool forceDontClearColor = true;

    [Header("Auto Fill (no shader)")] [Tooltip("Tự fill dần khi đủ ngưỡng")] [SerializeField]
    private bool animateFillOnComplete = true;

    [SerializeField] private float fillDuration = 0.6f;

    public enum FillMode
    {
        Horizontal,
        Vertical
    }

    [SerializeField] private FillMode fillMode = FillMode.Horizontal;
    [SerializeField] private bool invertFill = false; // Horizontal: true = phải->trái ; Vertical: true = trên->dưới
    [SerializeField] private bool completeAfterFill = true;

    [Header("UI (optional)")] [SerializeField]
    private Image progressBar;

    [SerializeField] private UnityEvent<float> onProgressChanged;
    [SerializeField] private UnityEvent onStepCompleted;

    #endregion

    // ===============================================================
    //  RUNTIME STATE
    // ===============================================================

    #region RUNTIME

    private ParticleSystem _ps, _spray;
    private bool _dragging;
    private int _activeFingerId = -1;
    private Vector3 _toolTarget, _lastHitPoint;
    private Vector3 _lastHitNormal = Vector3.up;

    private RenderTexture _probeRT;
    private float _lastProbeTime = -999f;
    private bool _sprayedThisFrame = false;

    public float _targetProgress = 0f; // đo từ RT
    public float _progress = 0f; // hiển thị (smoothed)
    private bool _completed = false;
    private bool _autoFilled = false;
    private Coroutine _fillCo;
    public float Progress => _progress;
    private RenderTexture SrcRT => glueRT != null ? glueRT : (glueCam ? glueCam.targetTexture : null);

    bool _spraySoundOn = false;

    #endregion

    // ===============================================================
    //  UNITY
    // ===============================================================

    #region UNITY

    private void Awake()
    {
        _ps = brush ? brush.GetComponent<ParticleSystem>() : null;
        _spray = glueTool ? glueTool.GetComponentInChildren<ParticleSystem>() : null;
        _toolTarget = glueTool ? glueTool.position : Vector3.zero;

        CacheDefaultTool();
    }

    private void CacheDefaultTool()
    {
        _defaultTool = glueTool;
        _defaultChildRayOrigin = childRayOrigin;
        _defaultToolActive = _defaultTool ? _defaultTool.gameObject.activeSelf : false;
    }

    public void AdoptExternalTool(Transform newTool,
        bool disableDefaultGO = true)
    {
        if (newTool == null)
        {
            RestoreDefaultTool(true);
            return;
        }

        StopSprayImmediate();

        if (disableDefaultGO)
        {
            if (_defaultTool) _defaultTool.gameObject.SetActive(false);
        }
        var tool = Instantiate(newTool, glueToolParent);
        tool.gameObject.SetActive(true);
        glueTool = tool;
        this.childRayOrigin = tool.GetChild(0);
        _toolTarget = glueTool.position;
    }

    public void RestoreDefaultTool(bool reactivateDefaultGO = true)
    {
        StopSprayImmediate();

        glueTool = _defaultTool;
        this.childRayOrigin = _defaultChildRayOrigin;
        if (reactivateDefaultGO)
        {
            if (_defaultTool) _defaultTool.gameObject.SetActive(true);
        }
        
        if (glueTool) _toolTarget = glueTool.position;
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        caseCollider = phoneCase ? phoneCase.GlueCollider : caseCollider;
        ResetRTAndProgression();

        var start = new Vector3(0f, 7.3f, 0f);

        if (lockAxis) start = ProjectToDragPlane(start);

        _toolTarget = start;
        glueTool.position = start;
        CustomerOrderManager.instance.ShowRewardGlue();
        RestoreDefaultTool();
    }
    
    public override void CompleteStep()
    {
        glueTool.gameObject.SetActive(false);
        CustomerOrderManager.instance.HideReward();
    }

    private void OnEnable()
    {
        StopSprayImmediate();

        if (forceDontClearColor && glueCam)
            glueCam.clearFlags = CameraClearFlags.Nothing;

        _completed = false;
        _autoFilled = false;
        _sprayedThisFrame = false;
        _targetProgress = _progress = 0f;
        _lastProbeTime = -999f;
        EnsureProbeRT();
        PushProgressUI();
    }

    private void OnDisable()
    {
        StopSprayImmediate();
        ReleaseProbeRT();
        if (_fillCo != null) StopCoroutine(_fillCo);
    }

    private void Update()
    {
        if (glueTool == null || worldCam == null || glueCam == null)
        {
            Progression_Tick();
            return;
        }

        bool hasPointer;
        Vector3 pointer;
        HandlePointer(out hasPointer, out pointer);

        if (!hasPointer)
        {
            StopSpraySmooth();
            LerpTool();
            Progression_Tick();
            return;
        }

        // 1) Drag target theo mặt phẳng cố định (hoặc theo mặt phẳng bề mặt nếu tắt lockAxis)
        var pointerRay = BuildPointerRay(pointer);
        UpdateFreeDragTargetOnPlane(pointerRay);

        // 2) Lấy UV cho brush (ưu tiên child-ray; fallback pointer-ray)
        bool gotHit = false;
        RaycastHit hit;

        if (useChildRayForUV && childRayOrigin != null)
        {
            var childRay = BuildChildRay();
            if (caseCollider != null && caseCollider.Raycast(childRay, out hit, childRayDistance))
            {
                gotHit = true;
                ApplyHitForBrushAndOptionallyStick(hit, fromChildRay: true);
            }
            else if (fallbackToPointerWhenChildMiss)
            {
                gotHit = TryPointerHit(pointerRay, out hit);
                if (gotHit) ApplyHitForBrushAndOptionallyStick(hit, fromChildRay: false);
            }
        }
        else
        {
            gotHit = TryPointerHit(pointerRay, out hit);
            if (gotHit) ApplyHitForBrushAndOptionallyStick(hit, fromChildRay: false);
        }

        if (!gotHit) OnPointerOffCase();

        LerpTool(); // giữ tool trên drag-plane nếu lockAxis
        Progression_Tick(); // đo tiến độ
    }

    #endregion

    // ===============================================================
    //  INPUT HANDLING
    // ===============================================================

    #region INPUT_HANDLING

    private static bool IsPointerOverUIStandalone()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private static bool IsTouchOverUI(Touch t)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);

    private bool CanStartDragAtPointer(Vector3 screenPos)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (ignoreWhenPointerOverUI && IsPointerOverUIStandalone())
            return false;
#endif
        if (!requireStartOnTool) return true;

        var r = worldCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(r, out var h, 1000f, startRayMask))
        {
            if (startDragCollider)
                return h.collider == startDragCollider || h.collider.transform.IsChildOf(startDragCollider.transform);
            return h.collider && (h.collider.transform == glueTool || h.collider.transform.IsChildOf(glueTool));
        }

        return false;
    }

    private void HandlePointer(out bool hasPointer, out Vector3 pointer)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (!CanStartDragAtPointer(Input.mousePosition))
            {
                _dragging = false;
                hasPointer = false;
                pointer = default;
                return;
            }

            _dragging = true;
            _activeFingerId = -1;
        }

        if (Input.GetMouseButtonUp(0)) _dragging = false;
        if (ignoreWhenPointerOverUI && IsPointerOverUIStandalone())
        {
            hasPointer = false;
            pointer = default;
            return;
        }

        hasPointer = _dragging || (!requireStartOnTool && Input.GetMouseButton(0));
        pointer = hasPointer ? (Vector3)Input.mousePosition : default;
#else
        hasPointer = false; pointer = default;
        if (!_dragging)
        {
            for (int i = 0;i<Input.touchCount;i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (ignoreWhenPointerOverUI && IsTouchOverUI(t)) continue;
                if (!CanStartDragAtPointer(t.position)) continue;
                _dragging = true; _activeFingerId = t.fingerId; hasPointer = true; pointer = t.position; break;
            }
        }
        else
        {
            for (int i = 0;i<Input.touchCount;i++)
            {
                var t = Input.GetTouch(i); if (t.fingerId != _activeFingerId) continue;
                if (ignoreWhenPointerOverUI && IsTouchOverUI(t))
                { hasPointer = false; pointer =
 default; if (t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended) _dragging = false; return; }
                pointer = t.position; hasPointer = true;
                if (t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended) { _dragging = false; hasPointer = false; }
                return;
            }
            _dragging = false; hasPointer = false;
        }
#endif
    }

    #endregion

    // ===============================================================
    //  RAYS & HITS
    // ===============================================================

    #region RAYS_HITS

    private Ray BuildPointerRay(Vector3 pointer) => worldCam.ScreenPointToRay(pointer);
    private Ray BuildChildRay() => new Ray(childRayOrigin.position, childRayOrigin.forward);

    private bool TryPointerHit(Ray pointerRay, out RaycastHit hit)
    {
        if (caseCollider != null)
        {
            if (caseCollider.Raycast(pointerRay, out hit, 1000f))
                return true;
        }

        hit = default;
        return false;
    }

    #endregion

    // ===============================================================
    //  DRAG / STICK / LERP  (ĐÃ MANG TỪ SPRAYSTEP SANG)
    // ===============================================================

    #region DRAG_STICK_LERP

    private void UpdateFreeDragTargetOnPlane(Ray ray)
    {
        Plane plane;
        if (lockAxis)
        {
            plane = new Plane(DragUp, DragPlanePoint);
        }
        else
        {
            var n = (_lastHitNormal == Vector3.zero) ? Vector3.up : _lastHitNormal;
            var p0 = (_lastHitPoint == Vector3.zero) ? glueTool.position : _lastHitPoint;
            plane = new Plane(n, p0);
        }

        if (plane.Raycast(ray, out float dist))
        {
            var p = ray.GetPoint(dist);
            _toolTarget = lockAxis ? ProjectToDragPlane(p) : p;
        }
    }

    private void StickToolToCase(RaycastHit hit)
    {
        if (lockAxis)
        {
            _toolTarget = ProjectToDragPlane(hit.point); // luôn nằm trên plane cố định
        }
        else
        {
            _toolTarget = hit.point + hit.normal * toolSurfaceOffset; // bám theo normal khi không khóa
        }
    }

    private void LerpTool()
    {
        if (!glueTool) return;
        var target = lockAxis ? ProjectToDragPlane(_toolTarget) : _toolTarget;
        glueTool.position = Vector3.Lerp(glueTool.position, target, Time.deltaTime * followSpeed);
    }

    #endregion

    // ===============================================================
    //  UV / BRUSH
    // ===============================================================

    #region UV_BRUSH

    private void ApplyHitForBrushAndOptionallyStick(RaycastHit hit, bool fromChildRay)
    {
        _lastHitNormal = hit.normal;
        _lastHitPoint = hit.point;

        if (!fromChildRay || (fromChildRay && stickToolWhenUsingChildRay))
            StickToolToCase(hit);

        SprayOnCase(hit);
    }

    private void SprayOnCase(RaycastHit hit)
    {
        if (brush == null) return;

        var uv = AdjustUV(hit.textureCoord);
        brush.position = UVToGlueWorld(uv);
        brush.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);

        PlaySpray();
        _sprayedThisFrame = true; // kích hoạt đo progress
    }

    private void OnPointerOffCase()
    {
        if (hideBrushWhenOffCase && brush && glueCam)
        {
            brush.position = glueCam.transform.position - glueCam.transform.forward * 1000f;
        }
    }

    private Vector2 AdjustUV(Vector2 uv)
    {
        if (swapUV)
        {
            float t = uv.x;
            uv.x = uv.y;
            uv.y = t;
        }

        if (flipU) uv.x = 1f - uv.x;
        if (flipV) uv.y = 1f - uv.y;

        if (clampUVToRT)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        return uv;
    }

    private Vector3 UVToGlueWorld(Vector2 uv)
    {
        return glueCam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, brushDepthFromGlueCam));
    }

    #endregion

    // ===============================================================
    //  PARTICLES & SOUND
    // ===============================================================

    #region PARTICLES_SOUND

    private void PlaySpray()
    {
        if (_ps != null && !_ps.isPlaying) _ps.Play(true);
        if (_spray != null && !_spray.isPlaying) _spray.Play(true);
        StartSpraySound();
    }

    private void StopSpraySmooth()
    {
        if (_ps != null && _ps.isPlaying) _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_spray != null && _spray.isPlaying) _spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        StopSpraySound();
    }

    private void StopSprayImmediate()
    {
        if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_spray != null) _spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        StopSpraySound();
    }

    private void StartSpraySound()
    {
        if (_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null)
        {
            am.PlayGlue();
            _spraySoundOn = true;
        }
    }

    private void StopSpraySound()
    {
        if (!_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null) am.StopGlue();
        _spraySoundOn = false;
    }

    #endregion

    // ===============================================================
    //  PROGRESS IMPL
    // ===============================================================

    #region PROGRESS_IMPL

    private void Progression_Tick()
    {
        // mượt hiển thị
        _progress = smoothProgress
            ? Mathf.MoveTowards(_progress, _targetProgress, Time.unscaledDeltaTime * progressSmoothSpeed)
            : _targetProgress;

        PushProgressUI();
        onProgressChanged?.Invoke(_progress);

        // Khi đạt ngưỡng: bắt đầu fill dần (nếu bật), rồi complete khi xong
        if (!_autoFilled && (_targetProgress >= completeThreshold || _progress >= completeThreshold))
        {
            _autoFilled = true;
            _targetProgress = 1f;
            var rt = SrcRT;
            if (animateFillOnComplete && rt != null)
            {
                if (_fillCo != null) StopCoroutine(_fillCo);
                _fillCo = StartCoroutine(AnimateFillRT_NoShader(rt, fillDuration, fillMode, invertFill,
                    completeAfterFill));
                // Hoãn complete cho đến khi fill xong (coroutine sẽ gọi)
            }
            else
            {
                // Fill ngay lập tức & complete
                if (rt != null) Graphics.Blit(Texture2D.whiteTexture, rt);
                if (completeAfterFill && !_completed)
                {
                    onStepCompleted?.Invoke();
                    CompleteStep();
                }
            }
        }

        // Đọc tiến độ từ RT chỉ khi vừa xịt và đủ interval
        if (!_sprayedThisFrame) return;
        if (Time.unscaledTime - _lastProbeTime < progressCheckInterval) return;

        var src = SrcRT;
        if (src == null) return;

        EnsureProbeRT();
        Graphics.Blit(src, _probeRT); // downsample


        //#if UNITY_WEBGL
        //{
        Texture2D tex = new Texture2D(_probeRT.width, _probeRT.height, TextureFormat.RGBA32, false, false);

        var prev = RenderTexture.active;
        RenderTexture.active = _probeRT;
        tex.ReadPixels(new Rect(0, 0, _probeRT.width, _probeRT.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        Color32[] data = tex.GetPixels32();   // <--- COLOR32 ARRAY, KHÔNG PHẢI BYTE[]
        int len = data.Length;
        if (len == 0) return;

        int painted = 0;
        for (int i = 0; i < len; i++)
        {
            Color32 c = data[i];
            float v;

            switch (progressSource)
            {
                case ProgressSource.Alpha:
                    v = c.a / 255f;
                    break;

                case ProgressSource.Luminance:
                    v = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                    break;

                default: // MaxRGBA
                    byte m1 = (c.r > c.g) ? c.r : c.g;
                    byte m2 = (c.b > c.a) ? c.b : c.a;
                    byte m = (m1 > m2) ? m1 : m2;
                    v = m / 255f;
                    break;
            }

            if (v >= pixelOnThreshold) painted++;
        }

        _targetProgress = Mathf.Clamp01((float)painted * 2f / len);
        //}
        /*#else
                AsyncGPUReadback.Request(_probeRT, 0, request =>
                {
                    if (request.hasError) return;
                    var data = request.GetData<Color32>();
                    int len = data.Length;
                    if (len == 0) return;

                    int painted = 0;
                    for (int i = 0; i < len; i++)
                    {
                        var c = data[i];
                        float v;
                        switch (progressSource)
                        {
                            case ProgressSource.Alpha:
                                v = c.a / 255f; break;
                            case ProgressSource.Luminance:
                                v = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f; break;
                            default: // MaxRGBA
                                byte m1 = (c.r > c.g) ? c.r : c.g;
                                byte m2 = (c.b > c.a) ? c.b : c.a;
                                byte m = (m1 > m2) ? m1 : m2;
                                v = m / 255f;
                                break;
                        }

                        if (v >= pixelOnThreshold) painted++;
                    }

                    _targetProgress = Mathf.Clamp01((float)painted * 2f / len);
                });
        #endif*/
        _lastProbeTime = Time.unscaledTime;
        _sprayedThisFrame = false;
    }

    private void PushProgressUI()
    {
        if (progressBar) progressBar.fillAmount = _progress;
    }

    private void EnsureProbeRT()
    {
        if (_probeRT != null && _probeRT.width == probeSize && _probeRT.height == probeSize) return;
        ReleaseProbeRT();
        _probeRT = new RenderTexture(probeSize, probeSize, 0, RenderTextureFormat.ARGB32)
        {
            name = "GlueStep_ProbeRT",
            useMipMap = false,
            autoGenerateMips = false
        };
        _probeRT.Create();
    }

    private void ReleaseProbeRT()
    {
        if (_probeRT != null)
        {
            _probeRT.Release();
            _probeRT = null;
        }
    }
    private static Material _fillMat;
    private static Material FillMat
    {
        get
        {
            if (!_fillMat)
            {
                // UI/Default luôn exists trong WebGL + không cần vertex color
                _fillMat = new Material(Shader.Find("UI/Default"));
            }
            return _fillMat;
        }
    }

    private IEnumerator AnimateFillRT_NoShader(RenderTexture rt, float duration, FillMode mode, bool invert,
        bool doComplete)
    {
        int w = rt.width;
        int h = rt.height;
        float t = 0f;

        while (t < duration)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

            Rect r;
            if (mode == FillMode.Horizontal)
            {
                int px = Mathf.RoundToInt(w * k);
                r = invert ? new Rect(w - px, 0, px, h) : new Rect(0, 0, px, h);
            }
            else
            {
                int py = Mathf.RoundToInt(h * k);
                r = invert ? new Rect(0, h - py, w, py) : new Rect(0, 0, w, py);
            }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            // ✅ Convert RECT từ pixel sang normalized 0..1 (LoadOrtho)
            float xMin = r.xMin / w;
            float xMax = r.xMax / w;
            float yMin = r.yMin / h;
            float yMax = r.yMax / h;

            GL.PushMatrix();
            GL.LoadOrtho(); // ✅ Thay thế cho GL.LoadPixelMatrix

            FillMat.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.Vertex3(xMin, yMin, 0);
            GL.Vertex3(xMax, yMin, 0);
            GL.Vertex3(xMax, yMax, 0);
            GL.Vertex3(xMin, yMax, 0);
            GL.End();

            GL.PopMatrix();
            RenderTexture.active = prev;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // ✅ Fill full khi xong animation
        Graphics.Blit(Texture2D.whiteTexture, rt);

        if (doComplete && !_completed)
        {
            onStepCompleted?.Invoke();
            CompleteStep();
        }
    }


    [ContextMenu("DEBUG: Fill RT = 100%")]
    private void DebugFillFull()
    {
        var rt = SrcRT;
        if (rt) Graphics.Blit(Texture2D.whiteTexture, rt);
    }


    [ContextMenu("DEBUG: Reset RT + Progression")]
    private void Debug_ResetRTAndProgression() => ResetRTAndProgression();

    private void ResetRTAndProgression()
    {
        // Ngắt mọi hiệu ứng đang chạy
        StopSprayImmediate();
        if (_fillCo != null)
        {
            StopCoroutine(_fillCo);
            _fillCo = null;
        }

        // Reset trạng thái tiến độ
        _completed = false;
        _autoFilled = false;
        _sprayedThisFrame = false;
        _targetProgress = 0f;
        _progress = 0f;
        _lastProbeTime = -999f;

        // Clear RT về trong suốt (alpha = 0)
        var rt = SrcRT;
        if (rt != null)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        // (Tùy) đảm bảo probe RT tồn tại để đọc tiến độ
        EnsureProbeRT();

        // Cập nhật UI
        PushProgressUI();

        // Nếu đang dùng "không clear camera", đảm bảo camera không tự clear
        if (forceDontClearColor && glueCam) glueCam.clearFlags = CameraClearFlags.Nothing;
    }

    #endregion
}