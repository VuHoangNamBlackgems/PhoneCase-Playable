using UnityEngine;

public class MaskedBlurPainter : MonoBehaviour
{
    public Camera cam;
    [SerializeField] RenderTexture paintRT;     // RT bạn đang vẽ màu
    [SerializeField] Texture2D brushTex;        // PNG tròn mềm (tùy chọn)
    
    public Shader blurShader;
    

    [Range(0.005f, 0.25f)] [SerializeField] float brushRadiusUV = 0.06f;   // bán kính cọ theo UV
    [Range(0f, 1f)]        [SerializeField] float strength = 1f;           // độ mờ apply
    [Range(1, 4)]          [SerializeField] int iterations = 2;            // lặp blur
    [Range(1f, 3f)]        [SerializeField] float blurRadius = 1.5f;       // bước lấy mẫu

    Material _mat;
    RenderTexture _tempRT;

    void Awake()
    {
        
        if (!cam) cam = Camera.main;
        _mat = new Material(blurShader);
        _tempRT = new RenderTexture(paintRT.descriptor);
        _tempRT.filterMode = FilterMode.Bilinear;
        paintRT.wrapMode = TextureWrapMode.Clamp;
    }

    void OnDestroy()
    {
        if (_tempRT) _tempRT.Release();
        if (_mat) Destroy(_mat);
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (TryGetUVUnderMouse(out Vector2 uv))
            {
                BlurAtUV(uv);
            }
        }
    }

    bool TryGetUVUnderMouse(out Vector2 uv)
    {
        uv = Vector2.zero;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f))
        {
            // cần MeshCollider/Collider có UV
            uv = hit.textureCoord; // UV [0..1]
            return true;
        }
        return false;
    }

    public void BlurAtUV(Vector2 uv)
    {
        if (_mat == null || paintRT == null) return;

        _mat.SetTexture("_BrushTex", brushTex);
        _mat.SetFloat("_BlurRadius", blurRadius);

        // _Params = (u, v, radiusUV, strength)
        _mat.SetVector("_Params", new Vector4(uv.x, uv.y, brushRadiusUV, strength));

        for (int i = 0; i < iterations; i++)
        {
            // Horizontal
            _mat.SetVector("_Direction", new Vector2(1, 0));
            Graphics.Blit(paintRT, _tempRT, _mat);

            // Vertical
            _mat.SetVector("_Direction", new Vector2(0, 1));
            Graphics.Blit(_tempRT, paintRT, _mat);
        }
    }
}
