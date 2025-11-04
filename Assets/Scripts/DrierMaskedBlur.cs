using UnityEngine;

public class DrierMaskedBlur : MonoBehaviour
{
    public RenderTexture paintA;   // gán cái đang set cho _PaintTex (nguồn ban đầu)
    public RenderTexture paintB;   // RT phụ để ping-pong
    public RenderTexture heatMask; // nơi particle vẽ
    public Material maskedBlurMat; // material dùng shader "DIY/MaskedSeparableBlur_BIRP"

    [Range(0,6)] public int radiusPx = 3;
    [Range(0,1)] public float strength = 1f;
    [Range(1,3)] public int iterations = 1;
    public bool clearMaskEachFrame = true;

    RenderTexture src, dst;

    void Start()
    {
        src = paintA; dst = paintB;
        maskedBlurMat.SetTexture("_MaskTex", heatMask);
    }

    void Update()
    {
        if (!Input.GetMouseButton(0)) return; // đang sấy mới blur

        maskedBlurMat.SetFloat("_RadiusPx", radiusPx);
        maskedBlurMat.SetFloat("_Strength", strength);

        for (int i = 0; i < iterations; i++)
        {
            // H
            Graphics.Blit(src, dst, maskedBlurMat, 0);
            Swap();
            // V
            Graphics.Blit(src, dst, maskedBlurMat, 1);
            Swap();
        }

        // (tuỳ chọn) xoá mask để chỉ blur vùng vừa vẽ
        if (clearMaskEachFrame) ClearRT(heatMask);
    }

    void Swap(){ var t = src; src = dst; dst = t; }

    void ClearRT(RenderTexture rt){
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear); // (0,0,0,0)
        RenderTexture.active = prev;
    }
}