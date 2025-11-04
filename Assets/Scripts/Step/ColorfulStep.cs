using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;


public class ColorfulStep : StepBase
{
    // ============================ Refs ============================
    [Header("Refs")]
    [SerializeField] private Camera worldCam;           // Main camera 3D
    [SerializeField] private Camera sprayCam;           // Ortho cam render RT
    [SerializeField] private Transform sprayTool;       // Tool (chai xịt)
    [SerializeField] private Transform brush;           // Particle brush (layer "Glue")
    [SerializeField] private MeshCollider caseCollider; // Collider mặt ốp (phải có UV)
    [SerializeField] private Transform spawnItem;       // UI list slot
    [SerializeField] private SprayPreview prayPreviewPrefab;
    [SerializeField] private List<SprayPreview> sprayPreviews = new List<SprayPreview>();
    [SerializeField] private Material sprayCanMat;      // Vỏ chai
    [SerializeField] private GameObject effectSlot;     // Slot chứa effect xịt (particle)
    [SerializeField] private GameObject brushSlot;      // Slot chứa brush vẽ (particle)
    private Animator _animatorBrush;
    [SerializeField] private float doneIdleDelay = 3f;  
    // ============================ Options ============================
    // ============================ Options ============================
    [Header("Options")]
    [Tooltip("Độ sâu đặt brush trong không gian SprayCam (giữa near/far)")]
    [SerializeField] private float brushDepthFromGlueCam = 1f;

    [Tooltip("Đẩy tool nổi lên khỏi mặt ốp (chỉ dùng khi KHÔNG lockAxis)")]
    [SerializeField] private float toolSurfaceOffset = 0.002f;

    [Tooltip("Sửa UV khi 2 project khác nhau")]
    [SerializeField] private bool flipU = false, flipV = false, swapUV = false;

    [Tooltip("Kẹp UV vào [0..1]")]
    [SerializeField] private bool clampUVToRT = true;

    [Tooltip("Ra ngoài case thì ẩn brush (đẩy ra xa cam)")]
    [SerializeField] private bool hideBrushWhenOffCase = false;

    [Header("Feel")]
    [Tooltip("Tốc độ follow tool (lerp)")]
    [SerializeField] private float followSpeed = 18f;

    // ============================ Input Rules ============================
    [Header("Input Rules")]
    [Tooltip("Bỏ qua input 3D nếu con trỏ đang ở trên UI")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    [Tooltip("Chỉ bắt đầu kéo nếu bấm trúng collider của tool/child")]
    [SerializeField] private bool requireStartOnTool = false;

    [Tooltip("Collider để yêu cầu bấm trúng trước khi kéo (tuỳ chọn)")]
    [SerializeField] private Collider startDragCollider;

    [Header("Start Drag Filter")]
    [Tooltip("LayerMask để kiểm tra bắt đầu kéo (tránh dính collider nền)")]
    [SerializeField] private LayerMask startRayMask = ~0;

    // ============================ UV Sampling From Child ============================
    [Header("UV Sampling From Child")]
    [Tooltip("Bắn ray từ child (đầu vòi) CHỈ để lấy UV brush. Drag vẫn theo pointer.")]
    [SerializeField] private bool useChildRayForUV = true;

    [SerializeField] private Transform childRayOrigin;     // ví dụ đầu vòi
    [SerializeField] private float childRayDistance = 2f;  // tầm ray của đầu vòi
    [SerializeField] private LayerMask caseLayer = ~0;     // nếu dùng Physics.Raycast

    [Tooltip("Nếu child-ray không trúng case thì fallback sang pointer-ray để vẫn xịt")]
    [SerializeField] private bool fallbackToPointerWhenChildMiss = true;

    [Tooltip("Khi đang dùng child-ray, có dán tool theo điểm hit không? (thường để FALSE)")]
    [SerializeField] private bool stickToolWhenUsingChildRay = false;

    // ============================ Drag Plane Lock ============================
    [Header("Drag Plane Lock")]
    [Tooltip("Khóa kéo trên một mặt phẳng cố định để tool không chìm/lên")]
    [SerializeField] private bool lockAxis = true;

    [Tooltip("Null = dùng world; gán vào Transform để dùng trục up của object đó")]
    [SerializeField] private Transform dragSpace;

    [Tooltip("Tọa độ dọc theo 'up' của dragSpace (hoặc Y world nếu null)")]
    [SerializeField] private float axisPosition = 7.3f;

    // Helpers cho drag-plane
    private Vector3 DragUp => dragSpace ? dragSpace.up : Vector3.up;
    private Vector3 DragPlanePoint => (dragSpace ? dragSpace.position : Vector3.zero) + DragUp * axisPosition;

    private Vector3 ProjectToDragPlane(Vector3 p)
    {
        // chiếu p lên mặt phẳng (DragUp, DragPlanePoint)
        Vector3 n = DragUp;
        Vector3 p0 = DragPlanePoint;
        return p - n * Vector3.Dot(p - p0, n);
    }

    // ============================ Runtime ============================
    private ParticleSystem _ps;     // brush particle
    private ParticleSystem _spray;  // effect particle (tia xịt)
    private bool _dragging;
    private int _activeFingerId = -1;
    private Vector3 _toolTarget;

    // Neo plane kéo mượt (khi không lockAxis)
    private Vector3 _lastHitNormal = Vector3.up;
    private Vector3 _lastHitPoint;
    
    // ===== Done pulse runtime =====
    private bool _sprayedDuringDrag = false;  
    private bool _armedIdlePulse = false;     
    private bool _idlePulseActive = false;    
    private float _lastInteractionTime = 0f;  
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;

    // ============================ Unity ============================
    private void Awake()
    {
        _toolTarget = sprayTool ? sprayTool.position : Vector3.zero;
        _btnDoneBaseScale = _btnDone ? (Vector3)_btnDone.localScale : Vector3.one;
        _lastInteractionTime = Time.unscaledTime;

        BuildUIListAndSelectFirst();
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        // Gắn RT cho sprayCam (giả định sprayCam đã có targetTexture)
        if (sprayCam) sprayCam.targetTexture = phoneCase._paintRT;
        caseCollider = phoneCase.CaseCollider;
        
        var start = new Vector3(0f, 7.3f, 0f);

        if (lockAxis) start = ProjectToDragPlane(start);

        _toolTarget = start;
        sprayTool.position = start;
        if (sprayPreviews.Count > 0) OnSelectSpray(sprayPreviews[0]);
    }

    public override void CompleteStep()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()  => StopSprayImmediate();
    private void OnDisable()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        StopSprayImmediate();
    }


