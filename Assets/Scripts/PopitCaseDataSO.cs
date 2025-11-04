using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PopitCase Data", menuName = "Phone Case/Popit Case Data")]
public class PopitCaseDataSO : ScriptableObject
{
    public int id;
    public bool isUnlock = false;
    public Sprite icon;
    public PhoneCase casePopit;
    public BuyProperties BuyProperties;
}
