using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class AcryliicStep : StepBase
{
    // ============================ Refs ============================
    [Header("Refs")]
    [SerializeField] private Camera worldCam;
    [SerializeField] private Camera acrylicCam;
    [SerializeField] private Transform painBrush;
    [SerializeField] private Transform brush;
    [SerializeField] private MeshCollider caseCollider;
    [SerializeField] private Transform spawnItem;
    [SerializeField] private SprayPreview acrylicPreviewPrefab;
    [SerializeField] private List<SprayPreview> acrylicPreviews = new List<SprayPreview>();
    [SerializeField] private Material paintBrushMat;
    [SerializeField] private GameObject brushSlot;
    [SerializeField] private float doneIdleDelay = 3f;
    // ============================ Options ============================
    [Header("Options")] [Tooltip("Độ sâu đặt brush trong không gian SprayCam (giữa near/far)")] [SerializeField]
    private float brushDepthFromGlueCam = 1f;

    [Tooltip("Đẩy tool nổi lên khỏi mặt ốp")] [SerializeField]
    private float toolSurfaceOffset = 0.002f;

    [Tooltip("Sửa UV khi 2 project khác nhau")] [SerializeField]
    private bool flipU = false, flipV = false, swapUV = false;

    [Tooltip("Kẹp UV vào [0..1]")] [SerializeField]
    private bool clampUVToRT = true;

    [Tooltip("Ra ngoài case thì ẩn brush")] [SerializeField]
    private bool hideBrushWhenOffCase = false;

    [Header("Feel")] [Tooltip("Tốc độ follow tool (lerp)")] [SerializeField]
    private float followSpeed = 18f;

    [Header("Input Rules")] [Tooltip("Bỏ qua input 3D nếu con trỏ đang ở trên UI")] [SerializeField]
    private bool ignoreWhenPointerOverUI = true;

    [Tooltip("Chỉ bắt đầu kéo nếu bấm trúng collider của tool/child")] [SerializeField]
    private bool requireStartOnTool = false;

    [Tooltip("Collider để yêu cầu bấm trúng trước khi kéo (tuỳ chọn)")] [SerializeField]
    private Collider startDragCollider;

    [Header("Start Drag Filter")]
    [Tooltip("LayerMask để kiểm tra bắt đầu kéo (tránh dính collider nền)")]
    [SerializeField]
    private LayerMask startRayMask = ~0;

    [Header("Child Ray (optional)")] [Tooltip("Bắn ray từ child (nozzle) thay vì từ camera")] [SerializeField]
    private bool useChildRay = false;

    [SerializeField] private Transform childRayOrigin; // ví dụ đầu vòi
    [SerializeField] private float childRayDistance = 2f;
    [SerializeField] private LayerMask caseLayer = ~0;

    // ============================ Runtime ============================
    private ParticleSystem _ps; // brush particle
    private ParticleSystem _spray; // effect particle (tia xịt)
    private bool _dragging;
    private int _activeFingerId = -1;
    private Vector3 _toolTarget;

    // Nhớ thông tin hit cuối để dựng plane kéo ổn định
    private Vector3 _lastHitNormal = Vector3.up;
    private Vector3 _lastHitPoint;
    
    private bool _sprayedDuringDrag = false;  
    private bool _armedIdlePulse = false;     
    private bool _idlePulseActive = false;    
    private float _lastInteractionTime = 0f;  
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;

    // ============================ Unity ============================
    private void Awake()
    {
        _toolTarget = painBrush ? painBrush.position : Vector3.zero;
        
        _btnDoneBaseScale = _btnDone ? (Vector3)_btnDone.localScale : Vector3.one;
        _lastInteractionTime = Time.unscaledTime;
        _armedIdlePulse = false;    
        StopDonePulse();
        
        BuildUIListAndSelectFirst();
    }
    
    private void RegisterInteraction(bool armAfter = false)
    {
        // Dừng pulse ngay khi có tương tác
        StopDonePulse();
        _lastInteractionTime = Time.unscaledTime;

        // armAfter = true => cho phép tự bật lại sau 3s không tương tác
        _armedIdlePulse = armAfter;
    }
    
    public override void SetUp(PhoneCase phoneCase)
    {
        // Gắn RT cho sprayCam (giả định sprayCam đã có targetTexture)
        acrylicCam.targetTexture = phoneCase._paintRT;
        painBrush.transform.position = new Vector3(0, 7.3f, 0);
        Debug.Log(acrylicCam.targetTexture);
        caseCollider = phoneCase.CaseCollider;

        var start = new Vector3(0f, 7.3f, 0f);

        _toolTarget = start;
        painBrush.position = start;

        if (acrylicPreviews.Count > 0)
            OnSelectSpray(acrylicPreviews[0]);
    }

    public override void CompleteStep()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StopSprayImmediate();
    }

    private void OnDisable()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        StopSprayImmediate();
    }

    // ============================ UI List ============================
    private void BuildUIListAndSelectFirst()
    {
     /*   acrylicPreviews.Clear();
        var manager = ItemDataManager.Instance.listAcrylic;
        foreach (var data in ItemDataManager.Instance.listAcrylic)
            if (data.isUnlock && !UserGameData.IsItemAcrylicUnlocked(data.id))
                UserGameData.UnlockItemAcrylic(data.id);

        for (int i = 0; i < manager.Count; i++)
        {
            var painBrush = Instantiate(acrylicPreviewPrefab, spawnItem);
            painBrush.SetUp(manager[i], () => { OnSelectSpray(painBrush); });
            painBrush.SetUpUnlock(UserGameData.IsItemAcrylicUnlocked(painBrush.SprayData.id));
            acrylicPreviews.Add(painBrush);
        }

        if (acrylicPreviews.Count > 0)
            OnSelectSpray(acrylicPreviews[0]);*/
    }

    private void OnSelectSpray(SprayPreview p)
    {
        if (!p.IsUnlock)
        {
         /*   CallAdsManager.ShowRewardVideo("reward", () =>
            {
                paintBrushMat.color = p.SprayData.textureColor;
                UserTracking.ItemPick(step.ToString(), p.SprayData.id);
                if (_ps) Destroy(_ps.gameObject);
                UnSelect();
                p.Select();
                _ps = Instantiate(p.SprayData.brush, brushSlot.transform);
                brush = _ps.transform;

                StopSprayImmediate();
            });*/
            return;
        }

        paintBrushMat.color = p.SprayData.textureColor;

        if (_ps) Destroy(_ps.gameObject);
        UnSelect();
        p.Select();
        _ps = Instantiate(p.SprayData.brush, brushSlot.transform);
        brush = _ps.transform;

        StopSprayImmediate();
    }

    void UnSelect()
    {
        foreach (var acrylic in acrylicPreviews)
        {
            acrylic.Unselect();
        }
    }
    
    // ============================ Update Loop ============================
    private void Update()
    {
        if (painBrush == null || worldCam == null || acrylicCam == null)
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

        // Dựng ray
        var ray = BuildRayFromPointerOrChild(pointer);

        // Khi đang kéo, cập nhật target theo plane (dựa vào hit gần nhất)
        UpdateFreeDragTargetOnPlane(ray);

        // Raycast vào case
        bool hitCase = false;
        if (caseCollider && caseCollider.Raycast(ray, out var hit, useChildRay ? childRayDistance : 1000f))
        {
            hitCase = true;
            _lastHitNormal = hit.normal;
            _lastHitPoint = hit.point;

            StickToolToCase(hit);
            SprayOnCase(hit);
        }
        else
        {
            OnPointerOffCase();
        }

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
        if (!requireStartOnTool) return true;

        var r = worldCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(r, out var h, 1000f, startRayMask))
        {
            if (startDragCollider)
                return h.collider == startDragCollider || h.collider.transform.IsChildOf(startDragCollider.transform);

            return h.collider && (h.collider.transform == painBrush || h.collider.transform.IsChildOf(painBrush));
        }

        return false;
    }

    private void HandlePointer(out bool hasPointer, out Vector3 pointer)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        hasPointer = false;
        pointer = default;

        // BẮT ĐẦU KÉO (chặn nếu đang ở trên UI)
        if (Input.GetMouseButtonDown(0))
        {
            if (ignoreWhenPointerOverUI && IsPointerOverUIStandalone()) return;
            if (!CanStartDragAtPointer(Input.mousePosition)) return;

            RegisterInteraction(armAfter: false); // dừng pulse, chưa arm
            _dragging = true;
            _activeFingerId = -1;
            hasPointer = true;
            pointer = Input.mousePosition;
            _sprayedDuringDrag = false;
            return;
        }

        if (_dragging)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                hasPointer = false;

                // ✅ luôn arm sau khi thả tay
                RegisterInteraction(armAfter: true);
                _sprayedDuringDrag = false;
                return;
            }

            hasPointer = Input.GetMouseButton(0);
            pointer = Input.mousePosition;
            if (!hasPointer) _dragging = false;
            return;
        }


        // Không kéo
        hasPointer = false;
        pointer = default;

