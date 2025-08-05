using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Debug = DebugEx;

public class RotAndScale : MonoBehaviour
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    
    private float _rotSpeed;
    private float _scaleTime;
    private float _fadingTime;
    private float _enableDelay;
    
    private readonly Vector3 _scale = new (0, 0, 1);
    
    private void Awake()
    {
        _rectTransform =  GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _rotSpeed = JsonSaver.Instance.Settings.rotSpeed;
        _scaleTime = JsonSaver.Instance.Settings.scaleTime;
        _fadingTime =  JsonSaver.Instance.Settings.fadingTime;
        _enableDelay = JsonSaver.Instance.Settings.enableDelay;
    }

    private void OnEnable()
    {
        Invoke(nameof(StartEnable),_enableDelay);
    }

    private void StartEnable()
    {
        _rectTransform.DOScale(Vector3.one, _scaleTime);
        _canvasGroup.DOFade(1,_fadingTime);
    }

    private void Update()
    {
        transform.eulerAngles += new Vector3(0, 0, _rotSpeed * Time.deltaTime);
    }

    private void OnDisable()
    {
        _canvasGroup.alpha = 0;
        _rectTransform.localScale = _scale;
    }
}
