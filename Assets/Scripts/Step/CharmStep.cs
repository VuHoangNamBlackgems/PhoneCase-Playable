using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CharmStep : StepBase
{
    [Header("Scene Refs")]
    public Camera gameplayCam;
    public Collider caseSurface;
    public Transform caseRoot;
    public Transform spawnCenter;
    
    public Transform spawnItem;
    public CharmPreview charmPreviewPrefab;
    public List<CharmPreview> charmPreviews = new List<CharmPreview>();
    DraggableOnSurface current;
    private PhoneCase phoneCase;
    private bool isComplete = false;

    [Header("Idle Pulse")]
    [SerializeField] private float doneIdleDelay = 3f;
    private bool _armedIdlePulse = false;
    private bool _idlePulseActive = false;
    private float _lastInteractionTime = 0f;
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;

    void Awake()
    {
        // Publish context
        CharmRuntime.Cam         = gameplayCam;
        CharmRuntime.CaseSurface = caseSurface;
        CharmRuntime.CaseRoot    = caseRoot;
        CharmRuntime.SpawnCenter = spawnCenter;

        _btnDoneBaseScale = _btnDone ? (Vector3)_btnDone.localScale : Vector3.one;
        _lastInteractionTime = Time.unscaledTime;

        // Mới vào step: tắt pulse, chờ có tương tác rồi mới đếm idle
        _armedIdlePulse = false;
        StopDonePulse();
    }

    void OnEnable()  => CharmEvents.OnSpawnRequested += Spawn;
    void OnDisable() => CharmEvents.OnSpawnRequested -= Spawn;

    private void Start()
    {
        Initialized();
    }
    
    public override void SetUp(PhoneCase phoneCase)
    {
        isComplete = false;
        this.phoneCase = phoneCase;
        caseSurface = phoneCase.CaseCollider;
        caseRoot = phoneCase.transform;
        spawnCenter = phoneCase.Sticker3DSlot.transform;
        Gameplay.instance.Preview();
        UnSelect();
    }

    public override void CompleteStep()
    {
        StopDonePulse();
        _armedIdlePulse = false;
        isComplete = true;
        current?.Disable();
        phoneCase.DragRotate = false;
        phoneCase.transform.DORotate(Vector3.zero, 0.5f);
        gameObject.SetActive(false);
    }
    
    public void Initialized()
    {
        charmPreviews.Clear();
        var manager = ItemDataManager.Instance.listCharm;

        foreach (var data in manager)
            if (data.isUnlock && !UserGameData.IsItemSticker3DUnlocked(data.id))
                UserGameData.UnlockItemSticker3D(data.id);

        for (int i = 0; i < manager.Count; i++)
        {
            var charm = Instantiate(charmPreviewPrefab, spawnItem);
            charm.SetUp(manager[i]);
            charm.SetUpUnlock(UserGameData.IsItemSticker3DUnlocked(charm.definition.id));
            charmPreviews.Add(charm);
        }
    }

    private void Update()
    {
        if (isComplete) return;

        // Luôn tick idle
        CheckIdlePulseTick();

        // Khóa xoay khi đang kéo; đồng thời reset idle khi có dragging
        if (current)
        {
            if (current.Dragging)
            {
                phoneCase.DragRotate = false;
                RegisterInteraction(); // đang tương tác → dừng pulse + reset 3s
            }
            else
            {
                phoneCase.DragRotate = true;
            }
        }
    }

    void Spawn(CharmDefinitionDataSO def, CharmPreview p)
    {
        // Vừa click spawn: coi như user tương tác → dừng pulse ngay + reset 3s
        RegisterInteraction();

        if (!p.IsUnlock)
        {
         
                if (!def || !def.prefab) return;
                if (current) Destroy(current.gameObject);
                p.SetUpUnlock(true);
                UserTracking.ItemPick(step.ToString(), p.definition.id);
                UnSelect();
                p.Select(true);
                UserGameData.UnlockItemSticker3D(def.id); // <- fix đúng type

                current = Instantiate(def.prefab, spawnCenter);
                current.backSurface = (MeshCollider)caseSurface;
                current.transform.localPosition = Vector3.zero;  // <- dùng local
                current.transform.localScale = Vector3.one;

                EnsureTouchCollider(current.gameObject);
                TryBindSoftBone(current.gameObject, caseRoot);

                // Đặt vào giữa lưng case ngay
                current.MoveIntoCaseAtBackSurfaceCenter(true);
            return;
        }

        if (!def || !def.prefab) return;
        if (current) Destroy(current.gameObject);
        UnSelect();
        p.Select(true);
        current = Instantiate(def.prefab, spawnCenter);
        current.backSurface = (MeshCollider)caseSurface;
        current.transform.localPosition = Vector3.zero;      // <- dùng local
        current.transform.localScale = Vector3.one;

        EnsureTouchCollider(current.gameObject);
        TryBindSoftBone(current.gameObject, caseRoot);
        current.MoveIntoCaseAtBackSurfaceCenter(true);
    }

    void UnSelect()
    {
        if(charmPreviews.Count < 0) return;
        foreach (var charm in charmPreviews)
        {
            charm.Select(false);
        }
    }

    void EnsureTouchCollider(GameObject go)
    {
        if (go.GetComponent<Collider>()) return;

        var r = go.GetComponentInChildren<Renderer>();
        Bounds b = r ? r.bounds : new Bounds(go.transform.position, Vector3.one * 0.1f);

        var col = go.AddComponent<SphereCollider>();
        col.center = go.transform.InverseTransformPoint(b.center);
        col.radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 0.6f;
        col.isTrigger = false;
    }

    void TryBindSoftBone(GameObject go, Transform simulateSpace)
    {
        var comps = go.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            var t = c.GetType();
            if (t.Name == "EZSoftBone")
            {
                var p = t.GetProperty("SimulateSpace");
                if (p != null) p.SetValue(c, simulateSpace);
            }
        }
    }

    // ===== Idle Pulse =====
    private void RegisterInteraction()
    {
        StopDonePulse();                // dừng scale ngay
        _armedIdlePulse = true;         // cho phép nháy lại sau idle
        _lastInteractionTime = Time.unscaledTime; // reset 3s
    }

    private void CheckIdlePulseTick()
    {
        if (_armedIdlePulse && !_idlePulseActive)
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
            .Append(_btnDone.DOScale(_btnDoneBaseScale * 1.12f, 0.3f).SetEase(Ease.InQuad))
            .Append(_btnDone.DOScale(_btnDoneBaseScale, 0.3f).SetEase(Ease.OutQuad))
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
