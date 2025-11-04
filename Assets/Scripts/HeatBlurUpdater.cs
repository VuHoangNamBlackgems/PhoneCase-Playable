using UnityEngine;

public class HeatBlurUpdater : MonoBehaviour
{
    public RenderTexture paintRT;   // nguồn màu
    public RenderTexture blurRT;    // đích blur
    public Material blurMat;        // M_Blur

    [Range(0,6)] public int radiusPx = 3;
    [Range(0,1)] public float strength = 1.0f;
    [Range(1,3)] public int iterations = 2;

    void LateUpdate()
    {
        // chỉ blur khi đang sấy (chuột trái) – tùy bạn bật điều kiện
        if (!Input.GetMouseButton(0)) return;

        blurMat.SetFloat("_RadiusPx", radiusPx);
        blurMat.SetFloat("_Strength", strength);

        var tmp = RenderTexture.GetTemporary(blurRT.descriptor);

        // Blur mạnh: lặp 2 lần
        Graphics.Blit(paintRT, tmp,   blurMat, 0); // H
        Graphics.Blit(tmp,    blurRT, blurMat, 1); // V

        // Vòng thứ 2 (tuỳ)
        for (int i = 1; i < iterations; i++){
            Graphics.Blit(blurRT, tmp,   blurMat, 0);
            Graphics.Blit(tmp,   blurRT, blurMat, 1);
        }
        RenderTexture.ReleaseTemporary(tmp);
    }
}