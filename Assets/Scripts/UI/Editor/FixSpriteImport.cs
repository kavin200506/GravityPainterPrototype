using UnityEditor;
using UnityEngine;

public static class FixSpriteImport
{
    [InitializeOnLoadMethod]
    public static void Fix()
    {
        string path = "Assets/Resources/UI/restart_icon.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Debug.Log("Fixed import settings for " + path);
        }
    }
}
