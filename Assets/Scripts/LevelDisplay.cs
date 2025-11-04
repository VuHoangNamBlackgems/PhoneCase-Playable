using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelDisplay : MonoBehaviour
{
    public TMP_Text textLevel;
    private int level;
    private void Awake()
    {
        textLevel.text = $"Level {UserLevel.Level.ToString()}";
        level = UserLevel.Level;
    }

    private void OnEnable()
    {
        textLevel.text = $"Level {UserLevel.Level.ToString()}";
    }

    private void Update()
    {
        if (UserLevel.Level != level)
        {
            level = UserLevel.Level;
            textLevel.text = $"Level {UserLevel.Level.ToString()}";
            return;
        }
    }
}
