using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class CommonUIBase : MonoBehaviour, IPopupUI
{
    [Header("元设定")] [SerializeField] private bool hideOnStart = true;
    [SerializeField] protected RectTransform window;
    [SerializeField] protected float duration;
    [SerializeField] protected CanvasGroup cg;
    [SerializeField] protected Button closeBtn;
    protected PopupResult _popupResult;
    public PopupResult PopupResult => _popupResult;

    protected virtual void Awake()
    {
        closeBtn?.onClick.AddListener(HandleOnCloseBtnClick);
    }

    private void Start()
    {
        if (hideOnStart)
        {
            SetVisibility(false);
        }
    }

    public void SetVisibility(bool visible)
    {
        HandleVisibility(visible);
    }

    private void HandleVisibility(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        _popupResult = PopupResult.Unset;
        window.localScale = Vector3.zero;
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        window.DOScale(Vector3.one, duration).SetEase(Ease.OutSine).OnComplete(DoOnShow);
        cg.DOFade(1, duration).SetEase(Ease.OutSine);
    }

    private void Hide()
    {
        if (cg.alpha == 0)
        {
            return;
        }
        window.localScale = Vector3.one;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = 1;
        cg.DOFade(0, duration).SetEase(Ease.OutSine);
        window.DOScale(Vector3.zero, duration).SetEase(Ease.OutSine).OnComplete(DoOnHide);
    }

    protected virtual void HandleOnCloseBtnClick()
    {
        _popupResult = PopupResult.Cancel;
    }

    protected virtual void DoOnShow()
    {
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    protected virtual void DoOnHide()
    {
        _popupResult =  PopupResult.Unset;
        cg.alpha = 0;
    }
}
