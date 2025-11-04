using UnityEngine;
using System.Collections.Generic;

public class GlitterStickOnHit : MonoBehaviour {
    public ParticleSystem ps;
    public Collider caseCollider;           // collider của case
    public Transform parentOnCase;          // để gọn hierarchy
    public GameObject glitterPiecePrefab;   // quad nhỏ (Billboard)
    public int poolSize = 1500;
    public Vector2 pieceSize = new Vector2(0.006f, 0.012f);

    Queue<GameObject> pool = new Queue<GameObject>();
    List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>();

    void Start(){
        if (!ps) ps = GetComponent<ParticleSystem>();
        for (int i=0;i<poolSize;i++){
            var g = Instantiate(glitterPiecePrefab, parentOnCase);
            g.SetActive(false); pool.Enqueue(g);
        }
    }

    void OnParticleCollision(GameObject other){
        if (other != caseCollider.gameObject) return;
        int count = ps.GetCollisionEvents(other, events);
        for (int i = 0; i < count; i++){
            SpawnPiece(events[i].intersection, events[i].normal);
        }
    }

    void SpawnPiece(Vector3 pos, Vector3 normal){
        if (pool.Count == 0) return;
        var g = pool.Dequeue(); g.SetActive(true);
        g.transform.position = pos + normal * 0.0004f;
        g.transform.rotation = Quaternion.LookRotation(normal)
                               * Quaternion.Euler(0, Random.value*360f, 0);
        float s = Random.Range(pieceSize.x, pieceSize.y);
        g.transform.localScale = new Vector3(s, s, s);
    }

    public void ClearAll(){
        foreach (Transform t in parentOnCase){
            if (t.gameObject.activeSelf){ t.gameObject.SetActive(false); pool.Enqueue(t.gameObject); }
        }
    }
}