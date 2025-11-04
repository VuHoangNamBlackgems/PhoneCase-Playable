using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PhoneCase : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] MeshCollider _caseCollider;
    [SerializeField] MeshCollider _glueCollider;
    [SerializeField] GameObject _center;

    [Header("Meshes & Parts")]
    [SerializeField]
    private Renderer _caseMesh;

    [SerializeField] private Renderer _caseGlueMesh;

    [Header("Visual/Material")]
    [SerializeField]
    private Material _CaseMaterial;

    [SerializeField] private Material _CaseGlueMaterial;

    public RenderTexture _paintRT;
    public RenderTexture _glueRT;
    public RenderTexture _stickerRT;

    public bool isCasePopIt = false;
    public List<SkinnedMeshRenderer> listPopIt = new List<SkinnedMeshRenderer>();

    [Header("Flip Anchors (điểm đích để xoay)")]
    [SerializeField]
    private Transform _verticalAnchor;

    [SerializeField] private Transform _horizonAnchor;
    [SerializeField] private Transform _previewAnchor;
    [SerializeField] private Transform _screwAnchor;

    [SerializeField] private float _flipDuration = 0.4f;
    [SerializeField] private AnimationCurve _flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private GameObject _phoneStrapSlot;
    [SerializeField] private GameObject _sticker3DSlot;
    [SerializeField] private GameObject _currentStrap;

    [SerializeField] CasePaintRTBinder _casePaintBinder;

    // ================= Drag -> Rotate =================
    [Header("Drag Rotate")]
    [SerializeField]
    bool dragRotate = false;

    [SerializeField] float yawPerPixel = 0.25f;
    [SerializeField] bool invertDragX = false;
    [SerializeField] bool cancelDragWhenOverUI = true;

    [Header("Fix Phone")]
    public bool FixPhone = false;
    [SerializeField] Transform glassNewScreen;
    [SerializeField] MeshCollider _screenCollider;
    [SerializeField] Animator _glassAnimator;
    [SerializeField] Transform screenBroken;
    [SerializeField] MeshRenderer screenBrokenMeshRenderer;
    [SerializeField] Transform screenBrokenPosOut;
    [SerializeField] List<ScrewTarget> listScrewTarget;
    [SerializeField] Transform glass;
    [SerializeField] Transform targetCable;
    [SerializeField] Transform targetOutCable;
    [SerializeField] Transform sphereCable;
    [SerializeField] SkinnedMeshRenderer cableMesh;
    [SerializeField] Transform socket;
    [SerializeField] private Material screenMat;
    [SerializeField] Texture screenTexture;
    [SerializeField] Transform newScreen;
    [SerializeField] Texture screenBrokenTexture;
    public Vector3 ScreenBrokenPosIn;

    public bool DragRotate
    {
        get => dragRotate;
        set => dragRotate = value;
    }
    public MeshCollider ScreenCollider => _screenCollider;
    public Transform GlassNewScreen => glassNewScreen;
    public CasePaintRTBinder CasePaintBinder => _casePaintBinder;
    bool _dragging;
    int _activeFingerId = -1; // -1 = mouse
    Vector2 _lastScreenPos;
    bool _isFlipping;

    public Animator GlassAnimator => _glassAnimator;
    public MeshCollider CaseCollider => _caseCollider;
    public MeshCollider GlueCollider => _glueCollider;
    public GameObject PhoneStrapSlot => _phoneStrapSlot;
    public GameObject Sticker3DSlot => _sticker3DSlot;

    public Material CaseMaterial
    {
        get => _CaseMaterial;
        set => _CaseMaterial = value;
    }

    public Transform ScreenBroken => screenBroken;
    public SkinnedMeshRenderer CableMesh => cableMesh;
    public Transform NewScreen => newScreen;
    public Transform ScreenBrokenPosOut => screenBrokenPosOut;
    public List<ScrewTarget> ListScrewTarget => listScrewTarget;
    public Transform Glass => glass;
    public Transform TargetCable => targetCable;
    public Transform TargetOutCable => targetOutCable;
    public Transform SphereCable => sphereCable;
    public Transform Socket => socket;

    public bool IsVertical;

    private void Awake()
    {
        //_casePaintBinder = _caseMesh.GetComponent<CasePaintRTBinder>();
    }

    private void Start()
    {
        if (FixPhone)
        {

        }
        else
        {
            SetupMaterialAndRenderTexture();
            SetUpPopItNode();
        }
    }

    // ====== API ======

    public void SetupMaterialAndRenderTexture()
    {
        if (_casePaintBinder == null || _caseMesh == null) return;

        // Lấy RT từ Binder (Binder chỉ giữ vòng đời RT)
        var paintRT = _casePaintBinder.GetPaintRT();
        var stickerRT = _casePaintBinder.GetStickerRT();

        // Đẩy RT vào mọi material đang hiển thị trên case
        var mats = _caseMesh.materials; // clone array + instance
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (m == null) continue;
            if (m.HasProperty("_PaintTex")) m.SetTexture("_PaintTex", paintRT);
            if (m.HasProperty("_StickerRT")) m.SetTexture("_StickerRT", stickerRT);
            // Nếu shader dùng tên khác, thêm dòng alias tại đây:
            // if (m.HasProperty("_PaintRT")) m.SetTexture("_PaintRT", paintRT);
        }

        _caseMesh.materials = mats; // áp lại

        // Lưu lại tham chiếu để các step khác dùng
        _CaseMaterial = mats[0];
        _paintRT = paintRT;
        _stickerRT = stickerRT;
        Debug.Log($"Mat count: {_caseMesh.materials.Length}");

        // Nếu bạn có _glueRT thì đừng clear nhầm: chỉ clear paint khi cần
        // ClearRT(_paintRT, new Color(0,0,0,0)); // tuỳ logic của bạn
    }

    public void SetupStartFlatPosition(Transform horizontalObj, Transform verticalObj, Transform previewObj, Transform screwObj)
    {
        if (!_horizonAnchor)
            _horizonAnchor = horizontalObj;

        if (!_verticalAnchor)
            _verticalAnchor = verticalObj;

        if (!_previewAnchor)
            _previewAnchor = previewObj;

        if (!_screwAnchor)
            _screwAnchor = screwObj;

        IsVertical = false;
    }

    public void SetUpPopItNode()
    {
        if (!isCasePopIt) return;
        foreach (SkinnedMeshRenderer smr in listPopIt)
        {
            smr.material = CaseMaterial;
        }
    }

    public void SetupHorizonPos(Action callback, bool isEnable = false)
    {
        if (isEnable && !gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(CoFlipToAnchor(_horizonAnchor, false, callback));
    }

    public void SetupScrewPos(Action callback, bool isEnable = false)
    {
        if (isEnable && !gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(CoFlipToAnchor(_screwAnchor, false, callback));
    }

    public void SetUpVerticalPos(Action callback)
    {
        StartCoroutine(CoFlipToAnchor(_verticalAnchor, true, callback));
    }

    public void SetupPreviewPos(Action callback, bool isEnable = false)
    {
        if (isEnable && !gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(CoFlipToAnchor(_previewAnchor, false, callback));
    }

    private IEnumerator CoFlipToAnchor(Transform anchor, bool toVertical, Action callback)
    {
        if (!anchor)
        {
            Debug.LogWarning(
                "[PhoneCase] Anchor null. Gọi SetupStartFlatPosition() trước hoặc gán anchor trong Inspector.");
            callback?.Invoke();
            yield break;
        }

        _isFlipping = true;

        float t = 0f;
        var startPos = transform.position;
        var startRot = transform.rotation;
        var startScale = transform.localScale;

        var endPos = anchor.position;
        var endRot = anchor.rotation;
        var endScale = anchor.localScale;

        while (t < 2f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, _flipDuration);
            float k = _flipCurve.Evaluate(Mathf.Clamp01(t));

            transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPos, endPos, k),
                Quaternion.SlerpUnclamped(startRot, endRot, k));

            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, k);
            yield return null;
        }

        IsVertical = toVertical;

        // khóa lại base yaw sau khi flip xong

        _isFlipping = false;
        callback?.Invoke();
    }

    public void AddAttachment(Transform item)
    {
        if (!item || !Sticker3DSlot) return;
        item.SetParent(Sticker3DSlot.transform, false);
        item.localPosition = Vector3.zero;
        item.localRotation = Quaternion.identity;
        item.localScale = Vector3.one;
    }

    public void AddPhoneStrap(GameObject strap, int index)
    {
        if (!strap || !PhoneStrapSlot) return;
        if (_currentStrap) Destroy(_currentStrap);

        _currentStrap = Instantiate(strap, PhoneStrapSlot.transform);
        _currentStrap.transform.localPosition = Vector3.zero;
        _currentStrap.transform.localRotation = Quaternion.identity;
        _currentStrap.transform.localScale = Vector3.one;
    }

    public void CompletePhone() => ShowCompleteEffect();
    public void ShowCompleteEffect() => SendMessage("OnPhoneCompleted", SendMessageOptions.DontRequireReceiver);

    public void ClearRT(RenderTexture rt, Color color, bool clearDepth = false)
    {
        if (rt == null) return;
        if (!rt.IsCreated()) rt.Create();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(clearDepth, /*clearColor:*/ true, color);
        RenderTexture.active = prev;
    }

    public void SetModeHalloween(float v) => _CaseMaterial.SetFloat("_UsePaintAForReveal", Mathf.Clamp01(v));
    public void SetImageHalloween(Texture tex) => _CaseMaterial.SetTexture("_RevealTex", tex);

    // ================= Drag -> Rotate =================
    void Update()
    {
        if (!dragRotate || _isFlipping) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#endif
        HandleTouch();
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0) && !OverUI(-1))
            BeginDrag(-1, Input.mousePosition);

        if (_dragging && _activeFingerId == -1)
        {
            if (Input.GetMouseButton(0))
                DragTo(Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
                EndDrag();
        }
    }

    void HandleTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);

            if (!_dragging)
            {
                if (t.phase == TouchPhase.Began && !OverUI(t.fingerId))
                    BeginDrag(t.fingerId, t.position);
                continue;
            }

            if (t.fingerId != _activeFingerId) continue;

            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                DragTo(t.position);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                EndDrag();
        }
    }

    void BeginDrag(int id, Vector2 screenPos)
    {
        _dragging = true;
        _activeFingerId = id;
        _lastScreenPos = screenPos;
    }

    void DragTo(Vector2 screenPos)
    {
        float dx = screenPos.x - _lastScreenPos.x;
        float sign = invertDragX ? -1f : 1f;
        float yawDeg = sign * dx * yawPerPixel;

        // Xoay quanh trục Y thế giới, lấy tâm từ _center (nếu có) cho đẹp
        Vector3 pivot = _center ? _center.transform.position : transform.position;
        transform.RotateAround(pivot, Vector3.up, yawDeg);

        _lastScreenPos = screenPos;
    }

    void EndDrag()
    {
        _dragging = false;
        _activeFingerId = -1;
    }

    bool OverUI(int pointerId)
    {
        if (!cancelDragWhenOverUI) return false;
        if (EventSystem.current == null) return false;
        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    void OnDisable() => EndDrag();

    #region Screen Hinge

    public enum HingeSide
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [Header("Screen Hinge (open/close)")]
    [SerializeField]
    Transform screen; // mặt kính/màn hình

    [SerializeField] Transform hingePivot; // pivot bản lề (optional: auto)
    [SerializeField] Transform screenRestSpot; // chỗ đặt tạm sang 1 bên sau khi mở (optional)
    [SerializeField] Transform screenClosedSpot; // pose khi đã đóng (optional)

    [Header("Hinge Pivot Auto")]
    [SerializeField]
    bool autoCreatePivotIfNull = true;

    [SerializeField] HingeSide hingeSide = HingeSide.Left;
    [SerializeField] float pivotInset = 0f; // đẩy pivot vào theo local

    [Header("Open Defaults")]
    [SerializeField]
    float hingeOpenAngle = 95f; // Left/Right: quay Y | Top/Bottom: quay X

    [SerializeField] float hingeOpenDuration = 0.6f;
    [SerializeField] Ease hingeOpenEase = Ease.InOutSine;
    [SerializeField] float hingePlaceAsideDuration = 0.35f;
    [SerializeField] Ease hingePlaceAsideEase = Ease.OutCubic;

    [Header("Close Defaults")]
    [SerializeField]
    float hingePickDuration = 0.25f; // bắt mép về pivot trước khi parent

    [SerializeField] Ease hingePickEase = Ease.OutCubic;
    [SerializeField] float hingeCloseDuration = 0.6f;
    [SerializeField] Ease hingeCloseEase = Ease.InOutSine;
    [SerializeField] float hingeSnapDuration = 0.25f; // snap về pose đóng
    [SerializeField] Ease hingeSnapEase = Ease.OutCubic;

    public bool ScreenIsOpen { get; private set; }
    public bool HingeBusy { get; private set; }

    Sequence _hingeSeq; // tween hiện tại
    Vector3 _closedLocalEuler; // local euler của pivot khi "đóng"
    bool _closedEulerCaptured;

    // --- Public API ---

    /// Đảm bảo có pivot bản lề. Nếu thiếu thì auto tạo theo mép Mesh.bounds (LOCAL)
    public void EnsureHingePivot()
    {
        if (hingePivot)
        {
            CaptureClosedEulerIfNeeded();
            return;
        }

        if (!autoCreatePivotIfNull || !screen) return;

        var mf = screen.GetComponentInChildren<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        var b = mf.sharedMesh.bounds; // local bounds
        Vector3 localPos = b.center;
        switch (hingeSide)
        {
            case HingeSide.Left: localPos.x = b.min.x + pivotInset; break;
            case HingeSide.Right: localPos.x = b.max.x - pivotInset; break;
            case HingeSide.Top: localPos.y = b.max.y - pivotInset; break;
            case HingeSide.Bottom: localPos.y = b.min.y + pivotInset; break;
        }

        var go = new GameObject("[HingePivot_Auto]");
        hingePivot = go.transform;
        hingePivot.SetParent(screen.parent, worldPositionStays: true);
        hingePivot.position = screen.TransformPoint(localPos);
        hingePivot.rotation = screen.rotation;

        CaptureClosedEulerIfNeeded();
    }

    /// Mở màn: quay quanh pivot → (tuỳ chọn) đặt hẳn sang 1 bên
    public void FlipOpen(bool alsoPlaceAside = true, Transform overrideRestSpot = null, Action onComplete = null)
    {
        if (HingeBusy) return;
        EnsureHingePivot();
        if (!screen || !hingePivot)
        {
            onComplete?.Invoke();
            return;
        }
        AudioManager.Instance.PlayFlip();
        KillHingeSequence();
        HingeBusy = true;

        // Parent screen dưới pivot để quay như bản lề (giữ nguyên world)
        screen.SetParent(hingePivot, true);

        // Chọn trục quay: Left/Right -> Y; Top/Bottom -> X
        bool useY = (hingeSide == HingeSide.Left || hingeSide == HingeSide.Right);
        var targetEuler = _closedLocalEuler + (useY
            ? new Vector3(0f, hingeOpenAngle, 0f)
            : new Vector3(hingeOpenAngle, 0f, 0f));

        _hingeSeq = DOTween.Sequence();
        _hingeSeq.Append(hingePivot.DOLocalRotate(targetEuler, hingeOpenDuration).SetEase(hingeOpenEase));

        // Đặt sang một bên
        Transform rest = overrideRestSpot ? overrideRestSpot : screenRestSpot;
        if (alsoPlaceAside && rest)
        {
            _hingeSeq.AppendCallback(() =>
            {
                // tách ra khỏi pivot để move độc lập
                screen.SetParent(rest.parent, true);
            });
            _hingeSeq.Append(screen.DOMove(rest.position, hingePlaceAsideDuration).SetEase(hingePlaceAsideEase));
            _hingeSeq.Join(screen.DORotateQuaternion(rest.rotation, hingePlaceAsideDuration));

        }
        DOVirtual.DelayedCall(0.3f, () =>
        {
            cableMesh.enabled = true;
            Debug.Log("cableMesh.enabled");
        });
        _hingeSeq.OnComplete(() =>
        {
            HingeBusy = false;
            ScreenIsOpen = true;
            onComplete?.Invoke();
        });
    }

    /// Đóng màn: (tuỳ chọn) bắt từ Rest về mép pivot → parent vào pivot → quay về đóng → (tuỳ) snap đúng pose
    public void FlipClose(bool pickFromRestFirst = true, bool alsoSnapClosedSpot = true, Action onComplete = null)
    {
        if (HingeBusy) return;
        EnsureHingePivot();
        if (!screen || !hingePivot)
        {
            onComplete?.Invoke();
            return;
        }
        AudioManager.Instance.PlayFlip();
        KillHingeSequence();
        HingeBusy = true;

        _hingeSeq = DOTween.Sequence();

        // 1) Bắt mép bản lề về đúng vị trí pivot (nếu đang ở aside)
        if (pickFromRestFirst)
        {
            var mf = screen.GetComponentInChildren<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var b = mf.sharedMesh.bounds;
                Vector3 localAnchor = b.center;
                switch (hingeSide)
                {
                    case HingeSide.Left: localAnchor.x = b.min.x + pivotInset; break;
                    case HingeSide.Right: localAnchor.x = b.max.x - pivotInset; break;
                    case HingeSide.Top: localAnchor.y = b.max.y - pivotInset; break;
                    case HingeSide.Bottom: localAnchor.y = b.min.y + pivotInset; break;
                }

                Vector3 anchorWorld = screen.TransformPoint(localAnchor);
                Vector3 delta = hingePivot.position - anchorWorld;
                _hingeSeq.Append(screen.DOMove(screen.position + delta, hingePickDuration).SetEase(hingePickEase));
            }
        }

        _hingeSeq.AppendCallback(() => { screen.SetParent(hingePivot, true); });

        _hingeSeq.Append(
            hingePivot
                .DOLocalRotate(new Vector3(0, -180f, 0f), hingeCloseDuration, RotateMode.LocalAxisAdd)
                .SetEase(hingeCloseEase)
        );


        // 4) (optional) Snap đúng pose đóng
        if (alsoSnapClosedSpot && screenClosedSpot)
        {
            _hingeSeq.AppendCallback(() => { screen.SetParent(screenClosedSpot.parent, true); });
            _hingeSeq.Append(screen.DOMove(screenClosedSpot.position, hingeSnapDuration).SetEase(hingeSnapEase));
            _hingeSeq.Join(screen.DORotateQuaternion(screenClosedSpot.rotation, hingeSnapDuration));
        }
        DOVirtual.DelayedCall(1f, () =>
        {
            cableMesh.enabled = false;
            Debug.Log("cableMesh.enabled");
        });
        _hingeSeq.OnComplete(() =>
        {
            HingeBusy = false;
            ScreenIsOpen = false;
            onComplete?.Invoke();
        });
    }

    public void ChangeScreen()
    {
        //screenMat.SetTexture("_MainTex", screenTexture);
        screenBrokenMeshRenderer.gameObject.SetActive(false);
        newScreen.gameObject.SetActive(true);
    }

    /// Toggle nhanh theo trạng thái hiện tại
    public void FlipToggle()
    {
        if (ScreenIsOpen) FlipClose(true, true, null);
        else FlipOpen(true, null, null);
    }

    // --- Internals ---

    void CaptureClosedEulerIfNeeded()
    {
        if (!hingePivot) return;
        if (!_closedEulerCaptured)
        {
            _closedLocalEuler = hingePivot.localEulerAngles; // coi trạng thái hiện tại là "đã đóng"
            _closedEulerCaptured = true;
        }
    }

    void KillHingeSequence()
    {
        if (_hingeSeq != null && _hingeSeq.IsActive()) _hingeSeq.Kill();
        _hingeSeq = null;
    }

    #endregion
}