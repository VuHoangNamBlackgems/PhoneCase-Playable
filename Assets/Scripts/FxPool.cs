using System.Collections.Generic;
using UnityEngine;

public class FxPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int prewarm = 10;

    readonly Queue<GameObject> q = new Queue<GameObject>();
    Transform Root => transform;

    void Awake()
    {
        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(prefab, Root);
            go.SetActive(false);
            q.Enqueue(go);
        }
    }

    public GameObject Get(Transform parent)
    {
        var go = q.Count > 0 ? q.Dequeue() : Instantiate(prefab, Root);
        go.transform.SetParent(parent, false);
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(Root, false);
        q.Enqueue(go);
    }
}