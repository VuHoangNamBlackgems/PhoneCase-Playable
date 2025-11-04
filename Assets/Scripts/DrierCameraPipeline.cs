using UnityEngine;

public class DrierCameraPipeline : MonoBehaviour
{
    public RenderTexture paintRT, blurRT, heatMaskRT;
    public Material mixMat;  // M_DrierMix
    RenderTexture tmp;

    void Start(){
        tmp = new RenderTexture(paintRT.descriptor);
        mixMat.SetTexture("_PaintTex", paintRT);
        mixMat.SetTexture("_BlurTex",  blurRT);
        mixMat.SetTexture("_MaskTex",  heatMaskRT);
        ClearRT(paintRT); ClearRT(heatMaskRT);   // QUAN TRỌNG: clear về (0,0,0,0)
    }
    void LateUpdate(){
        if (!Input.GetMouseButton(0)) return;    // đang sấy thì trộn
        Graphics.Blit(paintRT, tmp, mixMat);     // composite RGB theo mask, giữ alpha
        Graphics.Blit(tmp, paintRT);
        ClearRT(heatMaskRT);                     // xoá mask cho frame sau
    }
    static void ClearRT(RenderTexture rt){
        var prev = RenderTexture.active; RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear); RenderTexture.active = prev;
    }
}