using System;
using System.Collections.Generic;
using System.Reflection;
using FenShen.Combat;
using UnityEditor;
using UnityEngine;

public class CombatSkillEditorWindow : EditorWindow
{
    private enum TimelineClipKind
    {
        None = 0,
        Hit = 1,
        Movement = 2,
        Derivation = 3,
        Cancel = 4,
        Projectile = 5,
        Vfx = 6,
        Sfx = 7,
        Camera = 8,
    }

    private struct TimelineDragState
    {
        public bool isDragging;
        public TimelineClipKind kind;
        public int index;
        public float startMouseX;
        public float startClipTime;
    }

    private readonly List<CombatSkillDefinitionSO> _skills = new List<CombatSkillDefinitionSO>();
    private Vector2 _skillListScroll;
    private Vector2 _inspectorScroll;
    private string _searchText = string.Empty;
    private CombatSkillDefinitionSO _selectedSkill;
    private CombatSkillPreviewAnchor _previewAnchor;
    private float _previewTime;
    private int _previewFrame;
    private bool _drawPreviewHitboxes = true;
    private bool _drawPreviewVfx = true;
    private bool _drawPreviewProjectiles = true;
    private bool _drawPreviewCamera = true;
    private bool _snapToFrames = true;
    private bool _isPreviewPlaying;
    private bool _loopPreview = true;
    private float _previewPlaybackSpeed = 1f;
    private double _lastPreviewEditorTime;
    private TimelineDragState _timelineDrag;
    private static readonly Color TimelineBackground = new Color(0.15f, 0.15f, 0.17f, 1f);
    private static readonly Color TimelineHitClip = new Color(1f, 0.45f, 0.2f, 0.9f);
    private static readonly Color TimelineDerivationClip = new Color(0.3f, 0.9f, 0.45f, 0.9f);
    private static readonly Color TimelineCancelClip = new Color(1f, 0.85f, 0.2f, 0.9f);
    private static readonly Color TimelineMovementClip = new Color(0.45f, 0.6f, 1f, 0.9f);
    private static readonly Color TimelineProjectileClip = new Color(1f, 0.35f, 0.85f, 0.9f);
    private static readonly Color TimelineVfxClip = new Color(0.35f, 1f, 0.95f, 0.9f);
    private static readonly Color TimelineSfxClip = new Color(0.95f, 0.95f, 0.35f, 0.9f);
    private static readonly Color TimelineCameraClip = new Color(1f, 0.6f, 0.3f, 0.9f);
    private static readonly Color TimelinePlayhead = new Color(0.2f, 0.9f, 1f, 1f);
    private static readonly Color PreviewFill = new Color(0.15f, 0.85f, 1f, 0.18f);
    private static readonly Color PreviewWire = new Color(0.1f, 0.95f, 1f, 1f);
    private static readonly Color PreviewVfxColor = new Color(0.25f, 1f, 0.95f, 0.9f);
    private static readonly Color PreviewProjectileColor = new Color(1f, 0.35f, 0.85f, 0.9f);
    private static readonly Color PreviewCameraColor = new Color(1f, 0.7f, 0.3f, 0.95f);
    private static readonly Dictionary<string, GameObject> ActivePreviewVfx = new Dictionary<string, GameObject>();
    private static readonly Dictionary<string, GameObject> ActivePreviewProjectiles = new Dictionary<string, GameObject>();
    private static readonly HashSet<string> PlayedPreviewSfx = new HashSet<string>();
    private static readonly Type AudioUtilType = Type.GetType("UnityEditor.AudioUtil, UnityEditor");
    private static readonly MethodInfo PlayPreviewClipMethod = AudioUtilType != null
        ? AudioUtilType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
            ?? AudioUtilType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(AudioClip), typeof(int), typeof(int), typeof(bool) }, null)
        : null;
    private static readonly MethodInfo StopAllPreviewClipsMethod = AudioUtilType != null
        ? AudioUtilType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        : null;

    [MenuItem("Window/Combat/Skill Editor")]
    public static void ShowWindow()
    {
        GetWindow<CombatSkillEditorWindow>("Skill Editor");
    }

    void OnEnable()
    {
        RefreshSkills();
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnEditorUpdate;
        AutoBindFromSelection();
    }

    void OnDisable()
    {
        StopPreviewPlayback();
        ClearScenePreviewObjects();
        StopPreviewAudio();
        StopAnimationPreview();
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawSkillListPane();
        DrawEditorPane();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillListPane()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260f));
        EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        string newSearchText = EditorGUILayout.TextField(_searchText);
        if (newSearchText != _searchText)
        {
            _searchText = newSearchText;
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
        {
            RefreshSkills();
        }
        EditorGUILayout.EndHorizontal();

        _skillListScroll = EditorGUILayout.BeginScrollView(_skillListScroll);
        for (int i = 0; i < _skills.Count; i++)
        {
            CombatSkillDefinitionSO skill = _skills[i];
            if (skill == null || !MatchesSearch(skill))
            {
                continue;
            }

            GUIStyle style = skill == _selectedSkill ? EditorStyles.toolbarButton : EditorStyles.miniButton;
            if (GUILayout.Button(skill.DisplayName, style))
            {
                SelectSkill(skill);
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Use Selected Asset"))
        {
            AutoBindSkillFromSelection();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEditorPane()
    {
        EditorGUILayout.BeginVertical();
        if (_selectedSkill == null)
        {
            EditorGUILayout.HelpBox("Select a CombatSkillDefinitionSO from the list or current selection.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        SerializedObject skillObject = new SerializedObject(_selectedSkill);
        skillObject.Update();

        _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
        EditorGUILayout.LabelField("Skill", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("Asset", _selectedSkill, typeof(CombatSkillDefinitionSO), false);
        EditorGUILayout.PropertyField(skillObject.FindProperty("skillId"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("displayName"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("description"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("previewAnimationClip"));
        DrawEntryGateEditor(skillObject);
        EditorGUILayout.PropertyField(skillObject.FindProperty("timeline"));
        DrawRecoveryRuleEditor(skillObject);

        SerializedProperty timelineProperty = skillObject.FindProperty("timeline");
        SkillTimelineSO timeline = timelineProperty.objectReferenceValue as SkillTimelineSO;
        if (timeline == null)
        {
            EditorGUILayout.HelpBox("Assign a SkillTimelineSO to edit timeline content.", MessageType.Warning);
            if (GUILayout.Button("Create Timeline Asset"))
            {
                CreateTimelineAsset(skillObject, timelineProperty);
            }

            skillObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space(8f);
        DrawPreviewControls(timeline);
        DrawTimelineOverview(timeline);
        DrawHitClipEditor(timeline);
        DrawProjectileClipEditor(timeline);
        DrawVfxClipEditor(timeline);
        DrawSfxClipEditor(timeline);
        DrawCameraClipEditor(timeline);
        DrawDerivationClipEditor(timeline);
        DrawCancelClipEditor(timeline);
        DrawMovementClipEditor(timeline);

        skillObject.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEntryGateEditor(SerializedObject skillObject)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Entry Gate", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(skillObject.FindProperty("groundedOnly"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("aerialOnly"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("requiredWeaponTraits"));
        EditorGUILayout.PropertyField(skillObject.FindProperty("previousSkills"), true);

        SerializedProperty enterConditionsProperty = skillObject.FindProperty("enterConditions");
        if (enterConditionsProperty != null)
        {
            SerializedProperty logicProperty = enterConditionsProperty.FindPropertyRelative("logic");
            SerializedProperty conditionsProperty = enterConditionsProperty.FindPropertyRelative("conditions");
            if (logicProperty != null)
            {
                EditorGUILayout.PropertyField(logicProperty);
            }

            if (conditionsProperty != null)
            {
                EditorGUILayout.PropertyField(conditionsProperty, true);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRecoveryRuleEditor(SerializedObject skillObject)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Recovery Rules", EditorStyles.boldLabel);

        SerializedProperty recoveryRulesProperty = skillObject.FindProperty("recoveryRules");
        for (int i = 0; i < recoveryRulesProperty.arraySize; i++)
        {
            SerializedProperty ruleProperty = recoveryRulesProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = ruleProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue)
                ? $"Recovery Rule {i + 1}"
                : displayNameProperty.stringValue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                recoveryRulesProperty.DeleteArrayElementAtIndex(i);
                skillObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_selectedSkill);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("nextSkill"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("description"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("requiresHitConfirm"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("requiresGrounded"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("requiresAerial"));
            EditorGUILayout.PropertyField(ruleProperty.FindPropertyRelative("conditions"), true);
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Recovery Rule"))
        {
            int index = recoveryRulesProperty.arraySize;
            recoveryRulesProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newRule = recoveryRulesProperty.GetArrayElementAtIndex(index);
            newRule.FindPropertyRelative("displayName").stringValue = $"Recovery Rule {index + 1}";
            newRule.FindPropertyRelative("requiresHitConfirm").boolValue = false;
            newRule.FindPropertyRelative("requiresGrounded").boolValue = false;
            newRule.FindPropertyRelative("requiresAerial").boolValue = false;
        }
    }

    private void DrawPreviewControls(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        _previewAnchor = (CombatSkillPreviewAnchor)EditorGUILayout.ObjectField("Anchor", _previewAnchor, typeof(CombatSkillPreviewAnchor), true);
        _drawPreviewHitboxes = EditorGUILayout.Toggle("Draw Hitboxes", _drawPreviewHitboxes);
        _drawPreviewVfx = EditorGUILayout.Toggle("Draw VFX", _drawPreviewVfx);
        _drawPreviewProjectiles = EditorGUILayout.Toggle("Draw Projectiles", _drawPreviewProjectiles);
        _drawPreviewCamera = EditorGUILayout.Toggle("Draw Camera Preview", _drawPreviewCamera);
        _snapToFrames = EditorGUILayout.Toggle("Snap To Frames", _snapToFrames);
        DrawPreviewPlaybackToolbar(timeline);

        float maxTime = Mathf.Max(0.01f, timeline.length);
        float newPreviewTime = EditorGUILayout.Slider("Preview Time", _previewTime, 0f, maxTime);
        if (!Mathf.Approximately(newPreviewTime, _previewTime))
        {
            SetPreviewTime(newPreviewTime);
        }

        if (_selectedSkill != null && _selectedSkill.previewAnimationClip != null)
        {
            float fps = _selectedSkill.ResolvePreviewFrameRate();
            int maxFrame = Mathf.Max(1, Mathf.RoundToInt(_selectedSkill.previewAnimationClip.length * fps));
            int newPreviewFrame = EditorGUILayout.IntSlider("Preview Frame", Mathf.Clamp(_previewFrame, 0, maxFrame), 0, maxFrame);
            if (newPreviewFrame != _previewFrame)
            {
                _previewFrame = newPreviewFrame;
                float frameTime = Mathf.Clamp(newPreviewFrame / fps, 0f, maxTime);
                SetPreviewTime(frameTime, false);
            }

            EditorGUILayout.LabelField("Frame Rate", fps.ToString("0.##"));
        }

        DrawPreviewEventSummary(timeline);
    }

    private void DrawPreviewPlaybackToolbar(SkillTimelineSO timeline)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(_isPreviewPlaying ? "Pause" : "Play", GUILayout.Width(64f)))
        {
            if (_isPreviewPlaying)
            {
                PausePreviewPlayback();
            }
            else
            {
                StartPreviewPlayback();
            }
        }

        if (GUILayout.Button("Stop", GUILayout.Width(64f)))
        {
            StopPreviewPlayback();
            SetPreviewTime(0f);
        }

        _loopPreview = EditorGUILayout.ToggleLeft("Loop", _loopPreview, GUILayout.Width(54f));
        _previewPlaybackSpeed = EditorGUILayout.Slider("Speed", _previewPlaybackSpeed, 0.1f, 2f);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreviewEventSummary(SkillTimelineSO timeline)
    {
        int hitCount = timeline != null && timeline.hitClips != null ? timeline.hitClips.Count : 0;
        int projectileCount = timeline != null && timeline.projectileClips != null ? timeline.projectileClips.Count : 0;
        int vfxCount = timeline != null && timeline.vfxClips != null ? timeline.vfxClips.Count : 0;
        int sfxCount = timeline != null && timeline.sfxClips != null ? timeline.sfxClips.Count : 0;
        int cameraCount = timeline != null && timeline.cameraClips != null ? timeline.cameraClips.Count : 0;
        EditorGUILayout.HelpBox(
            $"Preview Tracks  Hit:{hitCount}  Projectile:{projectileCount}  VFX:{vfxCount}  SFX:{sfxCount}  Camera:{cameraCount}",
            MessageType.None);
    }

    private void DrawTimelineOverview(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, 156f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, TimelineBackground);

        float length = Mathf.Max(0.01f, timeline.length);
        float fps = GetPreviewFrameRate();
        DrawFrameGrid(rect, length, fps);
        List<TimelineClipRect> clipRects = new List<TimelineClipRect>();
        if (timeline.hitClips != null)
        {
            for (int i = 0; i < timeline.hitClips.Count; i++)
            {
                HitSkillClip clip = timeline.hitClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 18f, width, 20f);
                EditorGUI.DrawRect(clipRect, TimelineHitClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Hit {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Hit, i, clipRect));
            }
        }

        if (timeline.projectileClips != null)
        {
            for (int i = 0; i < timeline.projectileClips.Count; i++)
            {
                ProjectileSkillClip clip = timeline.projectileClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 40f, width, 18f);
                EditorGUI.DrawRect(clipRect, TimelineProjectileClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Projectile {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Projectile, i, clipRect));
            }
        }

        if (timeline.movementClips != null)
        {
            for (int i = 0; i < timeline.movementClips.Count; i++)
            {
                MovementSkillClip clip = timeline.movementClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 62f, width, 18f);
                EditorGUI.DrawRect(clipRect, TimelineMovementClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Move {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Movement, i, clipRect));
            }
        }

        if (timeline.vfxClips != null)
        {
            for (int i = 0; i < timeline.vfxClips.Count; i++)
            {
                VfxSkillClip clip = timeline.vfxClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 84f, width, 16f);
                EditorGUI.DrawRect(clipRect, TimelineVfxClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"VFX {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Vfx, i, clipRect));
            }
        }

        if (timeline.sfxClips != null)
        {
            for (int i = 0; i < timeline.sfxClips.Count; i++)
            {
                SfxSkillClip clip = timeline.sfxClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 102f, width, 12f);
                EditorGUI.DrawRect(clipRect, TimelineSfxClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"SFX {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Sfx, i, clipRect));
            }
        }

        if (timeline.cameraClips != null)
        {
            for (int i = 0; i < timeline.cameraClips.Count; i++)
            {
                CameraShakeSkillClip clip = timeline.cameraClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 116f, width, 12f);
                EditorGUI.DrawRect(clipRect, TimelineCameraClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Camera {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Camera, i, clipRect));
            }
        }

        if (timeline.derivationWindowClips != null)
        {
            for (int i = 0; i < timeline.derivationWindowClips.Count; i++)
            {
                DerivationWindowSkillClip clip = timeline.derivationWindowClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 132f, width, 12f);
                EditorGUI.DrawRect(clipRect, TimelineDerivationClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Derive {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Derivation, i, clipRect));
            }
        }

        if (timeline.cancelWindowClips != null)
        {
            for (int i = 0; i < timeline.cancelWindowClips.Count; i++)
            {
                CancelWindowSkillClip clip = timeline.cancelWindowClips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                float xMin = rect.x + (clip.startTime / length) * rect.width;
                float width = Mathf.Max(2f, (Mathf.Max(0.01f, clip.duration) / length) * rect.width);
                Rect clipRect = new Rect(xMin, rect.y + 144f, width, 10f);
                EditorGUI.DrawRect(clipRect, TimelineCancelClip);
                GUI.Label(clipRect, clip.cancelPermission.ToString(), EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Cancel, i, clipRect));
            }
        }

        float playheadX = rect.x + (_previewTime / length) * rect.width;
        EditorGUI.DrawRect(new Rect(playheadX, rect.y, 2f, rect.height), TimelinePlayhead);

        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 16f), $"Length: {length:0.000}s ({Mathf.RoundToInt(length * fps)}f @ {fps:0.##}fps)", EditorStyles.miniBoldLabel);
        HandleTimelineDragging(rect, timeline, length, clipRects);
    }

    private void DrawProjectileClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Projectile Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty clipsProperty = timelineObject.FindProperty("projectileClips");
        for (int i = 0; i < clipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Projectile {i + 1}" : displayNameProperty.stringValue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                clipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(clipProperty.FindPropertyRelative("startTime"), clipProperty.FindPropertyRelative("duration"), timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("projectilePrefab"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("releaseMode"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("spawnOffset"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("fixedDirection"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("speed"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("lifetime"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Projectile Clip"))
        {
            int index = clipsProperty.arraySize;
            clipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = clipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Projectile {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.1f;
            newClip.FindPropertyRelative("speed").floatValue = 10f;
            newClip.FindPropertyRelative("lifetime").floatValue = 3f;
        }
        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();
        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
            SceneView.RepaintAll();
        }
    }

    private void DrawVfxClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("VFX Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty clipsProperty = timelineObject.FindProperty("vfxClips");
        for (int i = 0; i < clipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"VFX {i + 1}" : displayNameProperty.stringValue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                clipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(clipProperty.FindPropertyRelative("startTime"), clipProperty.FindPropertyRelative("duration"), timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("effectPrefab"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("spawnSpace"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("socketName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("localOffset"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add VFX Clip"))
        {
            int index = clipsProperty.arraySize;
            clipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = clipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"VFX {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.25f;
        }
        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();
        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
            SceneView.RepaintAll();
        }
    }

    private void DrawSfxClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("SFX Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty clipsProperty = timelineObject.FindProperty("sfxClips");
        for (int i = 0; i < clipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"SFX {i + 1}" : displayNameProperty.stringValue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                clipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(clipProperty.FindPropertyRelative("startTime"), clipProperty.FindPropertyRelative("duration"), timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("audioClip"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("volume"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("spatial"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add SFX Clip"))
        {
            int index = clipsProperty.arraySize;
            clipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = clipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"SFX {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
        }
        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();
        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
        }
    }

    private void DrawCameraClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Camera Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty clipsProperty = timelineObject.FindProperty("cameraClips");
        for (int i = 0; i < clipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Camera {i + 1}" : displayNameProperty.stringValue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                clipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(clipProperty.FindPropertyRelative("startTime"), clipProperty.FindPropertyRelative("duration"), timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("amplitude"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("frequency"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Camera Clip"))
        {
            int index = clipsProperty.arraySize;
            clipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = clipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Camera {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.15f;
            newClip.FindPropertyRelative("amplitude").floatValue = 1f;
            newClip.FindPropertyRelative("frequency").floatValue = 20f;
        }
        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();
        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
            SceneView.RepaintAll();
        }
    }

    private void DrawHitClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Hit Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty hitClipsProperty = timelineObject.FindProperty("hitClips");

        for (int i = 0; i < hitClipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = hitClipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Hit Clip {i + 1}" : displayNameProperty.stringValue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                hitClipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                SceneView.RepaintAll();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(
                clipProperty.FindPropertyRelative("startTime"),
                clipProperty.FindPropertyRelative("duration"),
                timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("hitShape"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("offset"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("size"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("targetLayers"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("allowRepeatHitsOnSameTarget"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("isContinuous"));
            if (clipProperty.FindPropertyRelative("isContinuous").boolValue)
            {
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("tickInterval"));
            }
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("damageMultiplier"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("poiseDamage"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Hit Feedback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("hitStopDuration"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("hitStopScale"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("cameraShakeAmplitude"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("hitSfx"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("hitVfxPrefab"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("knockbackDistance"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("knockbackUpwardDistance"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Hit Clip"))
        {
            int index = hitClipsProperty.arraySize;
            hitClipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = hitClipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Hit {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.1f;
            newClip.FindPropertyRelative("size").vector3Value = Vector3.one;
        }

        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();

        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
            SceneView.RepaintAll();
        }
    }

    private void DrawDerivationClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Derivation Window Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty derivationClipsProperty = timelineObject.FindProperty("derivationWindowClips");

        for (int i = 0; i < derivationClipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = derivationClipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Derivation {i + 1}" : displayNameProperty.stringValue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                derivationClipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(
                clipProperty.FindPropertyRelative("startTime"),
                clipProperty.FindPropertyRelative("duration"),
                timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("requiresHitConfirm"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("requiresGrounded"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("requiresAerial"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("nextSkills"), true);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Derivation Window"))
        {
            int index = derivationClipsProperty.arraySize;
            derivationClipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = derivationClipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Derivation {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.2f;
        }

        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();

        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
        }
    }

    private void DrawMovementClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Movement Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty movementClipsProperty = timelineObject.FindProperty("movementClips");

        for (int i = 0; i < movementClipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = movementClipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Movement {i + 1}" : displayNameProperty.stringValue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                movementClipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(
                clipProperty.FindPropertyRelative("startTime"),
                clipProperty.FindPropertyRelative("duration"),
                timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("motionSource"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("mirrorByFacing"));

            SerializedProperty motionSourceProperty = clipProperty.FindPropertyRelative("motionSource");
            if (motionSourceProperty.enumValueIndex == (int)SkillMotionSource.AnimatorCurve)
            {
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("curveXName"));
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("curveYName"));
            }
            else if (motionSourceProperty.enumValueIndex == (int)SkillMotionSource.CustomCurve)
            {
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("customCurveX"));
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("customCurveY"));
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Movement Clip"))
        {
            int index = movementClipsProperty.arraySize;
            movementClipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = movementClipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Movement {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.2f;
            newClip.FindPropertyRelative("motionSource").enumValueIndex = (int)SkillMotionSource.AnimatorCurve;
            newClip.FindPropertyRelative("mirrorByFacing").boolValue = true;
            newClip.FindPropertyRelative("curveXName").stringValue = "MoveX";
            newClip.FindPropertyRelative("curveYName").stringValue = "MoveY";
        }

        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();

        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
            SceneView.RepaintAll();
        }
    }

    private void DrawCancelClipEditor(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Cancel Window Clips", EditorStyles.boldLabel);

        SerializedObject timelineObject = new SerializedObject(timeline);
        timelineObject.Update();
        SerializedProperty cancelClipsProperty = timelineObject.FindProperty("cancelWindowClips");

        for (int i = 0; i < cancelClipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = cancelClipsProperty.GetArrayElementAtIndex(i);
            SerializedProperty displayNameProperty = clipProperty.FindPropertyRelative("displayName");
            string header = string.IsNullOrWhiteSpace(displayNameProperty.stringValue) ? $"Cancel {i + 1}" : displayNameProperty.stringValue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                cancelClipsProperty.DeleteArrayElementAtIndex(i);
                timelineObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(timeline);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("enabled"));
            DrawTimeFields(
                clipProperty.FindPropertyRelative("startTime"),
                clipProperty.FindPropertyRelative("duration"),
                timeline.length);
            EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("cancelPermission"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Cancel Window"))
        {
            int index = cancelClipsProperty.arraySize;
            cancelClipsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newClip = cancelClipsProperty.GetArrayElementAtIndex(index);
            newClip.FindPropertyRelative("displayName").stringValue = $"Cancel {index + 1}";
            newClip.FindPropertyRelative("enabled").boolValue = true;
            newClip.FindPropertyRelative("duration").floatValue = 0.15f;
            newClip.FindPropertyRelative("cancelPermission").enumValueIndex = 3;
        }

        if (GUILayout.Button("Ping Timeline"))
        {
            EditorGUIUtility.PingObject(timeline);
        }
        EditorGUILayout.EndHorizontal();

        timelineObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(timeline);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedSkill == null || _selectedSkill.timeline == null || _previewAnchor == null)
        {
            return;
        }

        if (_drawPreviewHitboxes)
        {
            List<HitSkillClip> activeClips = GetActiveHitClipsAtTime(_selectedSkill.timeline, _previewTime);
            for (int i = 0; i < activeClips.Count; i++)
            {
                DrawHitClip(_previewAnchor, activeClips[i]);
            }
        }

        if (_drawPreviewProjectiles)
        {
            DrawProjectilePreview(_selectedSkill.timeline, _previewTime);
        }

        if (_drawPreviewVfx)
        {
            DrawVfxPreview(_selectedSkill.timeline, _previewTime);
        }

        if (_drawPreviewCamera)
        {
            DrawCameraPreview(_selectedSkill.timeline, _previewTime, sceneView);
        }
    }

    private void RefreshSkills()
    {
        _skills.Clear();
        string[] guids = AssetDatabase.FindAssets("t:CombatSkillDefinitionSO");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CombatSkillDefinitionSO skill = AssetDatabase.LoadAssetAtPath<CombatSkillDefinitionSO>(path);
            if (skill != null)
            {
                _skills.Add(skill);
            }
        }
    }

    private void SelectSkill(CombatSkillDefinitionSO skill)
    {
        StopPreviewPlayback();
        ClearScenePreviewObjects();
        StopPreviewAudio();
        _selectedSkill = skill;
        if (_selectedSkill != null)
        {
            _previewTime = Mathf.Clamp(_previewTime, 0f, _selectedSkill.ResolveDuration());
            if (_selectedSkill.previewAnimationClip != null)
            {
                _previewFrame = Mathf.RoundToInt(_previewTime * _selectedSkill.ResolvePreviewFrameRate());
            }
        }
        SamplePreviewAnimation();
        Repaint();
        SceneView.RepaintAll();
    }

    private bool MatchesSearch(CombatSkillDefinitionSO skill)
    {
        if (skill == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        string query = _searchText.ToLowerInvariant();
        return skill.DisplayName.ToLowerInvariant().Contains(query)
            || skill.ResolveSkillId().ToLowerInvariant().Contains(query);
    }

    private void OnSelectionChanged()
    {
        SamplePreviewAnimation();
        Repaint();
    }

    private void OnEditorUpdate()
    {
        if (!_isPreviewPlaying || _selectedSkill == null || _selectedSkill.timeline == null)
        {
            return;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = _lastPreviewEditorTime <= 0d ? 0f : (float)(currentTime - _lastPreviewEditorTime);
        _lastPreviewEditorTime = currentTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        float nextTime = _previewTime + (deltaTime * _previewPlaybackSpeed);
        float maxTime = Mathf.Max(0.01f, _selectedSkill.timeline.length);
        if (nextTime >= maxTime)
        {
            if (_loopPreview)
            {
                ResetTriggeredPreviewEvents();
                nextTime %= maxTime;
            }
            else
            {
                nextTime = maxTime;
                PausePreviewPlayback();
            }
        }

        SetPreviewTime(nextTime);
        UpdatePreviewEvents(_selectedSkill.timeline);
    }

    private void AutoBindFromSelection()
    {
        AutoBindSkillFromSelection();
        AutoBindAnchorFromSelection();
    }

    private void AutoBindSkillFromSelection()
    {
        if (Selection.activeObject is CombatSkillDefinitionSO skill)
        {
            SelectSkill(skill);
        }
    }

    private void AutoBindAnchorFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            return;
        }

        CombatSkillPreviewAnchor anchor = Selection.activeGameObject.GetComponentInParent<CombatSkillPreviewAnchor>();
        if (anchor != null)
        {
            _previewAnchor = anchor;
            SceneView.RepaintAll();
        }
    }

    private void CreateTimelineAsset(SerializedObject skillObject, SerializedProperty timelineProperty)
    {
        string skillPath = AssetDatabase.GetAssetPath(_selectedSkill);
        string directory = string.IsNullOrWhiteSpace(skillPath) ? "Assets" : System.IO.Path.GetDirectoryName(skillPath);
        string defaultName = $"{_selectedSkill.ResolveSkillId()}_Timeline.asset";
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(directory, defaultName));

        SkillTimelineSO timeline = CreateInstance<SkillTimelineSO>();
        timeline.length = Mathf.Max(0.01f, _selectedSkill.ResolveDuration());
        AssetDatabase.CreateAsset(timeline, assetPath);
        AssetDatabase.SaveAssets();

        timelineProperty.objectReferenceValue = timeline;
        skillObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_selectedSkill);
        AssetDatabase.Refresh();
        SelectSkill(_selectedSkill);
    }

    private static List<HitSkillClip> GetActiveHitClipsAtTime(SkillTimelineSO timeline, float previewTime)
    {
        List<HitSkillClip> results = new List<HitSkillClip>();
        if (timeline == null || timeline.hitClips == null)
        {
            return results;
        }

        for (int i = 0; i < timeline.hitClips.Count; i++)
        {
            HitSkillClip clip = timeline.hitClips[i];
            if (clip == null || !clip.enabled)
            {
                continue;
            }

            float endTime = clip.duration > 0f ? clip.startTime + clip.duration : clip.startTime;
            if (previewTime >= clip.startTime && previewTime <= endTime)
            {
                results.Add(clip);
            }
        }

        return results;
    }

    private static List<VfxSkillClip> GetActiveVfxClipsAtTime(SkillTimelineSO timeline, float previewTime)
    {
        List<VfxSkillClip> results = new List<VfxSkillClip>();
        if (timeline == null || timeline.vfxClips == null)
        {
            return results;
        }

        for (int i = 0; i < timeline.vfxClips.Count; i++)
        {
            VfxSkillClip clip = timeline.vfxClips[i];
            if (clip == null || !clip.enabled)
            {
                continue;
            }

            float endTime = clip.duration > 0f ? clip.startTime + clip.duration : clip.startTime;
            if (previewTime >= clip.startTime && previewTime <= endTime)
            {
                results.Add(clip);
            }
        }

        return results;
    }

    private static List<ProjectileSkillClip> GetActiveProjectileClipsAtTime(SkillTimelineSO timeline, float previewTime)
    {
        List<ProjectileSkillClip> results = new List<ProjectileSkillClip>();
        if (timeline == null || timeline.projectileClips == null)
        {
            return results;
        }

        for (int i = 0; i < timeline.projectileClips.Count; i++)
        {
            ProjectileSkillClip clip = timeline.projectileClips[i];
            if (clip == null || !clip.enabled)
            {
                continue;
            }

            float endTime = clip.startTime + Mathf.Max(clip.duration, clip.lifetime, 0.05f);
            if (previewTime >= clip.startTime && previewTime <= endTime)
            {
                results.Add(clip);
            }
        }

        return results;
    }

    private static void DrawHitClip(CombatSkillPreviewAnchor anchor, HitSkillClip clip)
    {
        if (anchor == null || clip == null || anchor.PreviewOrigin == null)
        {
            return;
        }

        Vector3 center = ResolveHitCenter(anchor, clip);
        Handles.color = PreviewFill;
        switch (clip.hitShape)
        {
            case SkillHitShape.Sphere:
                float sphereRadius = Mathf.Max(0.01f, clip.size.x * 0.5f);
                Handles.SphereHandleCap(0, center, Quaternion.identity, sphereRadius * 2f, EventType.Repaint);
                Handles.color = PreviewWire;
                Handles.DrawWireDisc(center, Vector3.forward, sphereRadius);
                break;
            case SkillHitShape.Capsule:
                DrawCapsule(center, clip.size);
                break;
            default:
                Handles.DrawSolidRectangleWithOutline(BuildRectangle(center, clip.size), PreviewFill, PreviewWire);
                break;
        }
    }

    private static void DrawCapsule(Vector3 center, Vector3 size)
    {
        float radius = Mathf.Max(0.01f, size.x * 0.5f);
        float cylinderHeight = Mathf.Max(0f, size.y - (radius * 2f));
        Vector3 top = center + Vector3.up * (cylinderHeight * 0.5f);
        Vector3 bottom = center + Vector3.down * (cylinderHeight * 0.5f);

        Handles.color = PreviewFill;
        Handles.SphereHandleCap(0, top, Quaternion.identity, radius * 2f, EventType.Repaint);
        Handles.SphereHandleCap(0, bottom, Quaternion.identity, radius * 2f, EventType.Repaint);
        Handles.color = PreviewWire;
        Handles.DrawWireDisc(top, Vector3.forward, radius);
        Handles.DrawWireDisc(bottom, Vector3.forward, radius);
        Vector3 right = Vector3.right * radius;
        Handles.DrawLine(top + right, bottom + right);
        Handles.DrawLine(top - right, bottom - right);
    }

    private static Vector3 ResolveHitCenter(CombatSkillPreviewAnchor anchor, HitSkillClip clip)
    {
        Vector3 offset = clip.offset;
        if (anchor.MirrorByFacing && anchor.IsFacingLeft())
        {
            offset.x = -offset.x;
        }

        return anchor.PreviewOrigin.position + offset;
    }

    private void DrawProjectilePreview(SkillTimelineSO timeline, float previewTime)
    {
        List<ProjectileSkillClip> activeClips = GetActiveProjectileClipsAtTime(timeline, previewTime);
        for (int i = 0; i < activeClips.Count; i++)
        {
            ProjectileSkillClip clip = activeClips[i];
            Vector3 start = ResolvePreviewPosition(clip.spawnOffset);
            Vector3 direction = ResolveProjectileDirection(clip);
            float travelTime = Mathf.Max(0f, previewTime - clip.startTime);
            float distance = clip.speed * travelTime;
            Vector3 current = start + direction * distance;

            Handles.color = PreviewProjectileColor;
            Handles.DrawLine(start, current);
            Handles.SphereHandleCap(0, current, Quaternion.identity, 0.18f, EventType.Repaint);
            Handles.Label(current + Vector3.up * 0.2f, string.IsNullOrWhiteSpace(clip.displayName) ? "Projectile" : clip.displayName);
        }
    }

    private void DrawVfxPreview(SkillTimelineSO timeline, float previewTime)
    {
        List<VfxSkillClip> activeClips = GetActiveVfxClipsAtTime(timeline, previewTime);
        for (int i = 0; i < activeClips.Count; i++)
        {
            VfxSkillClip clip = activeClips[i];
            Vector3 position = ResolveVfxPosition(clip);
            Handles.color = PreviewVfxColor;
            Handles.DrawWireDisc(position, Vector3.forward, 0.24f);
            Handles.DrawSolidDisc(position, Vector3.forward, 0.08f);
            Handles.Label(position + Vector3.up * 0.2f, string.IsNullOrWhiteSpace(clip.displayName) ? "VFX" : clip.displayName);
        }
    }

    private void DrawCameraPreview(SkillTimelineSO timeline, float previewTime, SceneView sceneView)
    {
        if (timeline == null || timeline.cameraClips == null)
        {
            return;
        }

        for (int i = 0; i < timeline.cameraClips.Count; i++)
        {
            CameraShakeSkillClip clip = timeline.cameraClips[i];
            if (clip == null || !clip.enabled)
            {
                continue;
            }

            float endTime = clip.duration > 0f ? clip.startTime + clip.duration : clip.startTime;
            if (previewTime < clip.startTime || previewTime > endTime)
            {
                continue;
            }

            Handles.BeginGUI();
            Rect area = new Rect(16f, sceneView.position.height - 66f, 320f, 42f);
            GUI.color = PreviewCameraColor;
            GUI.Box(area, $"Camera Preview  Amp:{clip.amplitude:0.##}  Freq:{clip.frequency:0.##}");
            GUI.color = Color.white;
            Handles.EndGUI();
        }
    }

    private Vector3 ResolvePreviewPosition(Vector3 localOffset)
    {
        if (_previewAnchor == null || _previewAnchor.PreviewOrigin == null)
        {
            return localOffset;
        }

        Vector3 offset = localOffset;
        if (_previewAnchor.MirrorByFacing && _previewAnchor.IsFacingLeft())
        {
            offset.x = -offset.x;
        }

        return _previewAnchor.PreviewOrigin.position + offset;
    }

    private Vector3 ResolveVfxPosition(VfxSkillClip clip)
    {
        if (_previewAnchor == null || _previewAnchor.PreviewOrigin == null || clip == null)
        {
            return clip != null ? clip.localOffset : Vector3.zero;
        }

        if (clip.spawnSpace == SkillVfxSpawnSpace.World)
        {
            return clip.localOffset;
        }

        Transform socket = ResolveSocketTransform(clip.socketName);
        if (socket != null)
        {
            return socket.position + clip.localOffset;
        }

        return ResolvePreviewPosition(clip.localOffset);
    }

    private Transform ResolveSocketTransform(string socketName)
    {
        if (_previewAnchor == null || _previewAnchor.PreviewTarget == null || string.IsNullOrWhiteSpace(socketName))
        {
            return null;
        }

        Transform[] children = _previewAnchor.PreviewTarget.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == socketName)
            {
                return children[i];
            }
        }

        return null;
    }

    private Vector3 ResolveProjectileDirection(ProjectileSkillClip clip)
    {
        if (clip == null)
        {
            return Vector3.right;
        }

        Vector3 direction;
        switch (clip.releaseMode)
        {
            case ProjectileReleaseMode.FixedDirection:
                direction = clip.fixedDirection;
                break;
            default:
                direction = _previewAnchor != null && _previewAnchor.IsFacingLeft() ? Vector3.left : Vector3.right;
                break;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }

        return direction.normalized;
    }

    private static Vector3[] BuildRectangle(Vector3 center, Vector3 size)
    {
        Vector3 half = size * 0.5f;
        return new[]
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f),
        };
    }

    private void HandleTimelineDragging(Rect timelineRect, SkillTimelineSO timeline, float length, List<TimelineClipRect> clipRects)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            for (int i = clipRects.Count - 1; i >= 0; i--)
            {
                TimelineClipRect clipRect = clipRects[i];
                if (!clipRect.rect.Contains(currentEvent.mousePosition))
                {
                    continue;
                }

                float startTime = GetClipStartTime(timeline, clipRect.kind, clipRect.index);
                _timelineDrag = new TimelineDragState
                {
                    isDragging = true,
                    kind = clipRect.kind,
                    index = clipRect.index,
                    startMouseX = currentEvent.mousePosition.x,
                    startClipTime = startTime,
                };
                currentEvent.Use();
                return;
            }
        }

        if (_timelineDrag.isDragging && currentEvent.type == EventType.MouseDrag)
        {
            float deltaPixels = currentEvent.mousePosition.x - _timelineDrag.startMouseX;
            float deltaTime = (deltaPixels / Mathf.Max(1f, timelineRect.width)) * length;
            float newStartTime = Mathf.Clamp(_timelineDrag.startClipTime + deltaTime, 0f, length);
            if (_snapToFrames)
            {
                newStartTime = SnapTimeToFrame(newStartTime);
            }
            SetClipStartTime(timeline, _timelineDrag.kind, _timelineDrag.index, newStartTime);
            EditorUtility.SetDirty(timeline);
            Repaint();
            SceneView.RepaintAll();
            currentEvent.Use();
            return;
        }

        if (_timelineDrag.isDragging && (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp))
        {
            _timelineDrag = default;
            currentEvent.Use();
        }
    }

    private float GetClipStartTime(SkillTimelineSO timeline, TimelineClipKind kind, int index)
    {
        switch (kind)
        {
            case TimelineClipKind.Hit:
                return timeline.hitClips[index].startTime;
            case TimelineClipKind.Projectile:
                return timeline.projectileClips[index].startTime;
            case TimelineClipKind.Movement:
                return timeline.movementClips[index].startTime;
            case TimelineClipKind.Vfx:
                return timeline.vfxClips[index].startTime;
            case TimelineClipKind.Sfx:
                return timeline.sfxClips[index].startTime;
            case TimelineClipKind.Camera:
                return timeline.cameraClips[index].startTime;
            case TimelineClipKind.Derivation:
                return timeline.derivationWindowClips[index].startTime;
            case TimelineClipKind.Cancel:
                return timeline.cancelWindowClips[index].startTime;
            default:
                return 0f;
        }
    }

    private void SetClipStartTime(SkillTimelineSO timeline, TimelineClipKind kind, int index, float value)
    {
        switch (kind)
        {
            case TimelineClipKind.Hit:
                timeline.hitClips[index].startTime = value;
                break;
            case TimelineClipKind.Projectile:
                timeline.projectileClips[index].startTime = value;
                break;
            case TimelineClipKind.Movement:
                timeline.movementClips[index].startTime = value;
                break;
            case TimelineClipKind.Vfx:
                timeline.vfxClips[index].startTime = value;
                break;
            case TimelineClipKind.Sfx:
                timeline.sfxClips[index].startTime = value;
                break;
            case TimelineClipKind.Camera:
                timeline.cameraClips[index].startTime = value;
                break;
            case TimelineClipKind.Derivation:
                timeline.derivationWindowClips[index].startTime = value;
                break;
            case TimelineClipKind.Cancel:
                timeline.cancelWindowClips[index].startTime = value;
                break;
        }
    }

    private readonly struct TimelineClipRect
    {
        public readonly TimelineClipKind kind;
        public readonly int index;
        public readonly Rect rect;

        public TimelineClipRect(TimelineClipKind kind, int index, Rect rect)
        {
            this.kind = kind;
            this.index = index;
            this.rect = rect;
        }
    }

    private void DrawTimeFields(SerializedProperty startTimeProperty, SerializedProperty durationProperty, float maxLength)
    {
        float fps = GetPreviewFrameRate();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(startTimeProperty, new GUIContent("Start Time"));
        int startFrame = Mathf.RoundToInt(startTimeProperty.floatValue * fps);
        int newStartFrame = EditorGUILayout.IntField("Frame", startFrame, GUILayout.Width(120f));
        if (newStartFrame != startFrame)
        {
            startTimeProperty.floatValue = Mathf.Clamp(newStartFrame / fps, 0f, maxLength);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(durationProperty, new GUIContent("Duration"));
        int durationFrame = Mathf.RoundToInt(durationProperty.floatValue * fps);
        int newDurationFrame = EditorGUILayout.IntField("Frames", durationFrame, GUILayout.Width(120f));
        if (newDurationFrame != durationFrame)
        {
            durationProperty.floatValue = Mathf.Max(0f, newDurationFrame / fps);
        }
        EditorGUILayout.EndHorizontal();
    }

    private float GetPreviewFrameRate()
    {
        return _selectedSkill != null ? _selectedSkill.ResolvePreviewFrameRate() : 60f;
    }

    private float SnapTimeToFrame(float time)
    {
        float fps = GetPreviewFrameRate();
        return Mathf.Round(time * fps) / fps;
    }

    private void DrawFrameGrid(Rect rect, float length, float fps)
    {
        if (fps <= 0f || length <= 0f)
        {
            return;
        }

        int frameCount = Mathf.Max(1, Mathf.RoundToInt(length * fps));
        Color major = new Color(1f, 1f, 1f, 0.12f);
        Color minor = new Color(1f, 1f, 1f, 0.05f);

        for (int frame = 0; frame <= frameCount; frame++)
        {
            float t = frame / fps;
            float x = rect.x + (t / length) * rect.width;
            bool isMajor = frame == 0 || frame % Mathf.RoundToInt(fps) == 0;
            EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), isMajor ? major : minor);

            if (isMajor)
            {
                GUI.Label(new Rect(x + 2f, rect.y + 2f, 40f, 14f), frame.ToString(), EditorStyles.miniLabel);
            }
        }
    }

    private void SetPreviewTime(float newPreviewTime, bool updateFrameFromTime = true)
    {
        _previewTime = newPreviewTime;
        if (updateFrameFromTime && _selectedSkill != null && _selectedSkill.previewAnimationClip != null)
        {
            float fps = _selectedSkill.ResolvePreviewFrameRate();
            _previewFrame = Mathf.RoundToInt(_previewTime * fps);
        }

        SamplePreviewAnimation();
        UpdatePreviewEvents(_selectedSkill != null ? _selectedSkill.timeline : null);
        SceneView.RepaintAll();
        Repaint();
    }

    private void SamplePreviewAnimation()
    {
        if (_selectedSkill == null || _selectedSkill.previewAnimationClip == null || _previewAnchor == null || _previewAnchor.PreviewTarget == null)
        {
            StopAnimationPreview();
            return;
        }

        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
        }

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(
            _previewAnchor.PreviewTarget,
            _selectedSkill.previewAnimationClip,
            Mathf.Clamp(_previewTime, 0f, _selectedSkill.previewAnimationClip.length));
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }

    private void StopAnimationPreview()
    {
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }
    }

    private void StartPreviewPlayback()
    {
        _isPreviewPlaying = true;
        _lastPreviewEditorTime = EditorApplication.timeSinceStartup;
        ResetTriggeredPreviewEvents();
        UpdatePreviewEvents(_selectedSkill != null ? _selectedSkill.timeline : null);
    }

    private void PausePreviewPlayback()
    {
        _isPreviewPlaying = false;
        _lastPreviewEditorTime = 0d;
    }

    private void StopPreviewPlayback()
    {
        PausePreviewPlayback();
        ResetTriggeredPreviewEvents();
    }

    private void ResetTriggeredPreviewEvents()
    {
        PlayedPreviewSfx.Clear();
        ClearScenePreviewObjects();
        StopPreviewAudio();
    }

    private void UpdatePreviewEvents(SkillTimelineSO timeline)
    {
        UpdatePreviewVfx(timeline);
        UpdatePreviewProjectiles(timeline);
        UpdatePreviewSfx(timeline);
    }

    private void UpdatePreviewVfx(SkillTimelineSO timeline)
    {
        if (timeline == null || timeline.vfxClips == null)
        {
            ClearPreviewObjects(ActivePreviewVfx);
            return;
        }

        HashSet<string> activeKeys = new HashSet<string>();
        for (int i = 0; i < timeline.vfxClips.Count; i++)
        {
            VfxSkillClip clip = timeline.vfxClips[i];
            if (clip == null || !clip.enabled || clip.effectPrefab == null)
            {
                continue;
            }

            float endTime = clip.duration > 0f ? clip.startTime + clip.duration : clip.startTime + 0.05f;
            if (_previewTime < clip.startTime || _previewTime > endTime)
            {
                continue;
            }

            string key = "VFX_" + i;
            activeKeys.Add(key);
            if (!ActivePreviewVfx.TryGetValue(key, out GameObject previewObject) || previewObject == null)
            {
                previewObject = InstantiatePreviewObject(clip.effectPrefab, key);
                ActivePreviewVfx[key] = previewObject;
            }

            if (previewObject != null)
            {
                previewObject.transform.position = ResolveVfxPosition(clip);
            }
        }

        RemoveInactivePreviewObjects(ActivePreviewVfx, activeKeys);
    }

    private void UpdatePreviewProjectiles(SkillTimelineSO timeline)
    {
        if (timeline == null || timeline.projectileClips == null)
        {
            ClearPreviewObjects(ActivePreviewProjectiles);
            return;
        }

        HashSet<string> activeKeys = new HashSet<string>();
        for (int i = 0; i < timeline.projectileClips.Count; i++)
        {
            ProjectileSkillClip clip = timeline.projectileClips[i];
            if (clip == null || !clip.enabled || clip.projectilePrefab == null)
            {
                continue;
            }

            float endTime = clip.startTime + Mathf.Max(clip.duration, clip.lifetime, 0.05f);
            if (_previewTime < clip.startTime || _previewTime > endTime)
            {
                continue;
            }

            string key = "Projectile_" + i;
            activeKeys.Add(key);
            if (!ActivePreviewProjectiles.TryGetValue(key, out GameObject previewObject) || previewObject == null)
            {
                previewObject = InstantiatePreviewObject(clip.projectilePrefab, key);
                ActivePreviewProjectiles[key] = previewObject;
            }

            if (previewObject != null)
            {
                Vector3 start = ResolvePreviewPosition(clip.spawnOffset);
                Vector3 current = start + ResolveProjectileDirection(clip) * (clip.speed * Mathf.Max(0f, _previewTime - clip.startTime));
                previewObject.transform.position = current;
            }
        }

        RemoveInactivePreviewObjects(ActivePreviewProjectiles, activeKeys);
    }

    private void UpdatePreviewSfx(SkillTimelineSO timeline)
    {
        if (!_isPreviewPlaying || timeline == null || timeline.sfxClips == null)
        {
            return;
        }

        for (int i = 0; i < timeline.sfxClips.Count; i++)
        {
            SfxSkillClip clip = timeline.sfxClips[i];
            if (clip == null || !clip.enabled || clip.audioClip == null)
            {
                continue;
            }

            string key = "SFX_" + i;
            if (_previewTime >= clip.startTime && !PlayedPreviewSfx.Contains(key))
            {
                PlayedPreviewSfx.Add(key);
                PlayPreviewAudio(clip.audioClip);
            }
        }
    }

    private static GameObject InstantiatePreviewObject(GameObject prefab, string name)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject previewObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (previewObject == null)
        {
            previewObject = UnityEngine.Object.Instantiate(prefab);
        }

        if (previewObject != null)
        {
            previewObject.name = "[Preview] " + name;
            previewObject.hideFlags = HideFlags.HideAndDontSave;
        }

        return previewObject;
    }

    private static void RemoveInactivePreviewObjects(Dictionary<string, GameObject> objects, HashSet<string> activeKeys)
    {
        List<string> staleKeys = new List<string>();
        foreach (KeyValuePair<string, GameObject> pair in objects)
        {
            if (!activeKeys.Contains(pair.Key))
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(pair.Value);
                }

                staleKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            objects.Remove(staleKeys[i]);
        }
    }

    private static void ClearPreviewObjects(Dictionary<string, GameObject> objects)
    {
        foreach (KeyValuePair<string, GameObject> pair in objects)
        {
            if (pair.Value != null)
            {
                UnityEngine.Object.DestroyImmediate(pair.Value);
            }
        }

        objects.Clear();
    }

    private static void ClearScenePreviewObjects()
    {
        ClearPreviewObjects(ActivePreviewVfx);
        ClearPreviewObjects(ActivePreviewProjectiles);
    }

    private static void PlayPreviewAudio(AudioClip clip)
    {
        if (clip == null || PlayPreviewClipMethod == null)
        {
            return;
        }

        ParameterInfo[] parameters = PlayPreviewClipMethod.GetParameters();
        object[] args = parameters.Length == 3
            ? new object[] { clip, 0, false }
            : new object[] { clip, 0, 0, false };
        PlayPreviewClipMethod.Invoke(null, args);
    }

    private static void StopPreviewAudio()
    {
        if (StopAllPreviewClipsMethod != null)
        {
            StopAllPreviewClipsMethod.Invoke(null, null);
        }
    }
}
