using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ScenesInput0 : RegisterInputControl
{
    private static readonly int SELECT = Animator.StringToHash("Select");
    private static readonly int SPD = Animator.StringToHash("Spd");
    private static readonly int END = Animator.StringToHash("End");

    // 에디터에서 현재 입력 페이지를 보기위한 Serialize
    [SerializeField] private int currentPage;
    
    // 에디어테엇 현재 입력 인덱스를 보기위한 Serialize   
    [SerializeField] private int currentIndex;
    
    private bool _performed;

    [SerializeField] private Image circle;
    [SerializeField] private Animator[] anim;
    private int _selectionNum;
    [SerializeField] private GameObject effect;

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

        int len = clips.Length;
        _clipLength = new float[len];

        for (int i = 0; i < len; i++)
        {
            _clipLength[i] = clips[i].length;
        }

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
                
                InputManager.Instance.ResetEnable = true;
                _selectionNum = 0;
                circle.rectTransform.anchoredPosition = circlePos[0];
                break;
            case (1,1) :
                InputManager.Instance.ResetEnable = true;
                break;
            case (>= 2, var i) when i % 2 == 1 :
                // 입력 유지 처리
                AudioManager.Instance.PlayAudio(AudioName.StickUp, _performed);
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
            (2, 0) => P2I0,

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
                VideoManager.Instance.PlayNextMainVideo();
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
            case Key.Space:
                // 선택
                anim[_selectionNum].gameObject.SetActive(true);
                AudioManager.Instance.PlayOneShotAudio(AudioName.Button);
                AudioManager.Instance.PlayOneShotAudio((AudioName)(SelectionNum+1));
                effect.SetActive(true);
                break;
        }
    }
    
    private void P2I0(Key context, bool performed)
    {
        Debug.Log($"Page2 Index0 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
                anim[_selectionNum].SetFloat(SPD, performed ? 1 : 0);
                CurrentSpd = performed ? 1 : 0;
                break;
            case Key.DownArrow:
                anim[_selectionNum].SetFloat(SPD, performed ? -1 : 0);
                CurrentSpd = performed ? -1 : 0;
                break;
            case Key.Space:
                PageController.Instance.OpenAndClosePage(1);
                break;
        }
    }
    #endregion

    private void GetBounds(out bool atStart, out bool atEnd)
    {
        atStart = atEnd = false;

        // ① 네가 쓰는 프레임 트래커가 있으면 그걸 우선
        // if (GetAnimFrame.Instance) {
        //     int f = GetAnimFrame.Instance.currentFrame;
        //     atStart = f <= 0;
        //     atEnd   = f >= GetAnimFrame.Instance.maxFrame;
        //     return;
        // }

        // ② Animator 기준(비루프 클립 가정)
        var st = anim[_selectionNum].GetCurrentAnimatorStateInfo(0);
        var clips = anim[_selectionNum].GetCurrentAnimatorClipInfo(0);
        bool looping = clips.Length > 0 && clips[0].clip && clips[0].clip.isLooping;

        if (!looping)
        {
            float nt = st.normalizedTime;     // 비루프면 0~1 구간이 유효
            atStart = nt <= 0.001f;
            atEnd   = nt >= 0.999f;
        }
    }

    [SerializeField] private AnimationClip[] clips;
    private float[] _clipLength;
    private int currentFrame;
    private const float FRAMERATE = 30;

    private float _currentSpd;
    private float CurrentSpd
    {
        get =>  _currentSpd;
        set
        {
            anim[_selectionNum].SetFloat(SPD, value);
            _currentSpd = value;
        }
    }
    
    private void Update()
    {
        if (!anim[_selectionNum] || !anim[_selectionNum].isActiveAndEnabled) return ;
        
        var state = anim[_selectionNum].GetCurrentAnimatorStateInfo(0);
        var infos = anim[_selectionNum].GetCurrentAnimatorClipInfo(0);
        if (infos.Length == 0) return;
        
        AnimationClip playingClip = infos[0].clip;
        int idx = Array.IndexOf(clips, playingClip);
        if (idx < 0) return;

        float length = _clipLength[idx];
        int endFrame = Mathf.Max(0, Mathf.FloorToInt(length * FRAMERATE) - 1);
        
        float normalized = state.normalizedTime;

        if (normalized >= 1)
        {
            return;   
        }
        float timeSec = normalized * length;

        currentFrame = Mathf.FloorToInt(timeSec * FRAMERATE);

        //Debug.Log("Current : " + currentFrame + ", EndFrame : " + endFrame);
        if (currentFrame <= 0 && CurrentSpd <= -1 || currentFrame >= endFrame && CurrentSpd >= 1)
        {
            CurrentSpd = 0;
        }
    }

}
