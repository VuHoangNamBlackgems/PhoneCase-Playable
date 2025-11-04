using UnityEngine;

public class ScrewTarget : MonoBehaviour
{
    [Header("Refs")]
    public Transform screwMesh;    // object để move/rotate
    public Transform anchor;       // tâm/lỗ ốc (pose cuối)
    public Transform normalRef;    // optional: trục normal (forward = hướng ra ngoài)
    public AudioSource audioSrc;
    public AudioClip unscrewClip;
    public AudioClip screwInClip;
    public Vector3 localPos;
    [Header("Timings")]
    public float duration = 0.6f;  // thời gian animate vào/ra
    public int   turns = 6;        // số vòng quay
    public float pullUp = 0.02f;   // quãng đẩy/nhấc dọc theo normal

    [Header("State")]
    public bool removed;           // true = đang ở ngoài

    Vector3 _homeLocalPos;
    Quaternion _homeLocalRot;
    bool _cached;

    void Awake()
    {
        if (screwMesh && !_cached)
        {
            _homeLocalPos = screwMesh.localPosition;
            _homeLocalRot = screwMesh.localRotation;
            _cached = true;
        }
    }

    public Vector3 GetLocalPos()
    {
        return localPos;
    }
    
    public Vector3 Outward()
    {
        // Hướng normal ra ngoài bề mặt
        if (normalRef) return normalRef.forward;
        return anchor ? anchor.forward : transform.forward;
    }

    public void PrepareForInsertion(float offset)  // đặt ốc “gần lỗ”
    {
        removed = true;
        var col = GetComponentInChildren<Collider>(true);
        if (col) col.enabled = true;

        if (!screwMesh || !anchor) return;

        Vector3 outward = Outward();
        screwMesh.position = anchor.position;
        screwMesh.gameObject.SetActive(true);
    }

    public void MarkRemoved()
    {
        removed = true;
        var col = GetComponentInChildren<Collider>(true);
        if (col) col.enabled = true;
        gameObject.SetActive(false);
    }

    public void MarkInserted()
    {
        removed = false;

        if (screwMesh && anchor)
        {
            screwMesh.position = anchor.position;
            screwMesh.rotation = anchor.rotation;
        }

        var col = GetComponentInChildren<Collider>(true);
        if (col) col.enabled = false;
    }
    
    
}
