using UnityEngine;

public static class ScreenHeight10
{
    /// 10% chiều cao màn hình (pixel)
    public static float H10px => Screen.height * 0.1f;

    /// Lấy % chiều cao màn hình (0..1) theo pixel
    public static float Px(float percent01) => Screen.height * Mathf.Clamp01(percent01);

    /// World units cho camera Ortho (mặc định 10%)
    public static float WorldOrtho(Camera cam, float percent01 = 0.1f)
    {
        cam = cam ? cam : Camera.main;
        return (cam.orthographicSize * 2f) * Mathf.Clamp01(percent01);
    }

    /// World units cho camera Perspective tại khoảng cách zDist (mặc định 10%)
    public static float WorldPerspective(Camera cam, float zDist, float percent01 = 0.1f)
    {
        cam = cam ? cam : Camera.main;
        var p0 = cam.ScreenToWorldPoint(new Vector3(0f, 0f, zDist));
        var p1 = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height * Mathf.Clamp01(percent01), zDist));
        return Mathf.Abs(p1.y - p0.y);
    }

    /// Đặt UI theo anchor: cao = percent màn hình, full-width, dính đáy
    public static void SetRectAnchors(RectTransform rt, float heightPercent01)
    {
        if (!rt) return;
        heightPercent01 = Mathf.Clamp01(heightPercent01);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, heightPercent01);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// Đặt UI theo pixel tuyệt đối: cao = percent * Screen.height
    public static void SetRectPixelHeight(RectTransform rt, float heightPercent01)
    {
        if (!rt) return;
        float h = Px(heightPercent01);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }
}

[ExecuteAlways]
public class ScreenHeight10Applier : MonoBehaviour
{
    [Header("Percent (0..1)")]
    [Range(0f, 1f)] public float percent = 0.1f;

    [Header("Apply to UI (optional)")]
    public RectTransform targetRect;
    public bool useAnchors = true;       // true: anchor 0..percent ; false: pixel height

    [Header("World measure (optional)")]
    public Camera worldCamera;
    public float zDist = 1f;             // khoảng cách từ cam tới điểm cần đo (Perspective)

    [Header("Debug (read-only)")]
    public float hPx;                     // 10% theo pixel
    public float hWorld;                  // 10% theo world units (nếu có camera)

    int _lastW, _lastH;

    void OnEnable() { RecalcAndApply(); }

    void Update()
    {
        if (Screen.width != _lastW || Screen.height != _lastH)
            RecalcAndApply();
    }

    public void RecalcAndApply()
    {
        _lastW = Screen.width; _lastH = Screen.height;

        float p = Mathf.Clamp01(percent);
        hPx = ScreenHeight10.Px(p);

        if (worldCamera)
        {
            if (worldCamera.orthographic)
                hWorld = ScreenHeight10.WorldOrtho(worldCamera, p);
            else
                hWorld = ScreenHeight10.WorldPerspective(worldCamera, zDist, p);
        }

        if (targetRect)
        {
            if (useAnchors) ScreenHeight10.SetRectAnchors(targetRect, p);
            else            ScreenHeight10.SetRectPixelHeight(targetRect, p);
        }
    }
}
