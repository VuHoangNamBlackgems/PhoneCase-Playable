using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlurStep : StepBase
{
    [Header("Refs")]
    [SerializeField] private Camera worldCam;
    [SerializeField] private Camera paintCam;
    [SerializeField] private MeshCollider caseCollider;
    [SerializeField] private Transform brushVisual;
    [SerializeField] private Transform tool;
    [SerializeField] private Animator toolAnimator;
    [SerializeField] private ParticleSystem toolPar;
    [SerializeField] private ParticleSystem toolPar2;
    [SerializeField] private Animator tutorial;

    [Header("Blur Brush Settings")]
    [SerializeField] private bool blurOnDrag = true;
    [SerializeField] private float blurRadiusPx = 20f;
    [SerializeField] private float blurStrength = 0.9f;
    [SerializeField] private float spacingFactor = 0.6f;
    [SerializeField] private int maxStampsPerSecond = 90;

    [Header("Off-Case Follow")]
    [SerializeField] private bool followOffCase = true;
    [SerializeField] private bool hideWhenOffCase = false;
    [SerializeField] private float offCaseDepthDefault = 0.4f;
    [SerializeField] private float toolSurfaceOffset = 0.004f;
    [SerializeField] private float followPosSpeed = 18f;
    [SerializeField] private float followRotSpeed = 18f;

    float _lastHitDistance = -1f;
    private Vector3 _lastHitNormal = Vector3.up;
    private Vector3 _lastHitPoint;

    [Header("Bleed Controls (Big)")]
    [Range(0f, 2.0f)] public float bleedScale = 0.8f;
    [Range(0f, 256f)] public float bleedExtraPx = 12f;
    [Range(0f, 1f)]   public float bleedStrength = 0.7f;
    [Range(0.5f, 6f)] public float bleedExtentScale = 3.0f;
    [Range(0.0005f, 0.05f)] public float edgeSoftUV = 0.012f;

    [Header("UV Adjust")]
    [SerializeField] private bool flipU = false, flipV = false, swapUV = false;
    [SerializeField] private bool clampUVToRT = true;

    [Header("Input")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    [Header("Child Ray (optional)")]
    [SerializeField] private bool useChildRay = false;
    [SerializeField] private Transform childRayOrigin;
    [SerializeField] private float childRayDistance = 2f;

    [Header("UI")]
    [SerializeField] Image progressBar;

    [Header("Auto Complete")]
    [SerializeField] private bool autoCompleteAtFull = true;       // tự complete khi progress = 1
    [SerializeField] private bool autoSweepAtFull = true;          // quét mờ toàn ảnh trước khi complete
    [SerializeField, Tooltip("Mỗi lần đóng dấu sẽ tăng bấy nhiêu")] 
    private float progressPerStamp = 0.01f;                        // trước đây là +0.01f mỗi frame
    [SerializeField] private float completeDelay = 0.15f;          // trễ 1 nhịp trước khi đóng step

    [Header("Auto Sweep Tuning")]
    [SerializeField, Tooltip("Lưới quét NxN (tổng ~N*N dấu)")] 
    private int finishGrid = 18;
    [SerializeField, Tooltip("Nhân bán kính cho lượt quét cuối")]
    private float finishExtraRadiusMul = 1.25f;
    [SerializeField, Tooltip("Độ mạnh blur của lượt quét cuối")]
    private float finishStrength = 0.6f;
    [SerializeField, Tooltip("Thời gian thực thi sweep (chia theo frame)")]
    private float finishSweepDuration = 0.5f;

    // runtime
    [SerializeField] Shader blurStampShader;
    BlurBrushStamp _blur;
    private bool _dragging;
    private int _activeFinger = -1;
    private Vector2 _lastUV;
    private bool _hasLastUV;
    private float _stampCooldown;

    private bool _finishing = false;
    private bool _completed = false;

    public override void SetUp(PhoneCase phoneCase)
    {
        paintCam.targetTexture = phoneCase._paintRT;
        caseCollider = phoneCase.CaseCollider;
        progressBar.fillAmount = 0f;
        
    }

    private void Awake()
    {
        if (PlayerPrefs.GetInt("Tutorial") == 0)
        {
            tutorial.gameObject.SetActive(true);
            UserTracking.TutorialAction(ActionTut.dry_start);
        }
        else tutorial.gameObject.SetActive(false);

        _blur = new BlurBrushStamp(blurStampShader);
        _blur.bleedScale       = bleedScale;
        _blur.bleedExtraPx     = bleedExtraPx;
        _blur.bleedStrength    = bleedStrength;
        _blur.bleedExtentScale = bleedExtentScale;
        _blur.edgeSoftUV       = edgeSoftUV;
    }

    private void OnEnable()
    {
        toolAnimator.speed = 0f;
        _dragging = false;
        _activeFinger = -1;
        _hasLastUV = false;
        _stampCooldown = 0f;
        _finishing = false;
        _completed = false;
        _btnDone.gameObject.SetActive(true);
        var start = new Vector3(0f, 7.3f, 0f);
        tool.position = start;
        tool.gameObject.SetActive(true);
    }

    public override void CompleteStep()
    {
        UserTracking.TutorialAction(ActionTut.dry_end);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_completed || _finishing) return;
        if (worldCam == null || paintCam == null || caseCollider == null) return;

        bool hasPointer; Vector3 pointer;
        HandlePointer(out hasPointer, out pointer);
        if (!hasPointer)
        {
            toolPar.Stop();
            toolPar2.Stop();
            StopSpraySound();
            toolAnimator.speed = 0f;
            toolAnimator.Play("Drier");
            if (brushVisual) brushVisual.gameObject.SetActive(false);
            return;
        }

        var ray = BuildRay(pointer);

        // Raycast vào case
        if (caseCollider.Raycast(ray, out var hit, useChildRay ? childRayDistance : 1000f))
        {
            _lastHitNormal   = hit.normal;
            _lastHitPoint    = hit.point;
            _lastHitDistance = hit.distance;

            var uv = AdjustUV(hit.textureCoord);

            // đặt tool + brush theo bề mặt
            PlaceTool(hit.point, hit.normal);
            PlaceBrushVisual(hit.point, hit.normal);

            if (blurOnDrag) StampAlongPath(uv); else _hasLastUV = false;
        }
        else
        {
            _hasLastUV = false; // KHÔNG vẽ khi ngoài case

            if (followOffCase)
            {
                float d = (_lastHitDistance > 0f) ? _lastHitDistance
                    : (useChildRay ? childRayDistance : offCaseDepthDefault);
                Vector3 pos = ray.origin + ray.direction * d;
                Vector3 nrm = (_lastHitDistance > 0f) ? _lastHitNormal
                    : (-worldCam.transform.forward);

                PlaceTool(pos, nrm);
                PlaceBrushVisual(pos, nrm);

                if (hideWhenOffCase)
                {
                    if (tool) tool.gameObject.SetActive(false);
                    if (brushVisual) brushVisual.gameObject.SetActive(false);
                }
            }
            else
            {
                if (tool && hideWhenOffCase) tool.gameObject.SetActive(false);
                if (brushVisual) brushVisual.gameObject.SetActive(false);
            }
        }
    }

    private void PlaceTool(Vector3 pos, Vector3 normal)
    {
        if (!tool) return;
        if (!tool.gameObject.activeSelf) tool.gameObject.SetActive(true);

        Vector3 targetPos = pos + normal * toolSurfaceOffset;
        float kp = 1f - Mathf.Exp(-followPosSpeed * Time.deltaTime);
        tool.position = Vector3.Lerp(tool.position, targetPos, kp);
    }

    private void PlaceBrushVisual(Vector3 pos, Vector3 normal)
    {
        if (!brushVisual) return;
        if (!brushVisual.gameObject.activeSelf) brushVisual.gameObject.SetActive(true);
        brushVisual.position = pos + normal * toolSurfaceOffset;
    }

    // ===== Blur path =====
    private void StampAlongPath(Vector2 currentUV)
    {
        var rt = paintCam.targetTexture;
        if (rt == null) return;

        float radiusUV = blurRadiusPx / Mathf.Max(rt.width, rt.height);

        // hạn chế tần suất
        _stampCooldown -= Time.deltaTime;
        float minInterval = 1f / Mathf.Max(1, maxStampsPerSecond);
        if (_stampCooldown <= 0f)
        {
            if (_hasLastUV)
            {
                float spacing = Mathf.Max(0.0001f, spacingFactor * radiusUV);
                float dist = Vector2.Distance(_lastUV, currentUV);
                int steps = Mathf.FloorToInt(dist / spacing);
                if (steps <= 0)
                {
                    _blur.Stamp(rt, currentUV, blurRadiusPx, blurStrength);
                    OnProgress(); // tăng progress theo mỗi stamp
                }
                else
                {
                    Vector2 dir = (currentUV - _lastUV) / (steps + 1);
                    Vector2 uv = _lastUV;
                    for (int i = 0; i <= steps; i++)
                    {
                        uv += dir;
                        _blur.Stamp(rt, uv, blurRadiusPx, blurStrength);
                        OnProgress();
                        if (_finishing || _completed) return; // đã vào finish thì dừng
                    }
                }
            }
            else
            {
                _blur.Stamp(rt, currentUV, blurRadiusPx, blurStrength);
                OnProgress();
            }

            _lastUV = currentUV;
            _hasLastUV = true;
            _stampCooldown = minInterval;
        }
    }

    // ===== Input =====
    private void HandlePointer(out bool hasPointer, out Vector3 pointer)
{
#if UNITY_EDITOR || UNITY_STANDALONE
    // BẮT ĐẦU kéo
    if (Input.GetMouseButtonDown(0))
    {
        tutorial.gameObject.SetActive(false);

        // CHỈ chặn khi BẮT ĐẦU trên UI
        if (ignoreWhenPointerOverUI && IsPointerOverUIStandalone())
        { hasPointer = false; pointer = default; return; }

        toolPar.Play();
        toolPar2.Play();
        StartSpraySound();
        toolAnimator.speed = 1f;
        toolAnimator.Play("Drier");
        _dragging = true;
        _activeFinger = -1;
        _hasLastUV = false;
    }

    if (Input.GetMouseButtonUp(0))
    {
        _dragging = false;
        _hasLastUV = false;
    }

    // ĐANG KÉO: KHÔNG hủy dù con trỏ đang đè UI
    hasPointer = _dragging || Input.GetMouseButton(0);
    pointer = Input.mousePosition;

#else
    hasPointer = false; pointer = default;

    if (!_dragging)
    {
        // BẮT ĐẦU kéo
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;

            // CHỈ chặn khi BẮT ĐẦU trên UI
            if (ignoreWhenPointerOverUI && IsTouchOverUI(t)) continue;

            toolAnimator.speed = 1f;
            toolAnimator.Play("Drier");
            StartSpraySound();
            toolPar.Play();
            toolPar2.Play();
            _dragging = true;
            _activeFinger = t.fingerId;
            _hasLastUV = false;
            hasPointer = true;
            pointer = t.position;
            break;
        }
    }
    else
    {
        // ĐANG KÉO: KHÔNG hủy dù đè UI
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.fingerId != _activeFinger) continue;

            tutorial.gameObject.SetActive(false);

            pointer = t.position;
            hasPointer = (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled);

            if (!hasPointer) { _dragging = false; _hasLastUV = false; }
            return;
        }
        _dragging = false; _hasLastUV = false; hasPointer = false;
    }
