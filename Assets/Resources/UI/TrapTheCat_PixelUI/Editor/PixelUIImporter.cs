// Put this file in any "Editor" folder. Any texture inside a folder named "TrapTheCat_PixelUI"
// is imported as a crisp pixel-art UI sprite, with 9-slice borders set automatically.
// Pair with Canvas Scaler: Scale With Screen Size, reference 640x360 (portrait: 360x640),
// Reference Pixels Per Unit 100. Then 1 art pixel = 1 canvas unit.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PixelUIImporter : AssetPostprocessor
{
    const string Folder = "TrapTheCat_PixelUI";

    // Unity border order: (left, bottom, right, top)
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "btn_cream_hover", new Vector4(4, 5, 4, 5) },
        { "btn_cream_normal", new Vector4(4, 5, 4, 5) },
        { "btn_cream_pressed", new Vector4(4, 5, 4, 5) },
        { "btn_cream_selected", new Vector4(4, 5, 4, 5) },
        { "btn_disabled", new Vector4(4, 5, 4, 5) },
        { "btn_green_hover", new Vector4(4, 5, 4, 5) },
        { "btn_green_normal", new Vector4(4, 5, 4, 5) },
        { "btn_green_pressed", new Vector4(4, 5, 4, 5) },
        { "btn_green_selected", new Vector4(4, 5, 4, 5) },
        { "btn_red_hover", new Vector4(4, 5, 4, 5) },
        { "btn_red_normal", new Vector4(4, 5, 4, 5) },
        { "btn_red_pressed", new Vector4(4, 5, 4, 5) },
        { "btn_red_selected", new Vector4(4, 5, 4, 5) },
        { "btn_wood_hover", new Vector4(4, 5, 4, 5) },
        { "btn_wood_normal", new Vector4(4, 5, 4, 5) },
        { "btn_wood_pressed", new Vector4(4, 5, 4, 5) },
        { "btn_wood_selected", new Vector4(4, 5, 4, 5) },
        { "btn_yellow_hover", new Vector4(4, 5, 4, 5) },
        { "btn_yellow_normal", new Vector4(4, 5, 4, 5) },
        { "btn_yellow_pressed", new Vector4(4, 5, 4, 5) },
        { "btn_yellow_selected", new Vector4(4, 5, 4, 5) },
        { "chip_cream", new Vector4(4, 4, 4, 4) },
        { "chip_dark", new Vector4(4, 4, 4, 4) },
        { "divider", new Vector4(2, 0, 2, 0) },
        { "input_focused", new Vector4(4, 4, 4, 4) },
        { "input_normal", new Vector4(4, 4, 4, 4) },
        { "panel_cream", new Vector4(6, 9, 6, 6) },
        { "panel_wood", new Vector4(6, 9, 6, 6) },
        { "ribbon_red", new Vector4(12, 6, 12, 5) },
        { "slider_fill", new Vector4(4, 3, 4, 3) },
        { "slider_track", new Vector4(4, 3, 4, 3) },
    };

    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/" + Folder + "/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;

        var name = Path.GetFileNameWithoutExtension(assetPath);
        if (Borders.TryGetValue(name, out var b))
            ti.spriteBorder = b;
    }
}
#endif
