using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceExpressions : MonoBehaviour
{
    [System.Serializable] public struct Shape { public string name; [Range(0,100)] public float weight; }
    [System.Serializable] public class Expression { public string id = "Happy"; public float fade = 0.12f; public Shape[] shapes; }

    [Header("Refs")]
    public SkinnedMeshRenderer head;      // Drag M_head_001_mesh vào đây

    [Header("Expressions (thêm bao nhiêu cũng được)")]
    public Expression[] expressions;      // Ví dụ: Happy, Sad…

    // --- Talk (tùy chọn) ---
    [Header("Talk (optional)")]
    public string jawOpen = "jaw_open";
    public string[] phonemes = { "phoneme_Ah","phoneme_Ee","phoneme_Eh","phoneme_FV","phoneme_MBP","phoneme_Oo","phoneme_Oh","phoneme_R" };
    public Vector2 jawRange = new Vector2(35, 65);
    public Vector2 syllableDur = new Vector2(0.06f, 0.12f);

    Dictionary<string,int> _idx = new Dictionary<string, int>();
    Coroutine _fadeCo, _talkCo;

    void Awake()
    {
        if (!head) head = GetComponent<SkinnedMeshRenderer>();
        CacheBlendshapeIndex();
    }

    void CacheBlendshapeIndex()
    {
        _idx.Clear();
        if (!head || !head.sharedMesh) return;
        var m = head.sharedMesh;
        for (int i = 0; i < m.blendShapeCount; i++)
            _idx[m.GetBlendShapeName(i)] = i;
    }

    // ============= API chính =============
    public void Play(string id)
    {
        var ex = System.Array.Find(expressions, e => e.id == id);

        if (ex == null || head == null) return;
        Debug.Log(1);
        // Tập tất cả shape xuất hiện ở mọi preset -> mặc định 0
        var targets = new Dictionary<int, float>();
        foreach (var e in expressions)
            foreach (var s in e.shapes)
                if (TryIdx(s.name, out var i) && !targets.ContainsKey(i))
                    targets[i] = 0f;
        Debug.Log(2);
        // Gán mục tiêu cho preset được chọn
        foreach (var s in ex.shapes)
            if (TryIdx(s.name, out var i))
                targets[i] = s.weight;
        Debug.Log(3);
        StartFade(targets, ex.fade);
    }

    public void Clear(float fade = 0.1f)
    {
        var targets = new Dictionary<int, float>();
        foreach (var e in expressions)
            foreach (var s in e.shapes)
                if (TryIdx(s.name, out var i) && !targets.ContainsKey(i))
                    targets[i] = 0f;

        // Hạ talk về 0 luôn cho sạch
        if (TryIdx(jawOpen, out var j)) targets[j] = 0f;
        foreach (var n in phonemes) if (TryIdx(n, out var i)) targets[i] = 0f;
        StopTalk();

        StartFade(targets, fade);
    }

    public void StartTalk()
    {
        if (_talkCo != null || phonemes.Length == 0 || head == null) return;
        _talkCo = StartCoroutine(CoTalk());
    }

    public void StopTalk()
    {
        if (_talkCo != null) { StopCoroutine(_talkCo); _talkCo = null; }
        // Tắt miệng & phoneme
        var targets = new Dictionary<int, float>();
        if (TryIdx(jawOpen, out var j)) targets[j] = 0f;
        foreach (var n in phonemes) if (TryIdx(n, out var i)) targets[i] = 0f;
        StartFade(targets, 0.12f);
    }

    // ============= Nội bộ =============
    void StartFade(Dictionary<int, float> targets, float dur)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(targets, dur));
    }

    IEnumerator CoFade(Dictionary<int, float> targets, float dur)
    {
        var from = new Dictionary<int, float>();
        foreach (var kv in targets) from[kv.Key] = head.GetBlendShapeWeight(kv.Key);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = dur <= 0 ? 1f : t / dur;
            foreach (var kv in targets)
                head.SetBlendShapeWeight(kv.Key, Mathf.Lerp(from[kv.Key], kv.Value, a));
            yield return null;
        }
        foreach (var kv in targets)
            head.SetBlendShapeWeight(kv.Key, kv.Value);
        _fadeCo = null;
    }

    IEnumerator CoTalk()
    {
        while (true)
        {
            string p = phonemes[Random.Range(0, phonemes.Length)];
            float jaw = Random.Range(jawRange.x, jawRange.y);
            float dur = Random.Range(syllableDur.x, syllableDur.y);

            var targets = new Dictionary<int, float>();
            foreach (var n in phonemes) if (TryIdx(n, out var i)) targets[i] = (n == p) ? Random.Range(65f, 90f) : 0f;
            if (TryIdx(jawOpen, out var j)) targets[j] = jaw;

            StartFade(targets, dur * 0.5f);
            yield return new WaitForSeconds(dur);
        }
    }

    bool TryIdx(string name, out int idx) => _idx.TryGetValue(name, out idx);

    // Test nhanh trong Inspector (Play Mode)
    void _Happy() => Play("Happy");
    [ContextMenu("Play/Sad")]   void _Sad()   => Play("Sad");
    [ContextMenu("Talk/Start")] void _TalkOn()=> StartTalk();
    [ContextMenu("Talk/Stop")]  void _TalkOff()=> StopTalk();
}
