using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static  class Rt
{
    public static RenderTexture MakeColorRT(int w, int h, string name = "RT")
    {
        bool isLinear = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        GraphicsFormat fmt =
            isLinear
                ? (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_SRGB, FormatUsage.Sample)
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.B8G8R8A8_SRGB)
                : (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.Sample)
                    ? GraphicsFormat.R8G8B8A8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm);

        var desc = new RenderTextureDescriptor(w, h)
        {
            graphicsFormat   = fmt,
            depthBufferBits  = 0,
            msaaSamples      = 1,
            useMipMap        = false,
            autoGenerateMips = false,
            dimension        = UnityEngine.Rendering.TextureDimension.Tex2D
        };

        var rt = new RenderTexture(desc)
        {
            name       = name,
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        rt.Create();
        return rt;
    }

    /// <summary>Fill RT để đảm bảo không rỗng (tránh nhìn “đen”).</summary>
    public static void Clear(RenderTexture rt, Color c)
    {
        if (!rt) return;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, c);
        RenderTexture.active = prev;
    }
}
