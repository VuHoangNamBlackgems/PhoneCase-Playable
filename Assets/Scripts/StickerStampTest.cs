using UnityEngine;

public class StickerStampTest : MonoBehaviour
{
    public Camera mainCam;
    public MeshCollider caseCollider;
    public Material caseMaterial;
    public RenderTexture stickerRT;
    public Shader stampShader;
    public Texture2D stickerTex;

    [Range(0.05f,0.8f)] public float sizeUV = 0.25f;
    public float rotDeg = 0f;

    Material _stampMat;
    float _pinchBaseDist, _pinchBaseSize;   // cho mobile

    void Awake(){
        _stampMat = new Material(stampShader);
        var old = RenderTexture.active;
        RenderTexture.active = stickerRT; GL.Clear(true,true,Color.clear); RenderTexture.active = old;
        caseMaterial.SetTexture("_StickerRT", stickerRT);
    }
    void OnDestroy(){ if(_stampMat) Destroy(_stampMat); }

    void Update(){
        // chỉnh size: desktop (scroll)
        sizeUV = Mathf.Clamp(sizeUV + Input.mouseScrollDelta.y * 0.02f, 0.05f, 0.8f);
        // mobile (pinch)
        if (Input.touchCount == 2){
            var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
            float cur = (t0.position - t1.position).magnitude;
            if (_pinchBaseDist==0){ _pinchBaseDist = cur; _pinchBaseSize = sizeUV; }
            else sizeUV = Mathf.Clamp(_pinchBaseSize * (cur/Mathf.Max(1f,_pinchBaseDist)), 0.05f, 0.8f);
        } else _pinchBaseDist = 0;

        if (Input.GetKey(KeyCode.Q)) rotDeg += 90*Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) rotDeg -= 90*Time.deltaTime;

        // Click trái dán sticker
        if (Input.GetMouseButtonDown(0) && RaycastUV(Input.mousePosition, out var uv)) Stamp(uv);

        // Click phải để clear toàn bộ
        if (Input.GetMouseButtonDown(1)){
            var old = RenderTexture.active;
            RenderTexture.active = stickerRT; GL.Clear(true,true,Color.clear); RenderTexture.active = old;
        }
    }

    bool RaycastUV(Vector2 screen, out Vector2 uv){
        uv = default; var ray = mainCam.ScreenPointToRay(screen);
        if (caseCollider.Raycast(ray, out var hit, 100f)){ uv = hit.textureCoord; return true; }
        return false;
    }

    void Stamp(Vector2 uvCenter){
        float aspect = (float)stickerTex.height / stickerTex.width;
        _stampMat.SetTexture("_MainTex", stickerRT);
        _stampMat.SetTexture("_Stamp", stickerTex);
        _stampMat.SetVector("_Center", new Vector4(uvCenter.x, uvCenter.y, 0, 0));
        _stampMat.SetVector("_Scale",  new Vector4(sizeUV, sizeUV*aspect, 0, 0));
        _stampMat.SetFloat("_Rot", rotDeg*Mathf.Deg2Rad);
        _stampMat.SetColor("_Tint", Color.white);

        var tmp = RenderTexture.GetTemporary(stickerRT.width, stickerRT.height, 0, stickerRT.format);
        Graphics.Blit(stickerRT, tmp, _stampMat);
        Graphics.Blit(tmp, stickerRT);
        RenderTexture.ReleaseTemporary(tmp);
    }
}
