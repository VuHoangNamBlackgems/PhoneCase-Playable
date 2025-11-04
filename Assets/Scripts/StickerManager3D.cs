using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StickerManager3D : StepBase
{
    // ============================ 3D / Case ============================
    [Header("3D / Case")] public Camera mainCam;
    public MeshCollider caseCollider;
    public RenderTexture stickerRT;
    public Material caseMaterial;
    public Shader stampShader;

    [Header("Item UI")] public Transform spawnItem;
    public ItemPreview itemPreviewPrefab;
    public List<ItemPreview> itemPreviews = new List<ItemPreview>();
    public ScrollRect scrollRect;

    [Header("UI Gizmo (đặt sẵn)")] public Canvas uiCanvas; // Canvas Overlay
    public StickerGizmoUI gizmoUI; // Để sẵn, inactive
    public GraphicRaycaster[] raycastersToDisable; // UI khác có thể chặn touch (optional)
    [SerializeField] int gizmoCanvasSortOrder = 50000;

    // ============================ Done Pulse ============================
    [Header("Done Button Nudging")] [SerializeField]
    private float doneIdleDelay = 3f; // Sau 3s idle thì nháy

    [SerializeField] private float donePulseScale = 1.12f; // Độ phóng to
    [SerializeField] private float donePulseDuration = 0.25f; // Thời gian scale
    [SerializeField] private float donePulseInterval = 1.5f; // Nghỉ giữa 2 nhịp

    // ============================ Runtime ============================
    private Material stampMat;
    private int selected = -1;
    private float _lastInteractionTime;
    private bool _idlePulseActive;
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;
    ItemPreview _itemPreview;

    // ===== Dữ liệu 1 sticker =====
    [Serializable]
    public class SItem
    {
        public Texture2D tex;
        public Vector2 uv;
        public float sizeU;
        public float rotDeg;
        public Vector3 worldPos;

        public float refSizeU;
        public float refScreenPx;

        public float Aspect => tex ? (float)tex.height / tex.width : 1f;

        public bool ContainsUV(Vector2 uvTest)
        {
            Vector2 d = uvTest - uv;
            float c = Mathf.Cos(-rotDeg * Mathf.Deg2Rad);
            float s = Mathf.Sin(-rotDeg * Mathf.Deg2Rad);
            Vector2 local = new Vector2(c * d.x - s * d.y, s * d.x + c * d.y);
            local.x = local.x / sizeU + 0.5f;
            local.y = local.y / (sizeU * Aspect) + 0.5f;
            return (local.x >= 0 && local.x <= 1 && local.y >= 0 && local.y <= 1);
        }
    }

    public readonly List<SItem> items = new List<SItem>();

    static readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    public static bool IsOverUI(Vector2 screenPos, Canvas canvas = null)
    {
        if (!EventSystem.current) return false;
        var ed = new PointerEventData(EventSystem.current) { position = screenPos };
        _uiHits.Clear();

        // Ưu tiên raycast qua GraphicRaycaster của canvas gizmo (nếu có)
        if (canvas && canvas.TryGetComponent<GraphicRaycaster>(out var gr) && gr.enabled)
            gr.Raycast(ed, _uiHits);
        else
            EventSystem.current.RaycastAll(ed, _uiHits);

        return _uiHits.Count > 0;
    }

    public Camera CanvasCamera => null; // Overlay

    // ============================ Unity ============================
    void Awake()
    {
        _btnDoneBaseScale = _btnDone ? (Vector3)_btnDone.localScale : Vector3.one;
        _lastInteractionTime = Time.unscaledTime;

        if (stampShader) stampMat = new Material(stampShader);
        if (stickerRT)
        {
            ClearRT();
            if (caseMaterial) caseMaterial.SetTexture("_StickerRT", stickerRT);
        }

        Initialized();
    }

    private void OnEnable()
    {
        MarkInteraction(); // reset idle ngay khi vào step
    }

    void OnDisable() => StopDonePulse();

    void OnDestroy()
    {
        StopDonePulse();
        if (stampMat) Destroy(stampMat);
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        ClearAllStickers();
        stickerRT = phoneCase._stickerRT;
        caseCollider = phoneCase.CaseCollider;

        ClearRT();
        if (caseMaterial) caseMaterial.SetTexture("_StickerRT", stickerRT);
        OrderItem();
        MarkInteraction();
    }
    public void OrderItem()
    {
        var itemUnlocks = itemPreviews.FindAll(x => x.IsUnlock == true);
        var itemRandom = itemPreviews[UnityEngine.Random.Range(0, itemUnlocks.Count)];
        var data = itemRandom.Item;
        CustomerOrderManager.instance.OrderItem(new CustomerOrderManager.OrderEntry(data.id, data._iconPreview));
        itemRandom.transform.SetAsFirstSibling();
    }

    public void CheckOrder(int id)
    {
        CustomerOrderManager.instance.CompleteOrder(id);
    }
    public override void CompleteStep()
    {
        StopDonePulse();
        CustomerOrderManager.instance.HideItem();
        gameObject.SetActive(false);
    }

    public void Initialized()
    {
        itemPreviews.Clear();
        var manager = ItemDataManager.Instance ? ItemDataManager.Instance.listSticker3D : null;
        if (manager == null || !itemPreviewPrefab || !spawnItem) return;

        foreach (var data in manager)
            if (data.isUnlock && !UserGameData.IsItemSticker2DUnlocked(data.id))
                UserGameData.UnlockItemSticker2D(data.id);

        for (int i = 0; i < manager.Count; i++)
        {
            var sticker = Instantiate(itemPreviewPrefab, spawnItem);
            sticker.SetUp(manager[i], uiCanvas, this);
            sticker.SetUpUnlock(UserGameData.IsItemSticker2DUnlocked(sticker.Item.id));
            itemPreviews.Add(sticker);
        }
    }

    // ============================ Update ============================
    void Update()
    {
        // Lăn list cũng tính là tương tác
        if (scrollRect && (scrollRect.velocity.sqrMagnitude > 0.01f)) MarkInteraction();

        // Click chọn trên mesh (nếu không bấm lên UI)
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (!(UnityEngine.EventSystems.EventSystem.current &&
                  UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
            {
                MarkInteraction();
                TrySelectByClick(Input.mousePosition);
            }
        }
#else
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            int id = Input.GetTouch(0).fingerId;
                MarkInteraction();
                TrySelectByClick(Input.GetTouch(0).position);
        }
#endif
        CheckIdlePulseTick();
    }

    // ============================ Public API ============================
    public int AddSticker(int id,Texture2D tex, Vector2 uv, Vector3 worldPoint,
        float uvWidth, float pixelWidth, float rotDeg = 0f)
    {
        if (!tex) return -1;

        var it = new SItem
        {
            tex = tex,
            uv = uv,
            worldPos = worldPoint,
            sizeU = Mathf.Clamp01(uvWidth),
            rotDeg = rotDeg,
            refSizeU = Mathf.Max(1e-4f, uvWidth),
            refScreenPx = Mathf.Max(1f, pixelWidth)
        };
        items.Add(it);
        CheckOrder(id);
        RedrawAll();
        Select(items.Count - 1);
        MarkInteraction();

        return items.Count - 1;
    }

    public void TrySelectByClick(Vector2 screenPos)
    {
        if (!RaycastUV(screenPos, out var uv, out var hit)) return;

        for (int i = items.Count - 1; i >= 0; --i)
            if (items[i].ContainsUV(uv))
            {
                Select(i);
                return;
            }

        Select(-1);
    }

    // ===== Edit mode + gizmo =====
    void BeginEdit()
    {
        if (raycastersToDisable != null)
            foreach (var gr in raycastersToDisable)
                if (gr)
                    gr.enabled = false;

        if (gizmoUI && !gizmoUI.gameObject.activeSelf)
        {
            gizmoUI.gameObject.SetActive(true);
            gizmoUI.Open(this);
        }
    }

    public void EndEdit()
    {
        if (gizmoUI && gizmoUI.gameObject.activeSelf)
        {
            Debug.Log("end close");
            gizmoUI.gameObject.SetActive(false);
            gizmoUI.Close();
        }

        if (raycastersToDisable != null)
            foreach (var gr in raycastersToDisable)
                if (gr)
                    gr.enabled = true;
    }

    // Quản lý chọn / di chuyển / scale / xoay / xóa
    public void Select(int idx)
    {
        selected = idx;
        if (selected < 0)
        {
            EndEdit();
            return;
        }

        BeginEdit();
        UpdateGizmoTransform();
    }

    /// Kéo cả khung gizmo -> di chuyển sticker theo con trỏ
    public void DragMoveSelected(Vector2 screenPos)
    {
        if (selected < 0) return;
        if (!RaycastUV(screenPos, out var uv, out var hit)) return;

        var it = items[selected];
        it.uv = uv;
        it.worldPos = hit.point;

        RedrawAll();
        UpdateGizmoTransform();
        MarkInteraction(); // drag => dừng nháy
    }

    public float GetSelectedSize() => (selected < 0) ? 0f : items[selected].sizeU;
    public float GetSelectedRotation() => (selected < 0) ? 0f : items[selected].rotDeg;

    public void SetSelectedSize(float uvWidth)
    {
        if (selected < 0) return;
        items[selected].sizeU = Mathf.Clamp(uvWidth, 0.02f, 0.95f);
        RedrawAll();
        UpdateGizmoTransform();
        MarkInteraction();
    }

    public void SetSelectedRotation(float deg)
    {
        if (selected < 0) return;
        deg = Mathf.Repeat(deg + 180f, 360f) - 180f;
        items[selected].rotDeg = deg;
        RedrawAll();
        UpdateGizmoTransform();
        MarkInteraction();
    }

    public void DeleteSelected()
    {
        if (selected < 0) return;
        items.RemoveAt(selected);
        selected = -1;
        EndEdit();
        RedrawAll();
        MarkInteraction();
    }

    public void ClearAllStickers(bool destroyPreviews = false)
    {
        items.Clear();
        selected = -1;
        EndEdit();
        ClearRT();
        MarkInteraction();
    }

    // ============================ Draw ============================
    public void RedrawAll()
    {
        if (!stickerRT || items == null) return;
        ClearRT();
        if (!stampMat && stampShader) stampMat = new Material(stampShader);

        foreach (var it in items) Stamp(it);
    }

    void Stamp(SItem it)
    {
        if (!stickerRT || !it.tex || !stampMat) return;

        float aspect = it.Aspect;
        stampMat.SetTexture("_MainTex", stickerRT);
        stampMat.SetTexture("_Stamp", it.tex);
        stampMat.SetVector("_Center", new Vector4(it.uv.x, it.uv.y, 0, 0));
        stampMat.SetVector("_Scale", new Vector4(it.sizeU, it.sizeU * aspect, 0, 0));
        stampMat.SetFloat("_Rot", it.rotDeg * Mathf.Deg2Rad);
        stampMat.SetColor("_Tint", Color.white);

        var tmp = RenderTexture.GetTemporary(stickerRT.width, stickerRT.height, 0, stickerRT.format);
        Graphics.Blit(stickerRT, tmp, stampMat);
        Graphics.Blit(tmp, stickerRT);
        RenderTexture.ReleaseTemporary(tmp);
    }

    void ClearRT()
    {
        if (!stickerRT) return;
        var old = RenderTexture.active;
        RenderTexture.active = stickerRT;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = old;
    }

    // ============================ Gizmo transform ============================
    void UpdateGizmoTransform()
    {
        if (!gizmoUI || selected < 0) return;
        var it = items[selected];

        // 1) world -> screen -> local (Overlay => camera = null)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)uiCanvas.transform,
            mainCam.WorldToScreenPoint(it.worldPos),
            null,
            out var local);
        gizmoUI.rect.anchoredPosition = local;

        // 2) kích thước
        float scaleFactor = uiCanvas ? uiCanvas.scaleFactor : 1f;
        float px = it.refScreenPx * (it.sizeU / it.refSizeU);
        float padPx = 32f;
        float w = (px + padPx) / scaleFactor;
        float h = ((px * it.Aspect) + padPx) / scaleFactor;
        gizmoUI.rect.sizeDelta = new Vector2(w, h);

        // 3) xoay khung (đảo dấu cho cảm giác)
        gizmoUI.rect.localEulerAngles = new Vector3(0, 0, -it.rotDeg);

        gizmoUI.rect.SetAsLastSibling();
    }

    // ============================ Raycast ============================
    public bool RaycastUV(Vector2 screen, out Vector2 uv, out RaycastHit hit)
    {
        uv = default;
        hit = default;
        if (!mainCam || !caseCollider) return false;

        Ray ray = mainCam.ScreenPointToRay(screen);
        if (caseCollider.Raycast(ray, out hit, 100f))
        {
            uv = hit.textureCoord;
            return true;
        }

        return false;
    }

    // ============================ Idle Pulse ============================
    // Gọi khi có bất kỳ tương tác nào: click, drag, scroll, nút UI...
    public void MarkInteraction()
    {
        _lastInteractionTime = Time.unscaledTime;
        StopDonePulse(); // dừng nháy ngay khi có tương tác
    }

    private void CheckIdlePulseTick()
    {
        if (!_btnDone) return;
        if (_idlePulseActive) return;

        if (Time.unscaledTime - _lastInteractionTime >= doneIdleDelay)
            StartDonePulse();
    }

    private void StartDonePulse()
    {
        if (!_btnDone || _idlePulseActive) return;
        _idlePulseActive = true;

        _donePulseSeq = DOTween.Sequence()
            .Append(_btnDone.DOScale(_btnDoneBaseScale * donePulseScale, donePulseDuration).SetEase(Ease.OutBack))
            .Append(_btnDone.DOScale(_btnDoneBaseScale, donePulseDuration * 0.8f).SetEase(Ease.InBack))
            .AppendInterval(donePulseInterval)
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