using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Glitter Data", menuName = "Phone Case/Glitter Data")]
public class GlitterDataSO : ScriptableObject
{
    public int id;
    public bool isUnlock = false;
    public Sprite icon;
    public ParticleSystem brush;
    public Texture2D textureGlitter;
    public Color textureColor;
    public BuyProperties BuyProperties;
}