#else
    hasPointer = false;
    pointer = default;

    // BẮT ĐẦU KÉO (chỉ nhận touch Began, và không cho bắt đầu trên UI)
    if (!_dragging)
{
    for (int i = 0; i < Input.touchCount; i++)
    {
        var t = Input.GetTouch(i);
        if (t.phase != TouchPhase.Began) continue;

        if (ignoreWhenPointerOverUI && IsTouchOverUI(t)) continue;
        if (!CanStartDragAtPointer(t.position)) continue;

        RegisterInteraction(armAfter: false); // dừng pulse, chưa arm
        _dragging = true;
        _sprayedDuringDrag = false;
        _activeFingerId = t.fingerId;
        hasPointer = true;
        pointer = t.position;
        return;
    }
}
else
{
    for (int i = 0; i < Input.touchCount; i++)
    {
        var t = Input.GetTouch(i);
        if (t.fingerId != _activeFingerId) continue;

        pointer = t.position;
        bool alive = (t.phase != TouchPhase.Canceled && t.phase != TouchPhase.Ended);
        hasPointer = alive;
        if (!alive)
        {
            _dragging = false;
            // ✅ luôn arm sau khi thả tay
            RegisterInteraction(armAfter: true);
        }
        return;
    }

    _dragging = false;
    hasPointer = false;
}

