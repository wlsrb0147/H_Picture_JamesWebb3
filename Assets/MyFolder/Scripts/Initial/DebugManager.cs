using System;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;


// 디버깅 툴 사용하는곳
public class DebugManager : MonoBehaviour
{
    private bool _cursorVisible;
    private Reporter _debugger;
    
    // 시작할 때 커서 안보이게함
    // 시작할 때 디버거 안보이게 함
    // 커서는 M, 디버거는 D
    
    private static DebugManager _instance;
    public static DebugManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DebugManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
        private set => _instance = value;
    }
    
    private void Awake()
    {
        if (_instance == null) 
        { 
            _instance = this;
            DontDestroyOnLoad(_instance.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        
        Cursor.visible = false;
        if (_debugger == null)
        {
            _debugger = FindAnyObjectByType<Reporter>();
        }
        _debugger.show = false;
        _debugger.gameObject.SetActive(true);
    }
    


    // 뉴인풋 입력받음
    public void ToggleObjs(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (context.control is not KeyControl control) return;

        Key key = control.keyCode;

        Action action = key switch
        {
            // M 누르면 커서 Visible 토글
            Key.M => ToggleCursorVisible,
            
            // D 누르면 Debugger 토글
            Key.D => ToggleDebugger,
            _ => () => {}
        };
        
        action?.Invoke();
    }

    private void ToggleCursorVisible()
    {
        if (_cursorVisible)
        {
            _cursorVisible = false;
            Cursor.visible = false;
            Debug.Log("Hide cursor");
        }
        else
        {
            _cursorVisible = true;
            Cursor.visible = true;
            Debug.Log("Show cursor");
        }
    }

    private void ToggleDebugger()
    {
        _debugger.ToggleShow();
    }
}
