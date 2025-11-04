using UnityEngine;

public class PaintBlurRT : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public RenderTexture paintRT;        // _PaintTex bạn đang dùng trong shader case
    public Texture2D brushTex;           // PNG tròn mềm (alpha)
    public Material matPaint;            // dùng shader DIY/RT_ApplyBrush_BIRP
    public Material matBlur;             // dùng shader DIY/RT_MaskedSeparableBlur_BIRP

    [Header("Brush")]
    public Color brushColor = Color.green;
    [Range(0.005f,0.2f)] public float brushRadiusUV = 0.06f;
    [Range(0,1)] public float paintOpacity = 1f;

    [Header("Blur")]
    [Range(0,1)] public float blurStrength = 1f;
    [Range(0.5f,3f)] public float blurRadius = 1.5f;
    [Range(1,4)] public int iterations = 2;

    RenderTexture _tmp;

    void Start()
    {
        if (!cam) cam = Camera.main;
        _tmp = new RenderTexture(paintRT.descriptor);
        _tmp.filterMode = FilterMode.Bilinear;
        paintRT.wrapMode = TextureWrapMode.Clamp;
    }

    void OnDestroy(){ if (_tmp) _tmp.Release(); }

    void Update()
    {
        bool painting = Input.GetMouseButton(0);
        if (!painting) return;

        if (!RaycastUV(out var uv)) return;

        // ----- PASS 1: PAINT -----
        matPaint.SetTexture("_BrushTex", brushTex);
        matPaint.SetVector("_BrushColor", (Vector4)brushColor);
        matPaint.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0, 0));
        matPaint.SetFloat("_BrushRadiusUV", brushRadiusUV);
        matPaint.SetFloat("_PaintOpacity", paintOpacity);
        matPaint.SetFloat("_DoPaint", 1f);

        Graphics.Blit(paintRT, _tmp, matPaint);   // read paintRT -> write tmp
        Swap(ref paintRT, ref _tmp);              // kết quả mới nằm lại ở paintRT

        // ----- PASS 2: BLUR (separable H/V, chỉ trong vùng cọ) -----
        matBlur.SetTexture("_BrushTex", brushTex);
        matBlur.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0, 0));
        matBlur.SetFloat("_BrushRadiusUV", brushRadiusUV);
        matBlur.SetFloat("_Strength", blurStrength);
        matBlur.SetFloat("_Radius", blurRadius);

        for (int i = 0; i < iterations; i++)
        {
            // Horizontal
            matBlur.SetVector("_Direction", new Vector4(1,0,0,0));
            Graphics.Blit(paintRT, _tmp, matBlur);
            Swap(ref paintRT, ref _tmp);

            // Vertical
            matBlur.SetVector("_Direction", new Vector4(0,1,0,0));
            Graphics.Blit(paintRT, _tmp, matBlur);
            Swap(ref paintRT, ref _tmp);
        }

        // paintRT luôn giữ kết quả cuối cùng để shader case đọc ở _PaintTex
    }

    void Swap(ref RenderTexture a, ref RenderTexture b)
    { var t = a; a = b; b = t; }

    bool RaycastUV(out Vector2 uv)
    {
        uv = Vector2.zero;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f))
        { uv = hit.textureCoord; return true; }
        return false;
    }
}
