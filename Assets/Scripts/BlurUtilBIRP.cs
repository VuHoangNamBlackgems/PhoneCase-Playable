using UnityEngine;

public static class BlurUtilBIRP
{
    // blurMat: material dùng shader "DIY/SeparableBlur_BIRP"
    public static void BuildBlur(RenderTexture paintRT, ref RenderTexture blurRT, Material blurMat, float radius = 1f, int iterations = 1)
    {
        if (!paintRT || !blurMat) return;

        // Tạo blurRT cùng cấu hình
        if (blurRT == null || blurRT.width != paintRT.width || blurRT.antiAliasing != paintRT.antiAliasing)
        {
            if (blurRT) blurRT.Release();
            blurRT = new RenderTexture(paintRT.width, paintRT.height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
                antiAliasing = paintRT.antiAliasing
            };
            blurRT.Create();
        }

        var tmp = RenderTexture.GetTemporary(paintRT.width, paintRT.height, 0, RenderTextureFormat.ARGB32);
        blurMat.SetFloat("_Radius", radius);

        // có thể lặp nhiều lần cho mịn hơn
        Graphics.Blit(paintRT, tmp,    blurMat, 0); // pass 0: ngang
        Graphics.Blit(tmp,    blurRT,  blurMat, 1); // pass 1: dọc
        for (int i = 1; i < iterations; i++)
        {
            Graphics.Blit(blurRT, tmp,   blurMat, 0);
            Graphics.Blit(tmp,   blurRT, blurMat, 1);
        }

        RenderTexture.ReleaseTemporary(tmp);
    }
}
