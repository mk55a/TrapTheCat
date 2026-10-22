// Place in any "Editor" folder. Requires the 2D Sprite package (com.unity.2d.sprite).
// Select hex_tiles_sheet.png in the Project window, then:
//   Tools > Trap The Cat > Slice Hex Tile Sheet
// It sets pixel-art import settings and slices 16 named sprites with the correct pivot.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class HexTileSheetImporter
{
    const int CellW = 32, CellH = 40, Cols = 8;
    // Pivot = centre of the 32x32 hex footprint (bottom 32 px of each 32x40 cell).
    static readonly Vector2 Pivot = new Vector2(0.5f, 0.4f);

    static readonly string[] Names =
    {
        "empty_0", "empty_1", "empty_2", "empty_hover",
        "empty_pressed", "empty_edge", "empty_path", "empty_disabled",
        "blocked_0", "blocked_1", "blocked_2", "blocked_flash",
        "blocked_pop_0", "blocked_pop_1", "blocked_pop_2", "blocked_pop_3",
    };

    [MenuItem("Tools/Trap The Cat/Slice Hex Tile Sheet")]
    static void Slice()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null) { Debug.LogWarning("Select hex_tiles_sheet.png first."); return; }

        string path = AssetDatabase.GetAssetPath(tex);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var rows = Names.Length / Cols;
        var texH = rows * CellH;
        var rects = new List<SpriteRect>();
        var nameIds = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < Names.Length; i++)
        {
            int col = i % Cols, row = i / Cols;
            var r = new SpriteRect
            {
                name = Names[i],
                spriteID = GUID.Generate(),
                // Unity rects start bottom-left; row 0 is the top row of the image.
                rect = new Rect(col * CellW, texH - (row + 1) * CellH, CellW, CellH),
                alignment = SpriteAlignment.Custom,
                pivot = Pivot,
            };
            rects.Add(r);
            nameIds.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        }

        dp.SetSpriteRects(rects.ToArray());
        dp.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(nameIds);
        dp.Apply();
        importer.SaveAndReimport();
        Debug.Log($"Sliced {rects.Count} hex tiles from {path}");
    }
}
#endif
