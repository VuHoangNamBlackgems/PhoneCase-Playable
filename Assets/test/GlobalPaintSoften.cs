using UnityEngine;

public class GlobalPaintSoften
{
    readonly Material _mat;
    RenderTexture _tmp;

    public GlobalPaintSoften(Shader sepBlurShader = null)
    {
        if (sepBlurShader == null) sepBlurShader = Shader.Find("DIY/SeparableBlur_BIRP");
        _mat = new Material(sepBlurShader);
    }

    /// <summary>
    /// Làm mềm toàn RT theo 2 trục; verticalBias > 1 -> mờ dọc nhiều hơn (giống ảnh #2).
    /// </summary>
    public void Apply(RenderTexture rt, int iterations = 3, float radiusPx = 24f, float verticalBias = 2.8f)
    {
        if (!rt || !_mat) return;
        var desc = rt.descriptor;
        _tmp = RenderTexture.GetTemporary(desc);

        for (int i = 0; i < iterations; i++)
        {
            // pass ngang
            _mat.SetVector("_TexelStep", new Vector2(1, 0));
            _mat.SetFloat("_RadiusPx", radiusPx);
            Graphics.Blit(rt, _tmp, _mat, 0);

            // pass dọc (có bias)
            _mat.SetVector("_TexelStep", new Vector2(0, 1));
            _mat.SetFloat("_RadiusPx", radiusPx * verticalBias);
            Graphics.Blit(_tmp, rt, _mat, 0);

            radiusPx *= 1.2f; // mỗi vòng tăng nhẹ để lan xa hơn
        }
        RenderTexture.ReleaseTemporary(_tmp);
    }
}