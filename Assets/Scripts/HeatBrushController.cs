using UnityEngine;

public class HeatBrushController : MonoBehaviour
{
    public Camera mainCam;          // Main Camera
    public Camera maskCam;          // MaskCam (Ortho → HeatMaskRT)
    public Collider caseCollider;   // MeshCollider case
    public ParticleSystem brushPS;  // PS layer MaskFX
    public RenderTexture heatMaskRT;

    [Range(0.04f,0.16f)] public float brushSizeUV = 0.10f; // đường kính theo UV (0..1)
    [Range(0.18f,0.40f)] public float spacing = 0.26f;     // khoảng cách ≈ spacing*đường kính
    public float zOffset = 0.001f;

    ParticleSystem.MainModule mainM; ParticleSystem.EmissionModule emiM;

    void Start(){
        if (!mainCam) mainCam = Camera.main;
        mainM = brushPS.main; emiM = brushPS.emission;
        // clear mask lúc bắt đầu
        ClearRT(heatMaskRT);
    }

    void Update(){
        if (caseCollider.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out var hit, 200f))
        {
            float worldH = maskCam.orthographicSize * 2f;
            float brushWorld = worldH * brushSizeUV;
            mainM.startSize = brushWorld;

            float step = Mathf.Max(1e-4f, brushWorld * spacing);
            emiM.rateOverDistance = 1f / step;

            brushPS.transform.position = hit.point + hit.normal * zOffset;

            if (Input.GetMouseButtonDown(0)) brushPS.Clear();
            brushPS.Play(Input.GetMouseButton(0));
        }
        else brushPS.Stop();
    }

    [ContextMenu("Clear HeatMask")]
    public void ClearMask(){ ClearRT(heatMaskRT); }

    void ClearRT(RenderTexture rt){
        var prev = RenderTexture.active; RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear); RenderTexture.active = prev;
    }
}