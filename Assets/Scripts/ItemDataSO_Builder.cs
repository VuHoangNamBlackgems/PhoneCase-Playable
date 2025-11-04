#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ItemDataSO_Builder
{
    [MenuItem("Tools/Phone Case/Generate ItemData from Sprites...")]
    public static void Generate()
    {
        // 1) Chọn thư mục chứa sprites
        var srcAbs = EditorUtility.OpenFolderPanel("Select SPRITES folder", "Assets/Texture/UI", "");
        if (string.IsNullOrEmpty(srcAbs)) return;

        // 2) Chọn thư mục output cho các SO
        var dstAbs = EditorUtility.OpenFolderPanel("Select OUTPUT (ScriptableObjects) folder", "Assets/Resources/SO/Sticker2D", "");
        if (string.IsNullOrEmpty(dstAbs)) return;

        var src = ToProjectRelativePath(srcAbs);
        var dst = ToProjectRelativePath(dstAbs);
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst))
        {
            EditorUtility.DisplayDialog("Error", "Hãy chọn thư mục nằm trong Assets/", "OK");
            return;
        }

        // 3) Lấy tất cả Sprite trong thư mục nguồn (kể cả subfolders)
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { src });
        int created = 0, updated = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in spriteGuids)
            {
                var spritePath = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) continue;

                // Đặt tên SO theo tên sprite
                string soName = sprite.name + "_Item";
                string soPath = Path.Combine(dst, soName + ".asset").Replace("\\", "/");

                // Nếu đã có SO -> cập nhật, nếu chưa -> tạo mới
                var so = AssetDatabase.LoadAssetAtPath<ItemDataSO>(soPath);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<ItemDataSO>();
                    // Gợi ý id: nếu tên có số thì lấy; không có thì dùng HashCode dương
                    so.id = ExtractInt(sprite.name);
                    so._iconPreview = sprite;
                    so.isUnlock = true;
                    if (so.BuyProperties == null) so.BuyProperties = new BuyProperties();
                    so.BuyProperties.CurrencyType = CurrencyType.CASH;
                    so.BuyProperties.Amount = 0;

                    AssetDatabase.CreateAsset(so, soPath);
                    created++;
                }
                else
                {
                    // Cập nhật (id giữ nguyên nếu đã set)
                    if (so._iconPreview != sprite)
                        so._iconPreview = sprite;
                    if (so.BuyProperties == null) so.BuyProperties = new BuyProperties();
                    updated++;
                    EditorUtility.SetDirty(so);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Done",
            $"Sprites: {spriteGuids.Length}\nCreated: {created}\nUpdated: {updated}\nOutput: {dst}",
            "OK");
    }

    // Helpers
    static string ToProjectRelativePath(string abs)
    {
        abs = abs.Replace("\\", "/");
        var proj = Application.dataPath.Replace("\\", "/");
        if (!abs.StartsWith(proj)) return null;
        return "Assets" + abs.Substring(proj.Length);
    }

    static int ExtractInt(string s)
    {
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var id)) return id;
        // fallback: hash dương
        return Mathf.Abs(s.GetHashCode());
    }
}
#endif
