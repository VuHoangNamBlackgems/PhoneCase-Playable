using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickParticle : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform fxRoot; 
    [SerializeField] private Camera uiCamera;      
    [SerializeField] private FxPool pool;          

    [Header("Timing")]
    [SerializeField] private float life = 2f;      

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) Spawn(Input.mousePosition);
#endif
        foreach (var t in Input.touches)
            if (t.phase == TouchPhase.Began) Spawn(t.position);
    }

    void Spawn(Vector2 screenPos)
    {
        bool overlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fxRoot, screenPos, overlay ? null : uiCamera, out var local)) return;

        var go = pool.Get(fxRoot);
        if (go.transform is RectTransform rt) rt.anchoredPosition = local; else go.transform.position = screenPos;

        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps) ps.Play(true);

        StopCoroutineSafely(go); // tránh double routine nếu tái dùng ngay lập tức
        StartCoroutine(ReturnAfter(go, life));
    }

    readonly Dictionary<GameObject, Coroutine> running = new Dictionary<GameObject, Coroutine>();
    void StopCoroutineSafely(GameObject go)
    {
        if (running.TryGetValue(go, out var co) && co != null) StopCoroutine(co);
        running.Remove(go);
    }

    IEnumerator ReturnAfter(GameObject go, float t)
    {
        var co = StartCoroutine(Delay(go, t));
        running[go] = co;
        yield return co;
        running.Remove(go);
    }
    IEnumerator Delay(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        pool.Release(go);
    }
}
