
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Spray Data", menuName = "Phone Case/Spray Data")]
public class SprayDataSO : ScriptableObject
{
    public int id;
    public bool isUnlock = false;
    public Sprite icon;
    public ParticleSystem brush;
    public ParticleSystem spray;
    public Texture2D textureSprayCan;
    public bool textureOnCase;
    public Color textureColor;
    public Texture2D textureCase;
}
