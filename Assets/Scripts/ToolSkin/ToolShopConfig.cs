// ToolShopConfig.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ToolCategory { Glue, Dryer, Table }

[Serializable]
public class ToolSkinDef
{
    public string id;
    public Sprite icon;
}

[Serializable]
public class CategoryDef
{
    public ToolCategory category;
    public List<ToolSkinDef> skins = new List<ToolSkinDef>();
}

[CreateAssetMenu(fileName = "ToolShopConfig", menuName = "Game/Shop/Tool Shop Config")]
public class ToolShopConfig : ScriptableObject
{
    public List<CategoryDef> categories = new List<CategoryDef>();
    public CategoryDef GetCategory(ToolCategory cat)
        => categories.FirstOrDefault(c => c.category == cat);
}