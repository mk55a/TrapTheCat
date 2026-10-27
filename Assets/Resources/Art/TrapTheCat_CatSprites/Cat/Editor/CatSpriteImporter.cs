namespace TrapTheCat.EditorTools
{
    #if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEditor.U2D.Sprites;
    using UnityEngine;

    public static class CatSpriteImporter
    {
        const int Frame = 32, Frames = 8;
        static readonly Vector2 Pivot = new Vector2(0.5f, 2f / 32f);

        static readonly Dictionary<string, (float fps, bool loop)> Clips = new Dictionary<string, (float, bool)>
        {
            { "Cat-Idle",     (8f,  true)  },
            { "Cat-Laying",   (6f,  true)  },
            { "Cat-Licking",  (8f,  true)  },
            { "Cat-Meow",     (10f, false) },
            { "Cat-Run",      (12f, true)  },
            { "Cat-Sitting",  (6f,  true)  },
            { "Cat-Sleeping", (4f,  true)  },
            { "Cat-Walk",     (10f, true)  },
        };

        [MenuItem("Tools/Trap The Cat/Build Cat Animations")]
        static void Build()
        {
            var paths = new List<string>();
            foreach (var o in Selection.objects)
            {
                var p = AssetDatabase.GetAssetPath(o);
                if (AssetDatabase.IsValidFolder(p))
                    paths.AddRange(AssetDatabase.FindAssets("t:Texture2D Cat-", new[] { p }).Select(AssetDatabase.GUIDToAssetPath));
                else if (p.EndsWith(".png")) paths.Add(p);
            }
            paths = paths.Where(p => Clips.ContainsKey(Path.GetFileNameWithoutExtension(p))).Distinct().ToList();
            if (paths.Count == 0) { Debug.LogWarning("Select the folder containing the Cat-*.png sheets."); return; }

            string dir = Path.GetDirectoryName(paths[0]).Replace('\\', '/');
            var controllerPath = dir + "/Cat.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;

            foreach (var path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var sprites = SliceSheet(path, name);
                var (fps, loop) = Clips[name];
                var clip = MakeClip(sprites, fps, loop, name);
                string clipPath = $"{dir}/{name}.anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existing) { EditorUtility.CopySerialized(clip, existing); clip = existing; }
                else AssetDatabase.CreateAsset(clip, clipPath);

                string stateName = name.Replace("Cat-", "");
                var state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
                            ?? sm.AddState(stateName);
                state.motion = clip;
                if (stateName == "Idle") sm.defaultState = state;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Built {paths.Count} cat animations + {controllerPath}");
        }

        static Sprite[] SliceSheet(string path, string name)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 32;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>();
            var ids = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < Frames; i++)
            {
                var r = new SpriteRect
                {
                    name = $"{name}_{i}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(i * Frame, 0, Frame, Frame),
                    alignment = SpriteAlignment.Custom,
                    pivot = Pivot,
                };
                rects.Add(r);
                ids.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            }
            dp.SetSpriteRects(rects.ToArray());
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(ids);
            dp.Apply();
            ti.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        }

        static AnimationClip MakeClip(Sprite[] sprites, float fps, bool loop, string name)
        {
            var clip = new AnimationClip { frameRate = fps, name = name };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[sprites.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }
    }
    #endif
}
