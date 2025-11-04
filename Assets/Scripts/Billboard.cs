using UnityEngine;

public class Billboard : MonoBehaviour {
    Camera cam;
    void Awake(){ cam = Camera.main; }
    void LateUpdate(){
        if (!cam) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}