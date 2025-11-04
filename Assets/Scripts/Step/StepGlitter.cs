using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StepGlitter : StepBase
{
    [Header("Refs")]
    public Camera worldCam;
    public Camera glitterCam;
    public Transform glitterTool;
    public Transform brush;
    public MeshCollider caseCollider;
    public Transform spawnItem;
    public GlitterPreview glitterPreviewPrefab;
    public List<GlitterPreview> glitterPreviews = new List<GlitterPreview>();
    public Material glitterMat;
    public GameObject brushSlot;
    [SerializeField] private float doneIdleDelay = 3f;

    [Header("Options")]
    public float brushDepthFromGlueCam = 1f;
    public float toolSurfaceOffset = 0.002f;
    public bool flipU = false, flipV = false, swapUV = false;
    public bool clampUVToRT = true;
    public bool hideBrushWhenOffCase = false;

    [Header("Feel")]
    public float followSpeed = 18f;

    [Header("UI")]
    public bool cancelDragWhenOverUI = true;
    
    [Header("Axis Lock")]
    [SerializeField] bool lockToolY = true;  
    [SerializeField] float lockedToolY = 7.3f;

    ParticleSystem _ps;
    bool _dragging;
    int _activeFingerId = -1;
    Vector3 _toolTarget;
    private bool _hasLastHit = false;
    private Vector3 _lastHitNormal = Vector3.up;
    private Vector3 _lastHitPoint;
    
    private bool _sprayedDuringDrag = false;  
    private bool _armedIdlePulse = false;     
    private bool _idlePulseActive = false;    
    private float _lastInteractionTime = 0f;  
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;
    
    static readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
        _toolTarget = glitterTool.position;
        _btnDoneBaseScale = _btnDone ? (Vector3)_btnDone.localScale : Vector3.one;
        _lastInteractionTime = Time.unscaledTime;

        // Ban đầu tắt pulse, chờ tương tác mới arm
        _armedIdlePulse = false;
        StopDonePulse();

        if (lockToolY) lockedToolY = glitterTool.position.y;
    }

    void Start() => Initialized();

    public override void SetUp(PhoneCase phoneCase)
    {
        glitterCam.targetTexture = phoneCase._paintRT;
        caseCollider = phoneCase.CaseCollider;
        
        var start = new Vector3(0f, 7.3f, 0f);
        _toolTarget = start;
        glitterTool.position = start;

        // Vào step: coi như vừa tương tác nhưng chưa arm để chờ người dùng
        RegisterInteraction(false);
    }

    public override void CompleteStep()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        gameObject.SetActive(false);
    }

    public void Initialized()
    {
        glitterPreviews.Clear();

        foreach (var data in ItemDataManager.Instance.listGlitter)
            if (data.isUnlock && !UserGameData.IsItemGlitterUnlocked(data.id))
                UserGameData.UnlockItemGlitter(data.id);

        var manager = ItemDataManager.Instance.listGlitter;
        for (int i = 0; i < manager.Count; i++)
        {
            var glitter = Instantiate(glitterPreviewPrefab, spawnItem);
            glitter.SetUp(manager[i], () => Glitter_OnClick(glitter));
            glitter.SetUpUnlock(UserGameData.IsItemGlitterUnlocked(glitter.GlitterData.id));
            glitterPreviews.Add(glitter);
        }

        if (glitterPreviews.Count > 0) Glitter_OnClick(glitterPreviews[0]);
    }

    public void Glitter_OnClick(GlitterPreview p)
    {
        // Click chọn màu: dừng pulse và ARM đếm 3s
        RegisterInteraction(true);

        if (!p.IsUnlock)
        {
          /*  CallAdsManager.ShowRewardVideo("reward", () =>
            {*/
                UserTracking.ItemPick(step.ToString(), p.GlitterData.id);
                glitterMat.color = p.GlitterData.textureColor;
                p.SetUpUnlock(true);
                UserGameData.UnlockItemGlitter(p.GlitterData.id);

                if (_ps) Destroy(_ps.gameObject);
                _ps = Instantiate(p.GlitterData.brush, brushSlot.transform);
                brush = _ps.transform;

                SetEmission(false);
                // Sau rewarded cũng coi như vừa tương tác -> giữ arm 3s
                RegisterInteraction(true);
          //  });
            return;
        }

        UnSelect();
        p.Select();
        glitterMat.color = p.GlitterData.textureColor;

        if (_ps) Destroy(_ps.gameObject);
        _ps = Instantiate(p.GlitterData.brush, brushSlot.transform);
        brush = _ps.transform;

        SetEmission(false);
    }

    public void UnSelect()
    {
        foreach (var glit in glitterPreviews)
            glit.Unselect();
    }

    void Update()
    {
        if (!glitterTool || !worldCam || !glitterCam)
        {
            CheckIdlePulseTick();
            return;
        }
        
        bool hasPointer;
        Vector3 pointer;
        HandleInput(out hasPointer, out pointer);

        if (!hasPointer)
        {
            StopSpray();
            LerpTool();
            CheckIdlePulseTick();
            return;
        }

        var ray = BuildPointerRay(pointer);
        UpdateFreeDragTargetOnPlane(ray);

        if (caseCollider && caseCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
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
    private Ray BuildPointerRay(Vector3 pointer) => worldCam.ScreenPointToRay(pointer);
    // =============== INPUT ===============
    void HandleInput(out bool hasPointer, out Vector3 pointer)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        hasPointer = false; 
        pointer = default;

        if (Input.GetMouseButtonDown(0))
        {
            if (cancelDragWhenOverUI && IsOverUI(Input.mousePosition)) return;

            RegisterInteraction(false); // dừng pulse, CHƯA arm (đợi thả)
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

                // Thả tay: ARM luôn 3s
                RegisterInteraction(true);
                _sprayedDuringDrag = false;
                return;
            }

            pointer = Input.mousePosition;
            hasPointer = Input.GetMouseButton(0);
            if (!hasPointer) _dragging = false;
            return;
        }

        hasPointer = false;
        pointer = default;

#else
        hasPointer = false; 
        pointer = default;

        if (!_dragging)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (cancelDragWhenOverUI && IsOverUI(t.position)) continue;

                RegisterInteraction(false); // dừng pulse, CHƯA arm
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
                    // Thả tay: ARM luôn 3s
                    RegisterInteraction(true);
                    _sprayedDuringDrag = false;
                }
                return;
            }

            _dragging = false;
            hasPointer = false;
        }
