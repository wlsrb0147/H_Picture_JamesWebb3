using System;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class ScenesInput1 : RegisterInputControl
{
    // 에디터에서 현재 입력 페이지를 보기위한 Serialize
    [SerializeField] private int currentPage;
    
    // 에디어테엇 현재 입력 인덱스를 보기위한 Serialize   
    [SerializeField] private int currentIndex;

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
        currentPage = page;
        currentIndex = index;

        // 인덱스 바뀔 때 설정
        switch (page, index)
        {
            case (0,0) :
                Debug.Log("Input initialized");
                break;
            case (0 , >= 1 and <= 5) :
                Debug.Log("P0 Index 1 to 5");
                break;
            case (1, 0 or 2) :
                Debug.Log("P1 Index 0 or 2");
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
        
        Action<Key,bool> action = (currentPage, currentIndex) switch
        {
            (0, 0) => P0I0,
            (0 , <= 2) => P0I2,
            (0 , > 3) => P0I4,
            (1,0) => P1I0,
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
        Debug.Log($"Page0 Index0 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
            case Key.DownArrow:
                Debug.Log("Its Up/Down Arrow");
                break;
            case Key.Space:
                Debug.Log("Its Up/Space");
                SceneManager.LoadScene(0);
                break;
        }
    }
    private void P0I2(Key context, bool performed)
    {
        Debug.Log($"Page0 Index2 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
            case Key.DownArrow:
                Debug.Log("Its Up/Down Arrow");
                break;
            case Key.Space:
                Debug.Log("Its Up/Space");
                break;
        }
    }
    private void P0I4(Key context, bool performed)
    {
        Debug.Log($"Page0 Index4 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
            case Key.DownArrow:
                Debug.Log("Its Up/Down Arrow");
                break;
            case Key.Space:
                Debug.Log("Its Space ");
                break;
        }
    }

    #endregion

    #region page1

    private void P1I0(Key context, bool performed)
    {
        Debug.Log($"Page0 Index4 Selected : {context}");
        switch (context)
        {
            case Key.UpArrow:
            case Key.DownArrow:
                Debug.Log("Its Up/Down Arrow");
                break;
            case Key.Space:
                Debug.Log("Its Space ");
                break;
        }
    }

    #endregion

}
