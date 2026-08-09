using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Game.Gameplay;

namespace Game.EditorTools
{
    /// <summary>
    /// <c>Assets/Art/SlimeStrips/</c> 의 스프라이트 스트립을 잘라 종별
    /// AnimatorController 를 만들고, 그걸 <see cref="SlimeSpecies"/> 자산에 꽂는다.
    ///
    /// 스트립은 <c>Tools/extract_slime_strips.py</c> 가 만든다 — Unity 의 GIF
    /// 임포터는 첫 프레임만 가져오기 때문에 프레임을 밖에서 펴야 한다.
    ///
    /// 스트립 규칙은 파일 모양 하나로 끝난다: <b>셀 = 이미지 높이,
    /// 프레임 수 = 너비 / 높이.</b> 원본이 전부 정사각이라 메타데이터가 필요 없다.
    /// 이름은 <c>{speciesId}__{Action}__{direction}.png</c>.
    /// </summary>
    public static class SlimeAnimationBuilder
    {
        private const string StripFolder = "Assets/Art/SlimeStrips";
        private const string OutputFolder = "Assets/Animations/Species";
        private const string PlayerClipFolder = "Assets/Animations/Player";

        // 플레이어는 이미 있는 컨트롤러를 **제자리에서** 채운다. 지웠다 다시 만들면
        // GUID 가 바뀌어 Player.prefab 의 Animator 배선이 끊긴다.
        private const string PlayerActor = "player";
        private const string PlayerControllerPath = "Assets/Animations/PlayerAnimator.controller";

        // 원본 GIF 은 전부 프레임당 200ms(=5fps)로 나왔다. 슬라임이 통통 뛰는
        // 속도로는 맞지만 사람이 달리고 주먹을 지르는 데 쓰면 슬로모션이 된다.
        private const float FrameRate = 5f;
        private const float PlayerMoveFrameRate = 10f;   // 4프레임 = 0.4초 한 걸음
        private const float MeltFrameRate = 8f;          // 5프레임 = 0.6초 만에 주저앉는다

        // 플레이어 공격은 SwingArc 가 떠 있는 동안 다 돌아야 한다. 그 시간은
        // Player.prefab 의 PlayerMeleeAttack.swingVisibleSeconds 가 정하므로
        // 여기서 그 값을 읽어 프레임 수로 나눈다 — 둘 중 하나만 바뀌어 어긋나는
        // 일이 없도록 상수로 박지 않는다.
        private const string SwingWindowField = "swingVisibleSeconds";
        private const float SwingWindowFallback = 0.12f;

        // 배우별 PPU 는 `extract_slime_strips.py` 가 **보이는 실루엣 높이**로 계산해
        // 여기에 적어 둔다. 셀 크기로 정하면 안 된다 — 캔버스 대비 실루엣 비율이
        // 종마다 달라(파랑 30/60, 무지개 38/120) 무지개만 36% 작게 나왔다.
        private const string PixelsPerUnitTable = StripFolder + "/ppu.csv";

        // 파일명의 방향 이름 ↔ 블렌드트리 칸 이름(ActorAnimatorGenerator.Directions).
        private static readonly Dictionary<string, string> DirectionKeys = new()
        {
            ["south"] = "S",
            ["south-west"] = "SW",
            ["west"] = "W",
            ["north-west"] = "NW",
            ["north"] = "N",
            ["north-east"] = "NE",
            ["east"] = "E",
            ["south-east"] = "SE",
        };

        // 대각선 클립이 없는 종(용암 슬라임은 4방향뿐)이 쓸 대체 방향.
        // 위/아래보다 좌우 그림이 대각선에 덜 어색하다.
        private static readonly Dictionary<string, string> DiagonalFallback = new()
        {
            ["NE"] = "E",
            ["SE"] = "E",
            ["NW"] = "W",
            ["SW"] = "W",
        };