    // ============================ UI List ============================
    private void BuildUIListAndSelectFirst()
    {
        sprayPreviews.Clear();
        var manager = ItemDataManager.Instance.listColorful;
        
        foreach (var data in ItemDataManager.Instance.listColorful)
            if (data.isUnlock && !UserGameData.IsItemColorfulUnlocked(data.id))
                UserGameData.UnlockItemColorful(data.id);
        
        for (int i = 0; i < manager.Count; i++)
        {
            var sprayCan = Instantiate(prayPreviewPrefab, spawnItem);
            sprayCan.SetUp(manager[i], () => { OnSelectSpray(sprayCan); });
            sprayCan.SetUpUnlock(UserGameData.IsItemColorfulUnlocked(sprayCan.SprayData.id));
            sprayPreviews.Add(sprayCan);
        }
        if (sprayPreviews.Count > 0)
            OnSelectSpray(sprayPreviews[0]);
    }

    private void OnSelectSpray(SprayPreview p)
    {
        if (!p.IsUnlock)
        {
         /*   CallAdsManager.ShowRewardVideo("reward", () =>
            {*/
                sprayCanMat.mainTexture = p.SprayData.textureSprayCan;
                p.SetUpUnlock(true);
                UserTracking.ItemPick(step.ToString(), p.SprayData.id);
                if (_ps) Destroy(_ps.gameObject);
                if (_spray) Destroy(_spray.gameObject);
                UserGameData.UnlockItemColorful(p.SprayData.id);
                _ps = Instantiate(p.SprayData.brush, brushSlot.transform);
                _spray = Instantiate(p.SprayData.spray, effectSlot.transform);
                brush = _ps.transform;

                StopSprayImmediate();
            //});
            return;
        }
        
        UnSelect();
        p.Select();
        
        sprayCanMat.mainTexture = p.SprayData.textureSprayCan;

        if (_ps) Destroy(_ps.gameObject);
        if (_spray) Destroy(_spray.gameObject);

        _ps = Instantiate(p.SprayData.brush, brushSlot.transform);
        _spray = Instantiate(p.SprayData.spray, effectSlot.transform);
        brush = _ps.transform;
        _animatorBrush = _ps.gameObject.GetComponent<Animator>();
        StopSprayImmediate();
    }
    
