using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharmEvents
{
    public static event Action<CharmDefinitionDataSO, CharmPreview> OnSpawnRequested;

    public static void RequestSpawn(CharmDefinitionDataSO def, CharmPreview preview)
        => OnSpawnRequested?.Invoke(def, preview);
}