        [MenuItem("SlimeRanch/Build Actor Animations")]
        public static void Build()
        {
            string[] stripPaths = AssetDatabase
                .FindAssets("t:Texture2D", new[] { StripFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p)
                .ToArray();

            if (stripPaths.Length == 0)
            {
                Debug.LogError($"slime_strips_missing: {StripFolder} 가 비었다. " +
                               "먼저 python Tools/extract_slime_strips.py 를 돌린다.");
                return;
            }

            _pixelsPerUnit = null;
            EnsureFolder(OutputFolder);

            // 1) 스트립을 프로젝트 관례대로 임포트한다 (Point / 압축 없음 / 격자 슬라이스).
            //    PPU 를 셀 크기와 같게 두면 원본 픽셀 크기가 60~172 로 제각각이어도
            //    월드에서는 전부 1×1 유닛이 되어 종끼리 크기가 튀지 않는다.
            foreach (string path in stripPaths)
            {
                SliceStrip(path);
            }

            AssetDatabase.Refresh();

            // 2) 스트립 → 클립.
            var clips = new Dictionary<(string Species, string Action, string Direction), AnimationClip>();
            foreach (string path in stripPaths)
            {
                if (!TryParseName(path, out string species, out string action, out string direction))
                {
                    Debug.LogWarning($"slime_strip_name_unparsed: {path}");
                    continue;
                }

                AnimationClip clip = BuildClip(path, species, action, direction);
                if (clip != null)
                {
                    clips[(species, action, DirectionKeys[direction])] = clip;
                }
            }

            // 3) 종마다 컨트롤러 하나. 이미 있으면 지우고 다시 만든다 — 클립 배선이
            //    통째로 이 스크립트에서 나오므로 손으로 끼운 것을 지킬 이유가 없다.
            int wired = 0;
            foreach (string species in clips.Keys.Select(k => k.Species).Distinct().OrderBy(s => s))
            {
                if (WireSpecies(species, clips))
                {
                    wired++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"slime_animations_built: strips={stripPaths.Length} clips={clips.Count} species={wired}");
        }

        private static void SliceStrip(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int cell = texture.height;
            int frames = texture.width / cell;
            string actor = Path.GetFileName(path).Split(new[] { "__" }, StringSplitOptions.None)[0];
            int pixelsPerUnit = PixelsPerUnit(actor, cell);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Multiple;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            string stem = Path.GetFileNameWithoutExtension(path);
            var sheet = new SpriteMetaData[frames];
            for (int i = 0; i < frames; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"{stem}_{i}",
                    rect = new Rect(i * cell, 0, cell, cell),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }

            importer.spritesheet = sheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static Dictionary<string, int> _pixelsPerUnit;

        /// <summary>배우의 PPU. 표에 없으면 셀 크기(캔버스=1유닛)로 물러난다.</summary>
        private static int PixelsPerUnit(string actor, int cell)
        {
            if (_pixelsPerUnit == null)
            {
                _pixelsPerUnit = new Dictionary<string, int>();
                foreach (string line in File.Exists(PixelsPerUnitTable)
                             ? File.ReadAllLines(PixelsPerUnitTable) : Array.Empty<string>())
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int value) && value > 0)
                    {
                        _pixelsPerUnit[parts[0].Trim()] = value;
                    }
                }

                if (_pixelsPerUnit.Count == 0)
                {
                    Debug.LogWarning($"ppu_table_missing: {PixelsPerUnitTable} — 셀 크기로 대신한다.");
                }
            }

            return _pixelsPerUnit.TryGetValue(actor, out int ppu) ? ppu : cell;
        }

        private static bool TryParseName(string path, out string species, out string action, out string direction)
        {
            species = action = direction = null;
            string[] parts = Path.GetFileNameWithoutExtension(path).Split(
                new[] { "__" }, StringSplitOptions.None);
            if (parts.Length != 3 || !DirectionKeys.ContainsKey(parts[2]))
            {
                return false;
            }

            species = parts[0];
            action = parts[1];
            direction = parts[2];
            return true;
        }

        private static AnimationClip BuildClip(string path, string species, string action, string direction)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => FrameIndexOf(s.name))
                .ToArray();

            if (frames.Length == 0)
            {
                Debug.LogWarning($"slime_strip_not_sliced: {path}");
                return null;
            }

            string folder = species == PlayerActor ? PlayerClipFolder : $"{OutputFolder}/{species}";
            EnsureFolder(folder);
            string clipPath = $"{folder}/{action}_{DirectionKeys[direction]}.anim";