#endif
    }

    bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        _uiHits.Clear();
        EventSystem.current.RaycastAll(data, _uiHits);
        return _uiHits.Count > 0;
    }

    // =============== DRAG TOOL ===============
    void UpdateFreeDragTargetOnPlane(Ray ray)
    {
        // Nếu đang khóa Y: ray vào mặt phẳng y = lockedToolY
        if (lockToolY)
        {
            var yPlane = new Plane(Vector3.up, new Vector3(0f, lockedToolY, 0f));
            if (yPlane.Raycast(ray, out float d))
            {
                _toolTarget = ray.GetPoint(d); // y đã đúng, không cần sửa thêm
            }
            return;
        }

        // Không khóa Y: ưu tiên plane từ lần hit gần nhất, fallback về forward cũ
        Plane plane;
        if (_hasLastHit)
            plane = new Plane(_lastHitNormal, _lastHitPoint);
        else if (caseCollider)
            plane = new Plane(caseCollider.transform.forward, caseCollider.bounds.center);
        else
            plane = new Plane(worldCam.transform.forward, glitterTool.position); // fallback an toàn

        if (plane.Raycast(ray, out float dist))
        {
            var p = ray.GetPoint(dist);
            _toolTarget = p;
        }
    }

    void StickToolToCase(RaycastHit hit)
    {
        var p = hit.point + hit.normal * toolSurfaceOffset;
        if (lockToolY) p.y = lockedToolY;      // giữ nguyên Y khi bám vào bề mặt
        _toolTarget = p;
        
        _lastHitNormal = hit.normal;
        _lastHitPoint  = hit.point;
        _hasLastHit    = true;
    }

    void LerpTool()
    {
        var target = _toolTarget;
        if (lockToolY) target.y = lockedToolY;
        glitterTool.position = Vector3.Lerp(glitterTool.position, target, Time.deltaTime * followSpeed);
    }

    // =============== SPRAY / BRUSH ===============
    void SprayOnCase(RaycastHit hit)
    {
        if (!_ps || !brush || !glitterCam) return;

        Vector2 uv = AdjustUV(hit.textureCoord);
        brush.position = glitterCam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, brushDepthFromGlueCam));
        SetEmission(true);
    }

    void OnPointerOffCase()
    {
        SetEmission(false);
        if (hideBrushWhenOffCase && brush && glitterCam)
            brush.position = glitterCam.transform.position - glitterCam.transform.forward * 1000f;

        if (brush)
        {
            var p = brush.position;
            if (lockToolY) p.y = lockedToolY;
            brush.position = p;
        }
    }

    Vector2 AdjustUV(Vector2 uv)
    {
        if (swapUV) { float t = uv.x; uv.x = uv.y; uv.y = t; }
        if (flipU) uv.x = 1f - uv.x;
        if (flipV) uv.y = 1f - uv.y;
        if (clampUVToRT) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); }
        return uv;
    }

    void SetEmission(bool on)
    {
        if (!_ps) return;

        var em = _ps.emission;
        em.enabled = on;

        if (on)
        {
            // Có hoạt động xịt trong lúc kéo -> coi là đang tương tác
            _sprayedDuringDrag = true;
            _lastInteractionTime = Time.unscaledTime;

            if (!_ps.isEmitting)
            {
                DOVirtual.DelayedCall(0.11f, () => { if (_ps) _ps.Play(); });
                StartSpraySound();
            }
        }
        else
        {
            if (_ps.isEmitting) _ps.Stop();
        }
    }

    void StopSpray()
    {
        if (!_ps) return;
        var em = _ps.emission;
        em.enabled = false;
        if (_ps.isEmitting) _ps.Stop();
        StopSpraySound();
    }

    bool _spraySoundOn = false;
    
    private void StartSpraySound()
    {
        if (_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null)
        {
            am.PlayGlitter();
            _spraySoundOn = true;
        }
    }

    private void StopSpraySound()
    {
        if (!_spraySoundOn) return;
        var am = AudioManager.Instance;
        if (am != null) am.StopGlitter();
        _spraySoundOn = false;
    }
    
    // =============== Idle Pulse ===============
    private void RegisterInteraction(bool armAfter)
    {
        StopDonePulse();                       // dừng pulse ngay
        _lastInteractionTime = Time.unscaledTime;
        _armedIdlePulse = armAfter;            // true = sẽ bật lại sau 3s không tương tác
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

        _donePulseSeq = DOTween.Sequence()
            .Append(_btnDone.DOScale(_btnDoneBaseScale * 1.12f, 0.3f).SetEase(Ease.OutQuad))
            .Append(_btnDone.DOScale(_btnDoneBaseScale, 0.3f).SetEase(Ease.InQuad))
            .SetLoops(-1, LoopType.Restart);
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
