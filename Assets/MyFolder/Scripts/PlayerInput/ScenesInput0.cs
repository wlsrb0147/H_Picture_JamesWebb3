using System;
using System.Collections;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ScenesInput0 : RegisterInputControl
{
    // 에디터에서 현재 입력 페이지를 보기위한 Serialize
    [SerializeField] private int currentPage;
    
    // 에디어테엇 현재 입력 인덱스를 보기위한 Serialize   
    [SerializeField] private int currentIndex;
    
    private bool _performed;

    [SerializeField] private Other other;
    [SerializeField] private Image circle;
    private int _selectionNum;

    private int SelectionNum
    {
        get => _selectionNum;
        set
        {
            AudioManager.Instance.PlayOneShotAudio(AudioName.StickHorizon);
            StartCoroutine(DelaySelect(value));
        }
    }

    [SerializeField] private float delay;
    private IEnumerator DelaySelect(int val)
    {
        if (val is >= 4 or < 0 )
        {
            yield break;
        }
        _selectionNum = val;
        
        yield return new WaitForSeconds(delay);
        
        circle.rectTransform.anchoredPosition = circlePos[_selectionNum];
    }
    
    private readonly Vector2[] circlePos = { new(-1505, 180), new(-580, -78), new(537, -279), new(1395, -15) };
    
    protected override void Start()
    {
        base.Start();
        SetCurrentInput(0,0);
    }
    
    public override void ChangeIndex()
    {
        base.ChangeIndex();
        SetCurrentInput(currentPage, currentIndex+1);
    }
    
    public override void SetCurrentInput(int page, int index)
    {
        base.SetCurrentInput(page, index);
        currentPage =  page;
        currentIndex = index;

        // 인덱스 바뀔 때 설정
        switch (page, index)
        {
            case (0,0) :
                Debug.Log("Input initialized");
                InputManager.Instance.ResetEnable = false;
                break;
            case (1 , 0) :
                _selectionNum = 0;
                circle.rectTransform.anchoredPosition = circlePos[0];
                break;
            case (1,1) :
                InputManager.Instance.ResetEnable = true;
                break;
            case (>= 2, var i) when i % 2 == 1 :
                // 입력 유지 처리
                AudioManager.Instance.PlayAudio(AudioName.StickUp, _performed);
                VideoManager.Instance.PauseVideo(_performed);
                break;
            case (>= 2, var i) when i % 2 == 0 :
                AudioManager.Instance.PlayAudio(AudioName.StickUp, false);
                break;
            default:
                break;
        }
    }

    public override void ExecuteInput(Key key, bool performed)
    {
        base.ExecuteInput(key, performed);
        if (key == Key.None)
        {
            Debug.LogWarning("Invalid or empty key received.");
            return;
        }

        if (key == Key.UpArrow)
        {
            _performed = performed;

            if (currentIndex % 2 == 1)
            {
                // 입력시 비디오 재생처리
                VideoManager.Instance.PauseVideo(performed);
                AudioManager.Instance.PlayAudio(AudioName.StickUp, performed);
            }
        }
        
        Action<Key,bool> action = (currentPage, currentIndex) switch
        {
            (0, 0) => P0I0,
            (1, 0) => P1I0,
            (1, 1) => P1I1,
            ( >= 2, var i) when i % 2 == 1 => P2I1,
            _ => DefaultInput
        };

        action(key,performed);
    }
    
    private void DefaultInput(Key context, bool performed)
    {
        Debug.Log($"P{currentPage}I{currentIndex} : Default - {context}");
        
        switch (context)
        {
            case Key.UpArrow:
                Debug.Log($"{currentPage}{currentIndex} : UpArrow Pressed");
                break;
        }
    }

    #region page0

    private void P0I0(Key context, bool performed)
    {
        if (!performed)
        {
            return;
        }
        Debug.Log($"Page0 Index0 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
            case Key.DownArrow:
                Debug.Log("Its Up/Down Arrow");
                break;
            case Key.Space:
                AudioManager.Instance.PlayOneShotAudio(AudioName.Button);
                PageController.Instance.LoadNextPage();
                VideoManager.Instance.PlayNextVideoOnOtherPage();
                other.DisableWithFade();
                break;
        }
    }
    
    private void P1I0(Key context, bool performed)
    {
        if (!performed) return;
        
        switch (context)
        {
            case Key.LeftArrow:
                --SelectionNum;
                break;
            case Key.RightArrow:
                ++SelectionNum;
                break;
        }
    }

    private void P1I1(Key context, bool performed)
    {
        if (!performed) return;
        
        switch (context)
        {
            case Key.LeftArrow:
                --SelectionNum;
                break;
            case Key.RightArrow:
                ++SelectionNum;
                break;
            case Key.Space:
                // 선택
                AudioManager.Instance.PlayOneShotAudio(AudioName.Button);
                VideoManager.Instance.SelectVideo(SelectionNum);
                break;
        }
    }
    
    private void P2I1(Key context, bool performed)
    {
        Debug.Log($"Page0 Index4 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
                break;
            case Key.Space:
                break;
        }
    }
    #endregion

}
