using UnityEngine;

[CreateAssetMenu(fileName = "Charm", menuName = "Phone Case/Charm Definition")]
public class CharmDefinitionDataSO : ScriptableObject
{
    public int id;

    public bool isUnlock = false;
    
    public Sprite icon;
    
    public DraggableOnSurface prefab;
}