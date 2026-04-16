using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class RaySampleManager : MonoBehaviour {
    public bool IsExporting;

    public RaySample raySample;
    public InputField roleInput;
    public InputField animInput;
    public InputField frameInput;
    public InputField totalFrameInput;
    public Slider progress;
    public Transform modelRoot;
    public Dropdown dropDown;

    // === Added by Assistant: normals toggle ===
    public bool ExportNormalsAtlas = true;

    // === Added by Assistant: Atlas config ===
    public int AtlasColumns = 8;
    public bool AtlasStartFromTop = false;
    public bool FixImporterForExports = true;
    public bool UseOverrideResolution = false;
    public int OverrideWidth = 512;
    public int OverrideHeight = 512;


    private ChildAnimatorState curState;
    private const float FrameRate = 240;

    public void OnClickExportCurrent()
    {
        raySample.ExportCurrent(roleInput.text, animInput.text + "-" + frameInput.text);
    }

    public void OnProgressBarChanged()
    {
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        animator.speed = 0f;
        animator.ForceStateNormalizedTime(progress.value * ((AnimationClip)curState.state.motion).length);
    }

    public void OnClickPlay()
    {
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        animator.speed = 1f;
        animator.Play(curState.state.name, -1, 0);
    }

    public void OnClickRefresh()
    {
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        AnimatorController animationController = animator.runtimeAnimatorController as AnimatorController;
        AnimatorStateMachine stateMachine = animationController.layers[0].stateMachine;
        dropDown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var s in stateMachine.states)
        {
            options.Add(s.state.name);
        }
        dropDown.AddOptions(options);
        OnDropDownChanged();
    }

    public void OnDropDownChanged()
    {
        string value = dropDown.options[dropDown.value].text;
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        AnimatorController animationController = animator.runtimeAnimatorController as AnimatorController;
        AnimatorStateMachine stateMachine = animationController.layers[0].stateMachine;
        foreach (var s in stateMachine.states)
        {
            if (s.state.name == value)
            { 
                curState = s;
            return;
            }
        }
    }

    public void OnClickExportAnimation()
    {
        StartCoroutine(StartExport(int.Parse(totalFrameInput.text)));
    }

    IEnumerator StartExport(int totalFrames)
    {
        Debug.Log("Start export!");
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        animator.speed = 0f;
        var length = ((AnimationClip)curState.state.motion).length;
        for(int i = 0; i < totalFrames; i++)
        {
            animator.ForceStateNormalizedTime(i / (float)totalFrames);
            raySample.ExportCurrent(roleInput.text + "_auto", animInput.text + "-" + (i+1).ToString());
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Export complete!");
    }

    // === Added by Assistant: Export Atlas ===
    public void OnClickExportAtlas()
    {
        int total = int.Parse(totalFrameInput.text);
        StartCoroutine(StartExportAtlas(total));
    }

    IEnumerator StartExportAtlas(int totalFrames)
    {
        int prevW = raySample.SampleWidth;
        int prevH = raySample.SampleHeight;
        int prevSampleTime = raySample.SampleTime;
        if (UseOverrideResolution) { raySample.SampleWidth = OverrideWidth; raySample.SampleHeight = OverrideHeight; }
        raySample.SampleTime = 1;
        if (IsExporting) yield break;
        IsExporting = true;
        Debug.Log("Start atlas export!");
        Animator animator = modelRoot.GetComponentInChildren<Animator>();
        animator.speed = 0f;
        var frames = new System.Collections.Generic.List<Color[]>();
        var framesN = new System.Collections.Generic.List<Color[]>();
        for (int i = 0; i < totalFrames; i++)
        {
            float t = (totalFrames > 1) ? i / (float)(totalFrames - 1) : 0f;
            animator.ForceStateNormalizedTime(t);
            animator.Update(0f);
            var colors = raySample.CaptureFrameColors();
            frames.Add(colors);
            if (ExportNormalsAtlas) { var normals = raySample.CaptureFrameNormals(); framesN.Add(normals); }
            yield return new WaitForEndOfFrame();
        }
        int columns = (AtlasColumns > 0) ? AtlasColumns : totalFrames;
        Debug.Log($"Exporting atlas: colors={frames.Count}, normals={framesN.Count}");
        var colorPath = raySample.ExportAtlas(roleInput.text + "_auto", animInput.text, frames, columns, FixImporterForExports, AtlasStartFromTop);
        if (ExportNormalsAtlas) { var normalPath = raySample.ExportAtlasNormals(roleInput.text + "_auto", animInput.text, framesN, columns, FixImporterForExports, AtlasStartFromTop); }
        Debug.Log("Export atlas complete!");
        raySample.SampleTime = prevSampleTime;
        if (UseOverrideResolution) { raySample.SampleWidth = prevW; raySample.SampleHeight = prevH; }
        IsExporting = false;
    }

}
