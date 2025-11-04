using UnityEngine;

[System.Serializable]
public class BlurBrushStamp
{
    [Header("Shader (DIY/BlurBrushStamp_BIRP)")]
    [SerializeField] Shader shader;
    Material _mat;
    RenderTexture _tmp;

    [Header("Blur")]
    [Range(0f, 1f)] public float defaultStrength = 0.85f;

    [Header("Bleed / Loang")]
    [Tooltip("Tỉ lệ px loang so với bán kính blur (0.2 = 20%).")]
    [Range(0f, 2.0f)] public float bleedScale = 0.20f;

    [Tooltip("Cộng thêm px loang tuyệt đối.")]
    [Range(0f, 256f)] public float bleedExtraPx = 0f;

    [Tooltip("Cường độ loang alpha.")]
    [Range(0f, 1f)] public float bleedStrength = 0.6f;

    [Tooltip("Mềm mép vùng tác động (UV).")]
    [Range(0.0005f, 0.05f)] public float edgeSoftUV = 0.01f;

    [Header("Bleed Color (RGB)")]
    [Tooltip("Bật = màu cũng loang rộng như alpha (như ảnh #2).")]
    public bool bleedColorWide = true;

    [Range(0f, 2f)]
    public float colorBleedScale = 1.0f;

    [Header("Bleed Extent (loang xa)")]
    [Tooltip("Mở rộng phạm vi loang so với bán kính blur (1 = bằng, 3 = gấp 3).")]
    [Range(0.5f, 6f)] public float bleedExtentScale = 2.5f;

    void InitIfNeeded()
    {
        if (_mat) return;
        if (!shader) shader = Shader.Find("DIY/BlurBrushStamp_BIRP");
        if (!shader) { Debug.LogError("BlurBrushStamp: shader DIY/BlurBrushStamp_BIRP not found."); return; }
        _mat = new Material(shader);
    }
    
    public BlurBrushStamp(Shader shader = null)
    {
        if (!shader) shader = Shader.Find("DIY/BlurBrushStamp_BIRP");
        if (!shader) { Debug.LogError("BlurBrushStamp: missing shader"); return; }
        _mat = new Material(shader);
    }

    /// <summary>
    /// Đóng dấu blur + bleed tại uv01 (0..1).
    /// </summary>
    public void Stamp(RenderTexture paintRT, Vector2 uv01, float blurRadiusPx, float strength)
    {
      /*  if (!paintRT) return;
        InitIfNeeded(); if (!_mat) return;

        float w = paintRT.width, h = paintRT.height;
        float maxDim = Mathf.Max(1f, Mathf.Max(w, h));
        float radiusUV = blurRadiusPx / maxDim;

        // Bước và phạm vi loang (px)
        float bleedStepPx   = Mathf.Max(1f, blurRadiusPx * Mathf.Max(0f, bleedScale) + bleedExtraPx);
        float bleedExtentPx = Mathf.Max(bleedStepPx, blurRadiusPx * Mathf.Max(0.5f, bleedExtentScale));

        _mat.SetVector("_CenterUV", uv01);
        _mat.SetFloat("_RadiusUV", radiusUV);
        _mat.SetFloat("_Strength", Mathf.Clamp01(strength));

        _mat.SetFloat("_BleedStepPx", bleedStepPx);
        _mat.SetFloat("_BleedExtentPx", bleedExtentPx);
        _mat.SetFloat("_BleedStrength", Mathf.Clamp01(bleedStrength));
        _mat.SetFloat("_EdgeSoft", edgeSoftUV);

        _mat.SetFloat("_BleedColorWide", bleedColorWide ? 1f : 0f);
        _mat.SetFloat("_ColorBleedScale", colorBleedScale);

        _tmp = RenderTexture.GetTemporary(paintRT.descriptor);
        Graphics.Blit(paintRT, _tmp, _mat, 0);
        Graphics.Blit(_tmp, paintRT);
        RenderTexture.ReleaseTemporary(_tmp);*/
    }
}