    public void UnSelect()
    {
        if(sprayPreviews.Count < 0) return;
        foreach (var spray in sprayPreviews)
        {
            spray.Unselect();
        }
    }

    // ============================ Update Loop ============================
    private void Update()
    {
        if (!sprayTool || !worldCam || !sprayCam)
        {
            CheckIdlePulseTick();
            return;
        }

        bool hasPointer;
        Vector3 pointer;
        HandlePointer(out hasPointer, out pointer);

        if (!hasPointer)
        {
            StopSpraySmooth();
            LerpTool();
            CheckIdlePulseTick();
            return;
        }

        // 1) Ray dùng cho DRAG (luôn là pointer-ray) -> cập nhật mục tiêu kéo theo plane
        var pointerRay = BuildPointerRay(pointer);
        UpdateFreeDragTargetOnPlane(pointerRay);

        // 2) Ray để SAMPLE UV brush (child-ray ưu tiên; fallback pointer-ray nếu bật)
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

        if (gotHit && _dragging) _sprayedDuringDrag = true;
        if (!gotHit) OnPointerOffCase();

        LerpTool();
        CheckIdlePulseTick();
    }

    // ============================ Input ============================
    private static bool IsPointerOverUIStandalone()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsTouchOverUI(Touch t)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
    }

    private bool CanStartDragAtPointer(Vector3 screenPos)
    {
        // Nếu đang ở trên UI và cấu hình bỏ qua -> không cho start
        if (ignoreWhenPointerOverUI)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (IsPointerOverUIStandalone()) return false;
#else
            // Mobile: check sẽ thực hiện trong HandlePointer ở nhánh TouchPhase.Began
#endif
        }

        if (!requireStartOnTool) return true;

        var r = worldCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(r, out var h, 1000f, startRayMask))
        {
            if (startDragCollider)
                return h.collider == startDragCollider || h.collider.transform.IsChildOf(startDragCollider.transform);

            return h.collider && (h.collider.transform == sprayTool || h.collider.transform.IsChildOf(sprayTool));
        }
        return false;
    }

     private void HandlePointer(out bool hasPointer, out Vector3 pointer)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        hasPointer = false;
        pointer = default;

        if (Input.GetMouseButtonDown(0))
        {
            _lastInteractionTime = Time.unscaledTime; 
            StopDonePulse();                          
            _armedIdlePulse = false;                  

            if (!CanStartDragAtPointer(Input.mousePosition)) { _dragging = false; return; }
            _dragging = true;
            _sprayedDuringDrag = false;
            PlaySpray();
            _activeFingerId = -1;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            StopSpraySmooth();

            _lastInteractionTime = Time.unscaledTime;
            if (_sprayedDuringDrag) _armedIdlePulse = true;  
            _sprayedDuringDrag = false;
        }

        bool pressed = Input.GetMouseButton(0);
        bool overUI  = ignoreWhenPointerOverUI && IsPointerOverUIStandalone();

        hasPointer = _dragging || (!requireStartOnTool && pressed && !overUI);
        pointer    = hasPointer ? (Vector3)Input.mousePosition : default;
#else
        // ===== MOBILE (KHÔNG hủy kéo khi chạm UI) =====
        hasPointer = false;
        pointer = default;

        if (!_dragging)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;

                _lastInteractionTime = Time.unscaledTime;
                StopDonePulse();
                _armedIdlePulse = false;

                // Chỉ CHẶN lúc bắt đầu nếu đang đè lên UI
                if (ignoreWhenPointerOverUI && IsTouchOverUI(t))
                    continue;

                if (!CanStartDragAtPointer(t.position))
                    continue;

                _dragging = true;
                _sprayedDuringDrag = false;
                PlaySpray();
                _activeFingerId = t.fingerId;
                hasPointer = true;
                pointer = t.position;
                break;
            }
        }
        else
        {
            // Đang kéo: chỉ theo dõi NGÓN TAY ĐÃ CHỌN, bỏ qua UI check
            bool found = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId != _activeFingerId) continue;
                found = true;

                if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
                {
                    _dragging = false;
                    hasPointer = false;
                    StopSpraySmooth();

                    _lastInteractionTime = Time.unscaledTime;
                    if (_sprayedDuringDrag) _armedIdlePulse = true;
                    _sprayedDuringDrag = false;
                }
                else
                {
                    pointer = t.position;
                    hasPointer = true;
                }
                break;
            }

            if (!found)
            {
                _dragging = false;
                hasPointer = false;
            }
        }
