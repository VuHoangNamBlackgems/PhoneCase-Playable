using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class PopItNode : MonoBehaviour
{
    private BoxCollider _collider;
    private SkinnedMeshRenderer _meshRenderer;

    private const float DURATION = 0.2f;
    private const float MAX_BSHAPE_VALUE = 100f;

    [SerializeField] private int _blendShapeIndex = 0;

    public bool IsClicked { get; private set; }

    void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<SkinnedMeshRenderer>()
                      ?? GetComponentInChildren<SkinnedMeshRenderer>()
                      ?? GetComponentInParent<SkinnedMeshRenderer>();

        if (_meshRenderer && _meshRenderer.sharedMesh)
            _blendShapeIndex = Mathf.Clamp(
                _blendShapeIndex, 0, _meshRenderer.sharedMesh.blendShapeCount - 1);
        else
            Debug.LogWarning($"{name}: No SkinnedMeshRenderer/BlendShape.");
    }

    public void EnableNode(bool isEnable)
    {
        if (_collider) _collider.enabled = isEnable;
        enabled = isEnable;
    }

    public void SetMaterial(Material m)
    {
        if (!_meshRenderer || !m) return;
        var mats = _meshRenderer.materials;
        for (int i = 0; i < mats.Length; i++) mats[i] = m;
        _meshRenderer.materials = mats;
    }

    /// <summary>true = nhấn xuống (100), false = nhả (0)</summary>
    public void SwitchState(bool isClicked)
    {
        if (!_meshRenderer) return;
        if (IsClicked == isClicked) return;

        IsClicked = isClicked;
        StopAllCoroutines();
        StartCoroutine(SwitchBlendShape(IsClicked ? MAX_BSHAPE_VALUE : 0f));
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (_meshRenderer) _meshRenderer.SetBlendShapeWeight(_blendShapeIndex, 0f);
        IsClicked = false;
    }

    private IEnumerator SwitchBlendShape(float target)
    {
        if (!_meshRenderer) yield break;
        float start = _meshRenderer.GetBlendShapeWeight(_blendShapeIndex);
        float elapsed = 0f;
        while (elapsed < DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DURATION);
            _meshRenderer.SetBlendShapeWeight(_blendShapeIndex, Mathf.Lerp(start, target, t));
            yield return null;
        }
        _meshRenderer.SetBlendShapeWeight(_blendShapeIndex, target);
    }
}
