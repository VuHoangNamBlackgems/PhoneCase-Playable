using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameState
{
    public static State State { get; set; }
}

[Serializable]
public enum State
{
    RUNNING, NO_ACTION
}
