using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastProbe : MonoBehaviour, IPointerDownHandler {
    public void OnPointerDown(PointerEventData e) {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, hits);
        Debug.Log("Top hit: " + (hits.Count>0 ? hits[0].gameObject.name : "NONE"));
        foreach (var h in hits) Debug.Log(" - " + h.gameObject.name + " (order=" + h.sortingOrder + ")");
    }
}