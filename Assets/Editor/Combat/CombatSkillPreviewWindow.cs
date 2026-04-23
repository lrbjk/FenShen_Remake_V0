using System.Collections.Generic;
using FenShen.Combat;
using UnityEditor;
using UnityEngine;

public class CombatSkillPreviewWindow : EditorWindow
{
    private static CombatSkillDefinitionSO _previewSkill;
    private static CombatSkillPreviewAnchor _previewAnchor;
    private static float _previewTime;
    private static bool _drawHitboxes = true;
    private static readonly Color FillColor = new Color(0.15f, 0.85f, 1f, 0.18f);
    private static readonly Color WireColor = new Color(0.1f, 0.95f, 1f, 1f);

    [MenuItem("Window/Combat/Skill Preview")]
    public static void ShowWindow()
    {
        GetWindow<CombatSkillPreviewWindow>("Skill Preview");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += Repaint;
        AutoBindFromSelection();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= Repaint;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Preview Source", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _previewAnchor = (CombatSkillPreviewAnchor)EditorGUILayout.ObjectField("Anchor", _previewAnchor, typeof(CombatSkillPreviewAnchor), true);
        _previewSkill = (CombatSkillDefinitionSO)EditorGUILayout.ObjectField("Skill", _previewSkill, typeof(CombatSkillDefinitionSO), false);
        _drawHitboxes = EditorGUILayout.Toggle("Draw Hitboxes", _drawHitboxes);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        if (_previewSkill == null)
        {
            EditorGUILayout.HelpBox("Select a CombatSkillDefinitionSO to preview.", MessageType.Info);
            if (GUILayout.Button("Use Selected Skill"))
            {
                AutoBindFromSelection();
            }
            return;
        }

        float maxTime = Mathf.Max(0.01f, _previewSkill.ResolveDuration());
        EditorGUI.BeginChangeCheck();
        _previewTime = EditorGUILayout.Slider("Preview Time", Mathf.Clamp(_previewTime, 0f, maxTime), 0f, maxTime);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Skill", _previewSkill.DisplayName);
        EditorGUILayout.LabelField("Length", maxTime.ToString("0.000"));
        EditorGUILayout.LabelField("Active Hit Clips", GetActiveHitClipsAtTime(_previewSkill, _previewTime).Count.ToString());

        EditorGUILayout.Space(6f);
        if (GUILayout.Button("Use Selected Anchor"))
        {
            AutoBindAnchorFromSelection();
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_drawHitboxes || _previewSkill == null || _previewAnchor == null)
        {
            return;
        }

        List<HitSkillClip> activeClips = GetActiveHitClipsAtTime(_previewSkill, _previewTime);
        if (activeClips.Count == 0)
        {
            return;
        }

        for (int i = 0; i < activeClips.Count; i++)
        {
            DrawHitClip(_previewAnchor, activeClips[i]);
        }
    }

    private static void DrawHitClip(CombatSkillPreviewAnchor anchor, HitSkillClip clip)
    {
        if (anchor == null || clip == null || anchor.PreviewOrigin == null)
        {
            return;
        }

        Vector3 center = ResolveHitCenter(anchor, clip);
        Handles.color = FillColor;
        switch (clip.hitShape)
        {
            case SkillHitShape.Sphere:
                float sphereRadius = Mathf.Max(0.01f, clip.size.x * 0.5f);
                Handles.SphereHandleCap(0, center, Quaternion.identity, sphereRadius * 2f, EventType.Repaint);
                Handles.color = WireColor;
                Handles.DrawWireDisc(center, Vector3.forward, sphereRadius);
                break;
            case SkillHitShape.Capsule:
                DrawCapsule(anchor, center, clip.size);
                break;
            default:
                Vector3 size = clip.size;
                Handles.DrawSolidRectangleWithOutline(
                    BuildRectangle(center, size),
                    FillColor,
                    WireColor);
                break;
        }
    }

    private static void DrawCapsule(CombatSkillPreviewAnchor anchor, Vector3 center, Vector3 size)
    {
        float radius = Mathf.Max(0.01f, size.x * 0.5f);
        float cylinderHeight = Mathf.Max(0f, size.y - (radius * 2f));
        Vector3 top = center + Vector3.up * (cylinderHeight * 0.5f);
        Vector3 bottom = center + Vector3.down * (cylinderHeight * 0.5f);

        Handles.color = FillColor;
        Handles.SphereHandleCap(0, top, Quaternion.identity, radius * 2f, EventType.Repaint);
        Handles.SphereHandleCap(0, bottom, Quaternion.identity, radius * 2f, EventType.Repaint);

        Handles.color = WireColor;
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

    private static List<HitSkillClip> GetActiveHitClipsAtTime(CombatSkillDefinitionSO skill, float previewTime)
    {
        List<HitSkillClip> results = new List<HitSkillClip>();
        if (skill == null || skill.timeline == null || skill.timeline.hitClips == null)
        {
            return results;
        }

        for (int i = 0; i < skill.timeline.hitClips.Count; i++)
        {
            HitSkillClip clip = skill.timeline.hitClips[i];
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

    private static void AutoBindFromSelection()
    {
        Object activeObject = Selection.activeObject;
        if (activeObject is CombatSkillDefinitionSO selectedSkill)
        {
            _previewSkill = selectedSkill;
        }

        AutoBindAnchorFromSelection();
    }

    private static void AutoBindAnchorFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            return;
        }

        CombatSkillPreviewAnchor anchor = Selection.activeGameObject.GetComponentInParent<CombatSkillPreviewAnchor>();
        if (anchor != null)
        {
            _previewAnchor = anchor;
        }
    }
}
