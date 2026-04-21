using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Antigravity.Editor
{
    public class FinalFixWizard : EditorWindow
    {
        private DefaultAsset characterFolder;
        private string exportFolder = "Assets/PlayerAnimations_FINAL";
        
        [MenuItem("Window/! FINAL PLAYER SETUP")]
        public static void ShowWindow()
        {
            GetWindow<FinalFixWizard>("Final Player Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Final Player Setup Wizard v3.5 (ULTRA PREMIUM)", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            characterFolder = (DefaultAsset)EditorGUILayout.ObjectField("Character Folder:", characterFolder, typeof(DefaultAsset), false);
            EditorGUILayout.HelpBox("Instructions:\n1. Slice all Gun PNGs as 48x64 Grid.\n2. Drag the character folder here.\n3. Run.", MessageType.Info);

            GUI.enabled = characterFolder != null;
            if (GUILayout.Button("AUTO-SETUP CHARACTER NOW")) RunSetup();
            GUI.enabled = true;
        }

        private void RunSetup()
        {
            string selectionPath = AssetDatabase.GetAssetPath(characterFolder);
            string projectRoot = Path.GetDirectoryName(Application.dataPath).Replace("\\", "/");
            string rootPath = Path.Combine(projectRoot, selectionPath).Replace("\\", "/");
            
            Debug.Log($"[WIZARD v3.5] Starting Full 8-Way Setup from: {rootPath}");

            string idlePath = FindSubFolder(rootPath, "Idle", "Gun");
            string walkPath = FindSubFolder(rootPath, "Walk", "Gun");
            string attackPath = FindSubFolder(rootPath, "Attack", "Gun");
            string deathPath = FindSubFolder(rootPath, "Death", "Gun");

            if (!AssetDatabase.IsValidFolder(exportFolder)) AssetDatabase.CreateFolder("Assets", "PlayerAnimations_FINAL");

            // Setup all 8-way sets
            var idleClips = SetupDirectionalClips(idlePath, "Idle_Gun", "Idle", projectRoot);
            var walkClips = SetupDirectionalClips(walkPath, "Walk_Gun", "Walk", projectRoot);
            var attackClips = SetupDirectionalClips(attackPath, "Shooting", "Attack", projectRoot);
            var deathClips = SetupDirectionalClips(deathPath, "death_Gun", "Death", projectRoot);

            CreateAnimator(idleClips, walkClips, attackClips, deathClips);

            Debug.Log("[WIZARD v3.5] DONE! Your character is now a true 8-directional Legend.");
            AssetDatabase.Refresh();
        }

        private Dictionary<string, AnimationClip> SetupDirectionalClips(string path, string filePrefix, string animName, string root)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string[] dirs = { "up", "down", "right", "left", "right_up", "right_down", "left_up", "left_down" };
            var dict = new Dictionary<string, AnimationClip>();

            foreach (var d in dirs)
            {
                // Try both underscored and non-underscored filenames
                string name = $"{filePrefix}_{d}";
                AnimationClip clip = CreateClip(path, name, $"Player_{animName}_{d}", root);
                if (clip == null && !filePrefix.EndsWith("_")) clip = CreateClip(path, $"{filePrefix}{d}", $"Player_{animName}_{d}", root); // Fallback for "Shootinright" etc if needed
                
                if (clip != null) dict.Add(d, clip);
            }
            return dict;
        }

        private AnimationClip CreateClip(string absDir, string fileName, string outName, string root)
        {
            string assetPath = Path.Combine(absDir, fileName + ".png").Replace("\\", "/");
            string relPath = assetPath.Replace(root, "").Replace("\\", "/").TrimStart('/');
            TextureImporter imp = AssetImporter.GetAtPath(relPath) as TextureImporter;
            if (imp == null) return null;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.filterMode = FilterMode.Point;
            imp.spritePixelsPerUnit = 32;
            imp.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(relPath).OfType<Sprite>().Cast<Object>().ToArray();
            if (sprites.Length == 0) return null;

            AnimationClip clip = new AnimationClip();
            if (outName.Contains("Walk") || outName.Contains("Idle")) {
                var s = AnimationUtility.GetAnimationClipSettings(clip);
                s.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, s);
            }

            var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i * 0.12f, value = sprites[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AssetDatabase.CreateAsset(clip, Path.Combine(exportFolder, outName + ".anim").Replace("\\", "/"));
            return clip;
        }

        private void CreateAnimator(Dictionary<string, AnimationClip> idles, Dictionary<string, AnimationClip> walks, Dictionary<string, AnimationClip> attacks, Dictionary<string, AnimationClip> deaths)
        {
            string path = Path.Combine(exportFolder, "PlayerAnimator_ULTRA.controller").Replace("\\", "/");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var root = controller.layers[0].stateMachine;

            BlendTree idleBT;
            var idleS = controller.CreateBlendTreeInController("Idle", out idleBT);
            idleBT.blendType = BlendTreeType.SimpleDirectional2D;
            idleBT.blendParameter = "MoveX"; idleBT.blendParameterY = "MoveY";
            FillBT(idleBT, idles);

            BlendTree walkBT;
            var walkS = controller.CreateBlendTreeInController("Movement", out walkBT);
            walkBT.blendType = BlendTreeType.SimpleDirectional2D;
            walkBT.blendParameter = "MoveX"; walkBT.blendParameterY = "MoveY";
            FillBT(walkBT, walks);

            idleS.AddTransition(walkS).AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
            walkS.AddTransition(idleS).AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

            if (attacks != null && attacks.Count > 0) {
                BlendTree atkBT;
                var atkS = controller.CreateBlendTreeInController("Attack", out atkBT);
                atkBT.blendType = BlendTreeType.SimpleDirectional2D;
                atkBT.blendParameter = "MoveX"; atkBT.blendParameterY = "MoveY";
                FillBT(atkBT, attacks);
                root.AddAnyStateTransition(atkS).AddCondition(AnimatorConditionMode.If, 0, "Shoot");
                atkS.AddTransition(idleS).hasExitTime = true;
            }

            if (deaths != null && deaths.Count > 0) {
                BlendTree dieBT;
                var dieS = controller.CreateBlendTreeInController("Death", out dieBT);
                dieBT.blendType = BlendTreeType.SimpleDirectional2D;
                dieBT.blendParameter = "MoveX"; dieBT.blendParameterY = "MoveY";
                FillBT(dieBT, deaths);
                root.AddAnyStateTransition(dieS).AddCondition(AnimatorConditionMode.If, 0, "Die");
            }
        }

        private void FillBT(BlendTree t, Dictionary<string, AnimationClip> c) {
            if (c == null) return;
            if (c.ContainsKey("up")) t.AddChild(c["up"], new Vector2(0, 1));
            if (c.ContainsKey("down")) t.AddChild(c["down"], new Vector2(0, -1));
            if (c.ContainsKey("right")) t.AddChild(c["right"], new Vector2(1, 0));
            if (c.ContainsKey("left")) t.AddChild(c["left"], new Vector2(-1, 0));
            if (c.ContainsKey("right_up")) t.AddChild(c["right_up"], new Vector2(1, 1));
            if (c.ContainsKey("right_down")) t.AddChild(c["right_down"], new Vector2(1, -1));
            if (c.ContainsKey("left_up")) t.AddChild(c["left_up"], new Vector2(-1, 1));
            if (c.ContainsKey("left_down")) t.AddChild(c["left_down"], new Vector2(-1, -1));
        }

        private string FindSubFolder(string root, string p, string c) {
            if (!Directory.Exists(root)) return null;
            var res = Directory.GetDirectories(root, p, SearchOption.AllDirectories);
            foreach (var r in res) {
                string cp = Path.Combine(r, c).Replace("\\", "/");
                if (Directory.Exists(cp)) return cp;
            }
            return null;
        }
    }
}
