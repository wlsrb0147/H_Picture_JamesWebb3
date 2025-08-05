using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Debug = DebugEx;

public class Other : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void DisableWithFade()
    {
        _canvasGroup.DOFade(0, 1f).OnComplete(()=>gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        VideoManager.Instance.PrePareEndingVideo();
        _canvasGroup.alpha = 1;
    }
}