            float frameRate = FrameRateFor(species, action, frames.Length);
            var clip = new AnimationClip { frameRate = frameRate };
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Idle/Move 는 계속 돌아야 하고, Attack/Hit/Dead 는 한 번 재생하고 멈춘다 —
            // 전이가 exitTime 으로 빠져나가므로 반복하면 영영 안 끝나고,
            // Dead 는 마지막 프레임(눌려서 검게 탄 모습)으로 남아 있어야 한다.
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = action is "Idle" or "Move";
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static float FrameRateFor(string actor, string action, int frames)
        {
            if (action == "Dead")
            {
                return MeltFrameRate;
            }

            if (actor != PlayerActor)
            {
                return FrameRate;
            }

            return action switch
            {
                // SwingArc 가 사라지기 전에 마지막 프레임까지 지나가야 한다.
                "Attack" => Mathf.Max(1f, frames / SwingWindowSeconds()),
                "Move" => PlayerMoveFrameRate,
                _ => FrameRate,
            };
        }

        /// <summary>
        /// Player.prefab 의 <c>swingVisibleSeconds</c>. 타입을 직접 참조하지 않고
        /// 직렬화 필드 이름으로 찾는다 — 컴포넌트가 갈려도 이 스크립트가 안 깨진다.
        /// </summary>
        private static float SwingWindowSeconds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (prefab != null)
            {
                foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                    {
                        continue;
                    }

                    SerializedProperty window = new SerializedObject(behaviour).FindProperty(SwingWindowField);
                    if (window != null && window.floatValue > 0f)
                    {
                        return window.floatValue;
                    }
                }
            }

            Debug.LogWarning($"swing_window_not_found: {SwingWindowField} 를 못 찾아 "
                             + $"{SwingWindowFallback}초로 가정한다.");
            return SwingWindowFallback;
        }

        private static int FrameIndexOf(string spriteName)
        {
            int underscore = spriteName.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(spriteName[(underscore + 1)..], out int index) ? index : 0;
        }

        private static bool WireSpecies(
            string species, Dictionary<(string, string, string), AnimationClip> clips)
        {
            AnimatorController controller;
            if (species == PlayerActor)
            {
                // 제자리 채우기. Player.prefab 이 GUID 로 물고 있어 지우면 배선이 끊긴다.
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
                if (controller == null)
                {
                    Debug.LogWarning($"player_controller_missing: {PlayerControllerPath} — "
                                     + "메뉴 SlimeRanch/Generate Actor Animators 를 먼저 실행한다.");
                    return false;
                }
            }
            else
            {
                string path = $"{OutputFolder}/{species}.controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }

                controller = ActorAnimatorGenerator.CreateController(path);
            }

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            // 슬라임은 이동 전용 그림이 없다 — 제자리에서 통통 뛰므로 Idle 을 그대로 쓴다.
            // 플레이어는 달리기 그림이 따로 있어 Resolve 가 그걸 집는다.
            FillTree(root, "Idle", species, "Idle", clips);
            FillTree(root, "Move", species, "Move", clips);
            FillTree(root, "Attack", species, "Attack", clips);

            // ponytail: Hit/Dead 상태는 방향이 없어 south 클립 한 장씩만 쓴다.
            // 방향별이 필요해지면 ActorAnimatorGenerator 에서 그 상태도
            // AddDirectionalState 로 바꾸고 여기도 FillTree 로 옮긴다.
            SetState(root, "Hit", Resolve(clips, species, "Hit", "S"));

            // Dead 를 비워 두면 애니메이터가 m_Sprite 를 아예 안 굴려서
            // 스프라이트가 **프리팹에 저장된 옛 그림으로 되돌아간다** — 슬라임이
            // 약화되는 순간 예전 PNG 가 튀어나오던 원인이다.
            SetState(root, "Dead", Resolve(clips, species, "Dead", "S"));
            TuneAttackTransitions(root);

            EditorUtility.SetDirty(controller);

            if (species == PlayerActor)
            {
                return true;
            }

            SlimeSpecies asset = LoadSpecies(species);
            if (asset == null)
            {
                Debug.LogWarning($"slime_species_asset_missing: {species}");
                return false;
            }

            asset.animatorController = controller;

            // 인벤토리·교배 UI 는 애니메이터를 안 거치고 정지 그림을 쓴다.
            // 남쪽 Idle 첫 프레임이 그 종의 대표 얼굴이다.
            AnimationClip idle = Resolve(clips, species, "Idle", "S");
            if (idle != null)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(
                    idle, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"));
                if (keys is { Length: > 0 } && keys[0].value is Sprite face)
                {
                    asset.defaultSprite = face;
                }
            }

            EditorUtility.SetDirty(asset);
            return true;
        }

        /// <summary>
        /// 공격 전이를 클립이 통째로 보이도록 손본다.
        /// </summary>
        /// <remarks>
        /// 들어가는 전이의 0.05초 크로스페이드는 0.12초짜리 플레이어 공격 클립의
        /// 절반 가까이를 이전 자세와 섞어 흐리게 만든다 — 0 으로 만들어 즉시 바꾼다.
        /// 나가는 전이의 <c>exitTime</c> 도 0.9 면 마지막 프레임이 뜨기 전에
        /// 빠져나가므로 1.0 으로 올려 끝까지 재생시킨다.
        /// 이 조정은 <b>모든 컨트롤러</b>에 건다 — 이미 있는 PlayerAnimator 는
        /// 제자리에서 채워져 ActorAnimatorGenerator 를 안 거치기 때문이다.
        /// </remarks>
        private static void TuneAttackTransitions(AnimatorStateMachine root)
        {
            AnimatorState attack = root.states.FirstOrDefault(s => s.state.name == "Attack").state;
            if (attack == null)
            {
                return;
            }

            foreach (ChildAnimatorState child in root.states)
            {
                foreach (AnimatorStateTransition transition in child.state.transitions)
                {
                    if (transition.destinationState == attack)
                    {
                        transition.duration = 0f;
                    }
                }
            }

            foreach (AnimatorStateTransition transition in attack.transitions)
            {
                if (transition.hasExitTime)
                {
                    transition.exitTime = 1f;
                }
            }
        }

        private static void SetState(AnimatorStateMachine root, string stateName, AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AnimatorState state = root.states.FirstOrDefault(s => s.state.name == stateName).state;
            if (state != null)
            {
                state.motion = clip;
            }
        }

        private static void FillTree(
            AnimatorStateMachine root,
            string stateName,
            string species,
            string action,
            Dictionary<(string, string, string), AnimationClip> clips)
        {
            AnimatorState state = root.states.FirstOrDefault(s => s.state.name == stateName).state;
            if (state == null || state.motion is not BlendTree tree)
            {
                return;
            }

            ChildMotion[] children = tree.children;
            for (int i = 0; i < children.Length && i < ActorAnimatorGenerator.Directions.Length; i++)
            {
                children[i].motion = Resolve(clips, species, action, ActorAnimatorGenerator.Directions[i].Name);
            }

            tree.children = children;
        }

        /// <summary>
        /// 이 종·동작·방향의 클립. 없으면 대각선→인접 가로방향, 그래도 없으면
        /// Idle 로 물러난다 — 빈 칸은 그 상태에서 그림이 멈춘 것처럼 보인다.
        /// </summary>
        private static AnimationClip Resolve(
            Dictionary<(string, string, string), AnimationClip> clips,
            string species,
            string action,
            string direction)
        {
            if (clips.TryGetValue((species, action, direction), out AnimationClip clip))
            {
                return clip;
            }

            if (DiagonalFallback.TryGetValue(direction, out string near)
                && clips.TryGetValue((species, action, near), out clip))
            {
                return clip;
            }

            if (action == "Idle")
            {
                return clips.TryGetValue((species, "Idle", "S"), out clip) ? clip : null;
            }

            return Resolve(clips, species, "Idle", direction);
        }

        private static SlimeSpecies LoadSpecies(string speciesId)
        {
            return AssetDatabase.FindAssets($"t:{nameof(SlimeSpecies)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SlimeSpecies>)
                .FirstOrDefault(s => s != null && s.speciesId == speciesId);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
