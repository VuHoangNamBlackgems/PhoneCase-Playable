using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CoinFlyFX : MonoBehaviour
{
    public static CoinFlyFX Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] RectTransform canvasRoot;   // Canvas (Screen Space)
    [SerializeField] Image coinPrefab;           // Image đồng xu (PNG alpha)
    [SerializeField] int poolSize = 12;

    [Header("Anim")]
    [SerializeField] float duration = 0.6f;
    [SerializeField] Ease moveEase = Ease.InCubic;
    [SerializeField] Vector2 spawnJitter = new Vector2(20, 20);
    [SerializeField] Vector2 arcRange = new Vector2(48, 90);

    readonly Queue<Image> pool = new Queue<Image>();
    readonly List<Image> running = new List<Image>();

    void Awake()
    {
        Instance = this;
        if (!canvasRoot) canvasRoot = GetComponentInParent<Canvas>().transform as RectTransform;
        for (int i = 0; i < poolSize; i++)
        {
            var img = Instantiate(coinPrefab, canvasRoot);
            img.gameObject.SetActive(false);
            pool.Enqueue(img);
        }
    }

    Image Rent()
    {
        var img = pool.Count > 0 ? pool.Dequeue() : Instantiate(coinPrefab, canvasRoot);
        img.gameObject.SetActive(true);
        img.transform.SetAsLastSibling();
        running.Add(img);
        return img;
    }
    void Return(Image img)
    {
        running.Remove(img);
        img.gameObject.SetActive(false);
        pool.Enqueue(img);
    }

    static Vector2 CanvasPos(RectTransform from, RectTransform canvas)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, from.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local);
        return local;
    }

    /// <summary>Spawn nhiều đồng bay từ <paramref name="from"/> → <paramref name="to"/>.
    /// Trả về thời gian bay lâu nhất để timing việc trừ tiền.</summary>
    public float Play(RectTransform from, RectTransform to, int amount)
    {
        if (!from || !to || amount <= 0) return 0f;

        int count = Mathf.Clamp(Mathf.CeilToInt(amount / 20f), 3, 10);
        Vector2 start = CanvasPos(from, canvasRoot);
        Vector2 end   = CanvasPos(to,   canvasRoot);

        float maxT = 0f;

        for (int i = 0; i < count; i++)
        {
            var img = Rent();
            var rt  = (RectTransform)img.transform;

            rt.anchoredPosition = start + new Vector2(Random.Range(-spawnJitter.x, spawnJitter.x),
                                                      Random.Range(-spawnJitter.y, spawnJitter.y));
            rt.localScale = Vector3.one * Random.Range(0.85f, 1.1f);
            img.color = Color.white;

            Vector2 dir = (end - start).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            float arc = Random.Range(arcRange.x, arcRange.y);
            Vector2 mid = Vector2.Lerp(start, end, Random.Range(0.35f, 0.55f)) + normal * arc;

            float t = duration * Random.Range(0.9f, 1.1f);
            maxT = Mathf.Max(maxT, t);

            DOVirtual.Float(0f, 1f, t, v =>
            {
                Vector2 p1 = Vector2.Lerp(start, mid, v);
                Vector2 p2 = Vector2.Lerp(mid, end, v);
                rt.anchoredPosition = Vector2.Lerp(p1, p2, v);
            }).SetEase(moveEase).OnComplete(() => Return(img));

            img.DOFade(0f, t).SetEase(Ease.InQuad);
            rt.DOScale(0.65f, t);
        }
        return maxT;
    }
}
