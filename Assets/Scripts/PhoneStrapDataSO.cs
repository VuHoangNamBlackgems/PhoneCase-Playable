using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PhoneStrap Data", menuName = "Phone Case/PhoneStrap Data")]
public class PhoneStrapDataSO : ScriptableObject
{
    public int id;
    public bool isUnlock = false;
    public Sprite icon;
    public GameObject phonestrapPrefab;
    public BuyProperties BuyProperties;
}