#endif
    }


    private Ray BuildRayFromPointerOrChild(Vector3 pointer)
    {
        if (useChildRay && childRayOrigin)
        {
            // Child ray: nếu không hit, frame sau UpdateFreeDragTargetOnPlane sẽ dùng plane cũ.
            return new Ray(childRayOrigin.position, childRayOrigin.forward);
        }

        return worldCam.ScreenPointToRay(pointer);
    }

    // ============================ Drag/Stick ============================
    private void UpdateFreeDragTargetOnPlane(Ray ray)
    {
        // Dùng plane theo normal & point gần nhất để hạn chế "kéo 1 trục"
        if (_lastHitNormal == Vector3.zero) _lastHitNormal = caseCollider ? caseCollider.transform.forward : Vector3.up;
        if (_lastHitPoint == Vector3.zero)
            _lastHitPoint = caseCollider ? caseCollider.bounds.center : painBrush.position;

        var plane = new Plane(_lastHitNormal, _lastHitPoint);
        if (plane.Raycast(ray, out float dist))
        {
            _toolTarget = ray.GetPoint(dist);
        }
    }

    private void StickToolToCase(RaycastHit hit)
    {
        _toolTarget = hit.point + hit.normal * toolSurfaceOffset;
    }

    private void LerpTool()
    {
        if (!painBrush) return;
        painBrush.position = Vector3.Lerp(painBrush.position, _toolTarget, Time.deltaTime * followSpeed);
    }

    private void SprayOnCase(RaycastHit hit)
    {
        if (brush == null) return;

        // Vị trí từ UV theo glue cam
        var uv = AdjustUV(hit.textureCoord);
        brush.position = UVToGlueWorld(uv);

        // Xoay theo normal bề mặt (nếu particle nhận rotation)
        brush.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        // Bật xịt
        PlaySpray();
    }

    private void OnPointerOffCase()
    {
        StopSpraySmooth();

        if (hideBrushWhenOffCase && brush)
        {
            // Đẩy brush ra khỏi tầm nhìn sprayCam
            brush.position = acrylicCam.transform.position - acrylicCam.transform.forward * 1000f;
            var vector3 = brush.position;
            vector3.y = 0;
            brush.position = vector3;
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
        return acrylicCam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, brushDepthFromGlueCam));
    }

    // ============================ Particles ============================
    private void PlaySpray()
    {
        if (_ps != null && !_ps.isPlaying) _ps.Play(true);
        if (_spray != null && !_spray.isPlaying) _spray.Play(true);
        StartSpraySound();
        _sprayedDuringDrag = true;
        _lastInteractionTime = Time.unscaledTime;
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
            am.PlayAcrylic();
            _spraySoundOn = true;
        }
    }

    private void StopSpraySound()
    {
        if (!_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null) am.StopAcrylic();
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