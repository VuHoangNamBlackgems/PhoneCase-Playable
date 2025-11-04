using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StepSticker : MonoBehaviour
{
    [Header("UI & Data")]
    [SerializeField] ScrollRect _listAttachmentPreview;
    [SerializeField] GameObject _tempItemPreview;     // prefab thumbnail
    [SerializeField] Canvas _stepUI;
    [SerializeField] Camera _uiCam;
    [SerializeField] Image[] _btnStickers;
    [SerializeField] Sprite[] _spriteBtn;

    [Header("3D/Case")]
    [SerializeField] LayerMask _layerCase;                 // layer của ốp
    [SerializeField] Camera _mainCam;
    [SerializeField] MeshCollider _caseCollider;           // collider ốp
    [SerializeField] Material _caseMat;                    // material ốp đang xài shader đọc _StickerRT

    [Header("RenderTexture")]
    [SerializeField] Texture2D _defaultTextureBlack;
    [SerializeField] Shader _stampShader;                  // DIY/RT_StampSprite_BIRP
    const int TEX_SIZE = 1024;
    RenderTexture _stickerRT;
    Material _stampMat;

    // runtime
    ItemSticker _currentItem;                              // UI khung có nút X/handle
    readonly List<ItemSticker> _items = new List<ItemSticker>();

    public void SetupStep()
    {
        // 1) RT & material
        //_stickerRT = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGB32) { useMipMap = false, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, anisoLevel = 1, sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear };
        _stampMat = new Material(_stampShader);

        // Clear RT
        Graphics.Blit(_defaultTextureBlack, _stickerRT);

        // Gán vào material ốp
        _caseMat.SetTexture("_StickerRT", _stickerRT);

        // 2) Nạp list sticker (ví dụ từ Resources)
        LoadListAttachment();
    }

    void LoadListAttachment()
    {
        // ví dụ: load sprite từ Resources/ prefabs/gameplay/stickers
        var sprites = Resources.LoadAll<Sprite>("prefabs/gameplay/stickers");
        foreach (var sp in sprites)
        {
            var p = Instantiate(_tempItemPreview, _listAttachmentPreview.content);
            //p.Setup(sp, () => OnSelectItem(sp));
        }
    }

    void OnSelectItem(Sprite sp)
    {
        // tạo UI điều khiển sticker (khung có nút X/handle)
        var go = new GameObject("StickerUI", typeof(RectTransform), typeof(ItemSticker));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_stepUI.transform, false);
        rect.sizeDelta = sp.rect.size;
        var item = go.GetComponent<ItemSticker>();
        item.Init(sp, OnRemoveSticker);
        _items.Add(item);
        _currentItem = item;
    }

    void OnRemoveSticker(ItemSticker it)
    {
        _items.Remove(it);
        Destroy(it.gameObject);
        // có thể redraw All từ lịch sử nếu bạn muốn “gỡ” khỏi RT
        RedrawAll();
    }

    void Update()
    {
        if (_currentItem == null) return;

        // theo chuột – raycast vào ốp để lấy UV
        if (RaycastUV(Input.mousePosition, out var uv, out var hit))
        {
            // dịch UI preview tới vị trí tương ứng (project world->screen)
            var screen = _mainCam.WorldToScreenPoint(hit.point + hit.normal * 0.001f);
            _currentItem.MoveToScreen(screen, _uiCam);

            // Nhấn chuột trái để "dán" xuống RT (hoặc khi bấm nút ✓)
            if (Input.GetMouseButtonUp(0) && !_currentItem.IsOnHandle)   // tránh khi đang kéo handle
            {
                StampToRT(_currentItem, uv);
                _currentItem.ShowAsBakedOutline(); // hiệu ứng đã dán (tùy bạn)
                _currentItem = null;
            }
        }
    }

    bool RaycastUV(Vector2 screenPos, out Vector2 uv, out RaycastHit hit)
    {
        uv = default;
        var ray = _mainCam.ScreenPointToRay(screenPos);
        if (_caseCollider.Raycast(ray, out hit, 100f))
        {
            uv = hit.textureCoord; return true;
        }
        return false;
    }

    void StampToRT(ItemSticker it, Vector2 uvCenter)
    {
        // chuẩn bị tham số cho shader stamp
        _stampMat.SetTexture("_MainTex", _stickerRT);
        _stampMat.SetTexture("_Stamp", it.Sprite.texture);
        _stampMat.SetVector("_Center", new Vector4(uvCenter.x, uvCenter.y, 0, 0));

        // scale theo UV: giữ tỉ lệ sprite
        float aspect = it.Sprite.rect.height / it.Sprite.rect.width;
        float sizeU = it.SizeUV;                   // 0..1 do ItemSticker trả về
        _stampMat.SetVector("_Scale", new Vector4(sizeU, sizeU * aspect, 0, 0));
        _stampMat.SetFloat("_Rot", it.RotationRad);
        _stampMat.SetColor("_Tint", Color.white);

        // vẽ: src=_stickerRT -> dst=_stickerRT với shader compositing
        RenderTexture temp = RenderTexture.GetTemporary(_stickerRT.width, _stickerRT.height, 0, _stickerRT.format);
        Graphics.Blit(_stickerRT, temp, _stampMat);
        Graphics.Blit(temp, _stickerRT);
        RenderTexture.ReleaseTemporary(temp);
    }

    // Khi xóa 1 sticker đã bake, ta cần render lại tất cả từ đầu dựa trên lịch sử (nếu bạn lưu).
    void RedrawAll()
    {
        Graphics.Blit(_defaultTextureBlack, _stickerRT);
        foreach (var it in _items)
        {
            // nếu muốn giữ những cái “đã dán”, gọi StampToRT(it, it.LastUV);
        }
    }

    // --- IStep tối thiểu ---
    public Sprite Icon => null;
    public void CompleteStep() { gameObject.SetActive(false); }
    public void DiscardStep() { /* cleanup */ }
}
