using UnityEngine;

/// <summary>
/// Khi giữ chuột/touch: raycast UV trên case -> đổi UV sang world của HeatCam
/// -> emit 1 hạt tại vị trí đó. HeatCam render hạt (màu trắng mềm) vào HeatMaskRT.
/// </summary>
public class HeatMaskPainter : MonoBehaviour
{
    [Header("Refs")]
    public Camera mainCam;            // camera gameplay để raycast
    public Camera heatCam;            // orthographic, Don’t Clear, Culling=TransparentFX,
                                      // TargetTexture = heatMaskRT
    public Collider caseCollider;     // MeshCollider của case (UV đúng)
    public ParticleSystem heatStampPS;// PS ở layer TransparentFX, material soft circle

    [Header("RenderTextures")]
    public RenderTexture heatMaskRT;  // gán sẵn, ColorFormat=ARGB32
    public KeyCode clearKey = KeyCode.C;

    [Header("Brush")]
    [Tooltip("Kích thước cọ chiếm tỉ lệ chiều cao khung nhìn của HeatCam")]
    [Range(0.01f, 0.5f)] public float brushSizePercentOfHeight = 0.10f;
    [Tooltip("Khoảng cách giữa 2 dấu cọ = spacing * brushSize (world)")]
    [Range(0.05f, 1.0f)] public float spacing = 0.35f;

    Vector3 _lastPos;
    bool _hasLast;
    float _worldBrushSize;

   /* void Awake()
    {
        if (!mainCam) mainCam = Camera.main;
        if (heatCam) heatCam.targetTexture = heatMaskRT;
        ClearMask();
    }

    void Update()
    {
        if (Input.GetKeyDown(clearKey)) ClearMask();

        // size cọ trong world theo chiều cao ortho của HeatCam
        float camHeight = heatCam.orthographicSize * 2f;
        _worldBrushSize = camHeight * brushSizePercentOfHeight;

        if (!Input.GetMouseButton(0) || caseCollider == null || heatStampPS == null) {
            _hasLast = false; return;
        }

        // Raycast UV trên case
        if (caseCollider.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out var hit, 100f))
        {
            Vector3 world = UVToHeatWorld(hit.textureCoord);

            // Emit theo spacing để mượt
            if (!_hasLast || Vector2.Distance(world, _lastPos) >= _worldBrushSize * spacing)
            {
                EmitAt(world, _worldBrushSize);
                _lastPos = world;
                _hasLast = true;
            }
        }
        else _hasLast = false;
    }

    // Map UV (0..1) -> world trong mặt phẳng nhìn của HeatCam
    Vector3 UVToHeatWorld(Vector2 uv)
    {
        float h = heatCam.orthographicSize * 2f;
        float w = h * heatCam.aspect;
        Vector3 c = heatCam.transform.position;
        return new Vector3(
            c.x + (uv.x - 0.5f) * w,
            c.y + (uv.y - 0.5f) * h,
            c.z    // particle billboard theo view của HeatCam
        );
    }

    void EmitAt(Vector3 pos, float worldSize)
    {
        var ep = new ParticleSystem.EmitParams();
        ep.position  = pos;
        ep.startSize = worldSize;   // size hạt = kích thước cọ
        ep.startColor = Color.white;
        heatStampPS.Emit(ep, 1);
    }

    public void ClearMask()
    {
        if (!heatMaskRT) return;
        var prev = RenderTexture.active;
        RenderTexture.active = heatMaskRT;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = prev;
        _hasLast = false;
    }*/
}
