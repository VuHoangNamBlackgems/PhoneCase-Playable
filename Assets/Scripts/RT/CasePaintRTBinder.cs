using UnityEngine;

/// <summary>
/// Tạo và gán RenderTexture cho shader DIY/CasePaintOnly_*
/// - Gán _PaintTex và _StickerRT (không dùng mask).
/// - Expose getter để hệ vẽ/Camera/Blit khác có thể ghi vào RT.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class CasePaintRTBinder : MonoBehaviour
{
    [Header("RT Size")]
    [SerializeField] private int width  = 512;
    [SerializeField] private int height = 512;

    [Header("Shader Properties")]
    [SerializeField] private string baseProp    = "_BaseMap";
    [SerializeField] private string paintProp   = "_PaintTex";
    [SerializeField] private string stickerProp = "_StickerRT";

    [Header("Auto Clear On Start (for sanity)")]
    [SerializeField] private bool clearOnStart       = true;
    [SerializeField] private Color paintClearColor   = new Color(0,0,0,0); // transparent
    [SerializeField] private Color stickerClearColor = new Color(0,0,0,0);

    private Renderer _renderer;
    private Material _matInstance;

    private RenderTexture _paintRT;
    private RenderTexture _stickerRT;

    public RenderTexture GetPaintRT()   => _paintRT;
    public RenderTexture GetStickerRT() => _stickerRT;
    public Material      GetMaterial()  => _matInstance;

    private void Awake()
    {
        _renderer    = GetComponent<Renderer>();
        _matInstance = _renderer.material; // instance riêng

        // Tạo RT theo color space hiện tại (Gamma/Linear)
        _paintRT   = Rt.MakeColorRT(width, height,   "PaintRT");
        _stickerRT = Rt.MakeColorRT(width, height, "StickerRT");

        if (clearOnStart)
        {
            Rt.Clear(_paintRT,   paintClearColor);
            Rt.Clear(_stickerRT, stickerClearColor);
        }

        // Gán vào material
        if (!string.IsNullOrEmpty(paintProp) && _matInstance.HasProperty(paintProp))
            _matInstance.SetTexture(paintProp, _paintRT);

        if (!string.IsNullOrEmpty(stickerProp) && _matInstance.HasProperty(stickerProp))
            _matInstance.SetTexture(stickerProp, _stickerRT);

        // Đảm bảo BaseMap không null
        if (!string.IsNullOrEmpty(baseProp) && _matInstance.HasProperty(baseProp) &&
            _matInstance.GetTexture(baseProp) == null)
        {
            _matInstance.SetTexture(baseProp, Texture2D.whiteTexture);
        }

        Debug.Log($"[CasePaintRTBinder] Mat={_matInstance?.name} Paint={_paintRT} Sticker={_stickerRT} CS={QualitySettings.activeColorSpace}");
    }
 
    [ContextMenu("Clear Paint RT")]
    public void ClearPaint() => Rt.Clear(_paintRT, paintClearColor);

    [ContextMenu("Clear Sticker RT")]
    public void ClearSticker() => Rt.Clear(_stickerRT, stickerClearColor);

    private void OnDestroy()
    {
        // Ngắt reference để GC RT
        if (_matInstance != null)
        {
            if (!string.IsNullOrEmpty(paintProp)   && _matInstance.HasProperty(paintProp))   _matInstance.SetTexture(paintProp, null);
            if (!string.IsNullOrEmpty(stickerProp) && _matInstance.HasProperty(stickerProp)) _matInstance.SetTexture(stickerProp, null);
        }

        if (_paintRT != null)   { _paintRT.Release();   Destroy(_paintRT); }
        if (_stickerRT != null) { _stickerRT.Release(); Destroy(_stickerRT); }

        if (_matInstance != null) Destroy(_matInstance);
    }
}