#endif
}


    // ===== Rays & UV =====
    private Ray BuildRay(Vector3 screenPos)
    {
        if (useChildRay && childRayOrigin)
            return new Ray(childRayOrigin.position, childRayOrigin.forward);
        return worldCam.ScreenPointToRay(screenPos);
    }

    private Vector2 AdjustUV(Vector2 uv)
    {
        if (swapUV) { float t = uv.x; uv.x = uv.y; uv.y = t; }
        if (flipU) uv.x = 1f - uv.x;
        if (flipV) uv.y = 1f - uv.y;
        if (clampUVToRT) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); }
        return uv;
    }

    private Vector3 UVToPaintWorld(Vector2 uv)
    {
        return paintCam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, 1f));
    }

    private static bool IsPointerOverUIStandalone()
    {
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
    }
    private static bool IsTouchOverUI(Touch t)
    {
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId);
    }

    // ===== Progress & Finish =====
    Tweener _tween;
    private void OnProgress(float delta = -1f)
    {
        if (_finishing || _completed) return;

        float step = (delta > 0f) ? delta : progressPerStamp;
        float target = Mathf.Clamp01(progressBar.fillAmount + step);

        _tween?.Kill(false);
        _tween = DOTween.To(() => progressBar.fillAmount, v => progressBar.fillAmount = v, target, 0.1f)
            .SetEase(Ease.OutCubic);

        if (autoCompleteAtFull && target >= 1f && !_finishing)
        {
            StartCoroutine(AutoFinishAndComplete());
        }
    }

    private bool _spraySoundOn = false;
    private void StartSpraySound()
    {
        if (_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null)
        {
            am.PlayDrier();
            _spraySoundOn = true;
        }
    }
    private void StopSpraySound()
    {
        if (!_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null) am.StopDrier();
        _spraySoundOn = false;
    }

    private IEnumerator AutoFinishAndComplete()
    {
        _finishing = true;

        // tắt input/FX
        _dragging = false;
        toolPar.Stop();
        toolPar2.Stop();
        StopSpraySound();
        if (tutorial) tutorial.gameObject.SetActive(false);

       
        // đảm bảo progress = 1
        _tween?.Kill(false);
        _tween = DOTween.To(() => progressBar.fillAmount, v => progressBar.fillAmount = v, 1f, 0.15f)
            .SetEase(Ease.OutCubic);
        // lượt quét cuối phủ toàn ảnh
        if (autoSweepAtFull && paintCam && paintCam.targetTexture)
        {
            var rt = paintCam.targetTexture;
            int N = Mathf.Max(4, finishGrid);
            int total = N * N;

            // chia đều theo thời gian để không tụt fps
            float t0 = Time.time;
            float deadline = t0 + Mathf.Max(0.05f, finishSweepDuration);
            int i = 0;

            while (i < total)
            {
                // đóng một cụm ~ (total / (fps * duration)) mỗi frame
                int chunk = Mathf.Clamp(Mathf.CeilToInt(total / (finishSweepDuration * 60f)), 1, 512);
                int end = Mathf.Min(i + chunk, total);

                for (; i < end; i++)
                {
                    int x = i % N;
                    int y = i / N;
                    // jitter nhỏ để không thấy pattern
                    float jx = (Random.value - 0.5f) / N;
                    float jy = (Random.value - 0.5f) / N;
                    Vector2 uv = new Vector2((x + 0.5f) / N + jx, (y + 0.5f) / N + jy);

                    _blur.Stamp(rt, uv, blurRadiusPx * finishExtraRadiusMul, finishStrength);
                }

                // nếu còn thời gian thì nhường frame
                if (Time.time < deadline) yield return null;
                else yield return null; // vẫn nhường để UI mượt
            }
        }
        if (tool) tool.gameObject.SetActive(false);
        if (brushVisual) brushVisual.gameObject.SetActive(false);
        _btnDone.gameObject.SetActive(false);
        yield return new WaitForSeconds(completeDelay);
        StepFlow.instance.Next();
        _completed = true;
        CompleteStep(); // tắt step
    }
}
