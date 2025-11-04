using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CopyOpaqueToRT : MonoBehaviour
{
    public string globalTexName = "_OpaqueSceneTex";
    public FilterMode filterMode = FilterMode.Bilinear;

    Camera cam;
    CommandBuffer cb;
    int opaqueTexID;
    bool added;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        opaqueTexID = Shader.PropertyToID(globalTexName);
     //   Build();
    }

    void OnDisable()
    {
        Teardown();
    }

    void Build()
    {
       /* Teardown();

        cb = new CommandBuffer { name = "Copy Opaque To RT" };
        // Allocate RT matching camera
        cb.GetTemporaryRT(opaqueTexID, -1, -1, 0, filterMode);
        // Copy color buffer sau khi vẽ xong Opaque
        cb.Blit(BuiltinRenderTextureType.CameraTarget, opaqueTexID);
        // Đặt global để các material Transparent sau đó sample
        cb.SetGlobalTexture(globalTexName, opaqueTexID);

        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cb);
        added = true;*/
    }

    void Teardown()
    {
        if (cam != null && cb != null && added)
        {
            cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, cb);
            added = false;
        }
        if (cb != null)
        {
            cb.Release();
            cb = null;
        }
    }
}