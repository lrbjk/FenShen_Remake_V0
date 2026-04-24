using System.Collections.Generic;
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
    private bool _snapToFrames = true;
    private TimelineDragState _timelineDrag;
    private static readonly Color TimelineBackground = new Color(0.15f, 0.15f, 0.17f, 1f);
    private static readonly Color TimelineHitClip = new Color(1f, 0.45f, 0.2f, 0.9f);
    private static readonly Color TimelineDerivationClip = new Color(0.3f, 0.9f, 0.45f, 0.9f);
    private static readonly Color TimelineCancelClip = new Color(1f, 0.85f, 0.2f, 0.9f);
    private static readonly Color TimelineMovementClip = new Color(0.45f, 0.6f, 1f, 0.9f);
    private static readonly Color TimelinePlayhead = new Color(0.2f, 0.9f, 1f, 1f);
    private static readonly Color PreviewFill = new Color(0.15f, 0.85f, 1f, 0.18f);
    private static readonly Color PreviewWire = new Color(0.1f, 0.95f, 1f, 1f);

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
        AutoBindFromSelection();
    }

    void OnDisable()
    {
        StopAnimationPreview();
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
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
        DrawDerivationClipEditor(timeline);
        DrawCancelClipEditor(timeline);
        DrawMovementClipEditor(timeline);

        skillObject.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
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
        _snapToFrames = EditorGUILayout.Toggle("Snap To Frames", _snapToFrames);

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
    }

    private void DrawTimelineOverview(SkillTimelineSO timeline)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, 80f, GUILayout.ExpandWidth(true));
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
                Rect clipRect = new Rect(xMin, rect.y + 46f, width, 18f);
                EditorGUI.DrawRect(clipRect, TimelineMovementClip);
                GUI.Label(clipRect, string.IsNullOrWhiteSpace(clip.displayName) ? $"Move {i + 1}" : clip.displayName, EditorStyles.whiteMiniLabel);
                clipRects.Add(new TimelineClipRect(TimelineClipKind.Movement, i, clipRect));
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
                Rect clipRect = new Rect(xMin, rect.y + 18f + 24f, width, 18f);
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
                Rect clipRect = new Rect(xMin, rect.y + 66f, width, 12f);
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
        if (!_drawPreviewHitboxes || _selectedSkill == null || _selectedSkill.timeline == null || _previewAnchor == null)
        {
            return;
        }

        List<HitSkillClip> activeClips = GetActiveHitClipsAtTime(_selectedSkill.timeline, _previewTime);
        for (int i = 0; i < activeClips.Count; i++)
        {
            DrawHitClip(_previewAnchor, activeClips[i]);
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
            case TimelineClipKind.Movement:
                return timeline.movementClips[index].startTime;
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
            case TimelineClipKind.Movement:
                timeline.movementClips[index].startTime = value;
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
}