#endif
    }

    // ============================ Rays ============================
    private Ray BuildPointerRay(Vector3 pointer) => worldCam.ScreenPointToRay(pointer);

    private Ray BuildChildRay() => new Ray(childRayOrigin.position, childRayOrigin.forward);

    private bool TryPointerHit(Ray pointerRay, out RaycastHit hit)
    {
        if (caseCollider != null)
        {
            // Dùng caseCollider.Raycast để lấy UV chính xác của mesh case
            if (caseCollider.Raycast(pointerRay, out hit, 1000f))
                return true;
        }

        // (Tuỳ chọn) nếu muốn layer mask chung
        // if (Physics.Raycast(pointerRay, out hit, 1000f, caseLayer)) return true;

        hit = default;
        return false;
    }

    // ============================ Drag/Stick/Brush ============================
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
            var p0 = (_lastHitPoint  == Vector3.zero) ? sprayTool.position : _lastHitPoint;
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
            // luôn nằm trên mặt phẳng kéo cố định
            _toolTarget = ProjectToDragPlane(hit.point);
        }
        else
        {
            _toolTarget = hit.point + hit.normal * toolSurfaceOffset;
        }
    }

    private void LerpTool()
    {
        if (!sprayTool) return;
        var target = lockAxis ? ProjectToDragPlane(_toolTarget) : _toolTarget;
        sprayTool.position = Vector3.Lerp(sprayTool.position, target, Time.deltaTime * followSpeed);
    }

    /// <summary>
    /// Lưu normal/point để neo plane; đặt tool (nếu cho phép) và đặt brush theo UV.
    /// </summary>
    private void ApplyHitForBrushAndOptionallyStick(RaycastHit hit, bool fromChildRay)
    {
        _lastHitNormal = hit.normal;
        _lastHitPoint  = hit.point;

        // Khi đang sample từ child-ray, mặc định KHÔNG dán tool theo hit để cảm giác kéo theo tay không bị giật.
        if (!fromChildRay || (fromChildRay && stickToolWhenUsingChildRay))
            StickToolToCase(hit);

        SprayOnCase(hit);
    }

    private void SprayOnCase(RaycastHit hit)
    {
        if (brush == null) return;

        var uv = AdjustUV(hit.textureCoord);
        brush.position = UVToSprayWorld(uv);
        brush.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
    }

    private void OnPointerOffCase()
    {
        if (hideBrushWhenOffCase && brush && sprayCam)
        {
            // Đẩy brush ra khỏi tầm nhìn sprayCam
            brush.position = sprayCam.transform.position - sprayCam.transform.forward * 1000f;
        }
    }

    // ============================ UV helpers ============================
    private Vector2 AdjustUV(Vector2 uv)
    {
        if (swapUV) { float t = uv.x; uv.x = uv.y; uv.y = t; }
        if (flipU) uv.x = 1f - uv.x;
        if (flipV) uv.y = 1f - uv.y;

        if (clampUVToRT)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }
        return uv;
    }

    private Vector3 UVToSprayWorld(Vector2 uv)
    {
        return sprayCam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, brushDepthFromGlueCam));
    }

    // ============================ Particles & Sound ============================
    private void PlaySpray()
    {
        DOVirtual.DelayedCall(0.11f, () =>
        {
            if (_ps != null && !_ps.isPlaying) _ps.Play(true);
        });
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

    bool _spraySoundOn = false;

    private void StartSpraySound()
    {
        if (_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null)
        {
            am.PlaySpray();
            _spraySoundOn = true;
        }
    }

    private void StopSpraySound()
    {
        if (!_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null) am.StopSpray();
        _spraySoundOn = false;
    }
    
    private void CheckIdlePulseTick()
    {
        if (_armedIdlePulse && !_dragging && !_idlePulseActive)
        {
            if (Time.unscaledTime - _lastInteractionTime >= doneIdleDelay)
                StartDonePulse();
        }
    }

    private void StartDonePulse()
    {
        if (_btnDone == null || _idlePulseActive) return;
        _idlePulseActive = true;

        // Lặp: phóng -> thu -> nghỉ -> lặp
        _donePulseSeq = DOTween.Sequence()
            .Append(_btnDone.DOScale(_btnDoneBaseScale * 1.12f, 0.3f).SetEase(Ease.InQuad))
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopDonePulse()
    {
        if (_donePulseSeq != null)
        {
            _donePulseSeq.Kill();
            _donePulseSeq = null;
        }
        _idlePulseActive = false;
        if (_btnDone) _btnDone.localScale = _btnDoneBaseScale;
    }
}
