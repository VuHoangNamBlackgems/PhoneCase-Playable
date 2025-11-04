using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomerOrderSO", menuName = "ScriptableObjects/CustomerOrderSO")]
public class CustomerOrderSO : ScriptableObject
{
    public OrderKind OrderKind;
    public Sprite Sprite;
    public GameObject Tool;
}

[System.Serializable]
public enum OrderKind
{
    Item,
    Tool
}
