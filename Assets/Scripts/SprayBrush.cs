using UnityEngine;

[DisallowMultipleComponent]
public class SprayBrush : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] ParticleSystem ps;
    [SerializeField] bool hideRendererWhenIdle = true;

    Renderer[] _renderers;

    void Awake()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
        StopSpray();
    }

    /// Di chuyển brush (con) tới vị trí world.
    public void MoveTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    public void OnDragStart() { SetVisible(true); }
    public void OnDragEnd()   { StopSpray(); SetVisible(false); }

    public void SetSpraying(bool on)
    {
        if (!ps) return;
        var em = ps.emission;
        em.enabled = on;
        if (on) { if (!ps.isEmitting) ps.Play(); }
        else    { if (ps.isEmitting) ps.Stop();  }
    }

    public void StopSpray()
    {
        if (!ps) return;
        var em = ps.emission; em.enabled = false;
        if (ps.isEmitting) ps.Stop();
    }

    public void SetVisible(bool visible)
    {
        if (!hideRendererWhenIdle || _renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i]) _renderers[i].enabled = visible;
    }

    // Tuỳ chọn
    public void SetColor(Color c) { if (ps) { var m = ps.main; m.startColor = c; } }
    public void SetRate(float r)  { if (ps) { var em = ps.emission; var v = em.rateOverTime; v.constant = r; em.rateOverTime = v; } }
}