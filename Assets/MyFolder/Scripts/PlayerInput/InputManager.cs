using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public interface InputControl
{
    public void SetCurrentInput(int page, int index);
    public void ExecuteInput(Key key, bool performed);
    public void ChangeIndex();
}

// 입력 제어
public class InputManager : MonoBehaviour
{
    // page, index 기반으로 입력 제어
    
    private static InputManager _instance;

    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InputManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
        private set => _instance = value;
    }


    // 시리얼 컨트롤러
    [SerializeField] private SerialController controller;
    
    // true일시, 입력하면 내가 Json에서 설정한 string값으로 반환
    // arduino에서는 받은값 그대로 println 해주면 실제 아두이노랑 비슷하게 사용가능
    public bool sendStringToArduino;
    
    // 스틱 입력을 받을 때, 매핑 데이터
    private readonly Dictionary<string, Key> map = new ();
    private InputControl _inputControl;

    // PlayNextMainVideo 설정시, 약간 딜레이가 필요해서 연타방지
    private bool _acceptInput;

    public bool AcceptInput
    {
        get;
        set;
    }

    // 인덱스 변경
    public void ChangeIndex()
    {
        _inputControl.ChangeIndex();
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
    }

    private void Start()
    {
        // 입력없을 때 초기화 시간
        _resetStandard = JsonSaver.Instance.Settings.resetStandard;
        
        // 버튼 꾹 눌렀을 때 초기화 시간
        _pressResetStandard = JsonSaver.Instance.Settings.pressResetStandard;
        
        // 스틱 키 매핑
        map[JsonSaver.Instance.Settings.stickInput.up] = Key.UpArrow;
        map[JsonSaver.Instance.Settings.stickInput.down] = Key.DownArrow;
        map[JsonSaver.Instance.Settings.stickInput.left] = Key.LeftArrow;
        map[JsonSaver.Instance.Settings.stickInput.right] = Key.RightArrow;
        map[JsonSaver.Instance.Settings.stickInput.button] = Key.Space;
    }

    // 씬 전환시, SceneInput 스크립트 교체
    public void SetInputControl(InputControl inputControl)
    {
        _inputControl = inputControl;
    }

    // 페이지 또는 인덱스가 바뀌었을 때의 설정 적용
    public void SetCurrentIndex(int page, int index)
    {
        if (_inputControl == null)
        {
            Debug.Log("Find first input control");
            _inputControl = FindFirstObjectByType<RegisterInputControl>();
        }
        
        AcceptInput = true;
        _inputControl.SetCurrentInput(page, index);
    }
    
    // 뉴인풋으로 입력 제어
    // 현재는 눌러졌을때만 입력을 받음 (preformed)
    // 뗐을때도 입력받고싶다면 canceled 도 입력받아야함
    public void KeyboardInputControl(InputAction.CallbackContext context)
    {
        bool performed;
        if (context.performed)
        {
            performed = true;
        }
        else if (context.canceled)
        {
            performed = false;
        }
        else
        {
            return;
        }
        
        // 키보드 입력이 아닐경우 리턴
        
        Key key;

        switch (context.control)
        {
            case KeyControl keyControl:
                key = keyControl.keyCode;
                break;
            case ButtonControl buttonControl:
                key = map.GetValueOrDefault(buttonControl.name, Key.None);

                if (key == Key.None)
                {
                    Debug.Log($"Invalid Input Name : {buttonControl.name}");
                }
                
                break;
            default:
                Debug.Log($"Invalid control type : {context.control.GetType()}" );
                return;
        }
        
        // bool값이 true라면, 키보드 입력을 SerialController에 전달
        if (sendStringToArduino)
        {
            InputData data = new InputData
            {
                Key =  key,
                IsPressed = performed
            };
            controller.SendArduinoKey(0,data);
            return;
        }
        
        Debug.Log($"Keyboard Input Come : {key}");
        
        if (performed)
        {
            _holdingKeys.Add(key);
        }
        else
        {
            _holdingKeys.Remove(key);
        }

        if (key == Key.Space)
        {
            PressReset = performed;
        }
        
        // 이렇게해두면 현재는 눌렀을 때 한번만 인식함
        /*if (!performed)
        {
            return;
        }*/
        
        if (!AcceptInput) return;
        ExecuteInput(key, performed);
    }
    
    // SerialController에서 전달받은 키보드 입력
    // SerialController에는 string을 key로 변환시켜 이 함수를 실행
    // index는 신호를 보낸 아두이노의 Thread Index, 여러 아두이노를 사용할 때 필요함
    public void ArduinoInputControl(InputData data,int index)
    {
        ExecuteInput(data.Key, data.IsPressed);
    }
    
    
    // 입력에 따른 함수 실행
    private void ExecuteInput(Key key, bool performed)
    {
        _inputControl.ExecuteInput(key, performed);
    }
    
    
    
    // 현재 입력중인 키들
    private readonly HashSet<Key> _holdingKeys = new();  
    
    // 입력 없을 때 초기화
    private float _resetStandard;
    private float _timeLeft;
    
    // 버튼 누르고있을 때 초기화
    private float _pressResetStandard;
    private float _pressTimeLeft;
    
    // n초간 Space 누르고있으면 리셋
    private bool _pressReset;
    
    private bool PressReset
    {
        get => _pressReset;
        set
        {
            _pressTimeLeft = _pressResetStandard;
            _pressReset = value;
        }
    }
    
    // ResetEnable 이 True 일 때,
    // 입력이 없을경우, resetTimer 돌아감
    // ResetEnable 타이밍은 직접 설정
    private bool _resetEnable;
    public bool ResetEnable
    {
        get => _resetEnable;
        set
        { 
            _resetEnable = value;
            _timeLeft = _resetStandard;
        }
    }
    
    private void Update()
    {
        if (PressReset)
        {
            _pressTimeLeft -= Time.unscaledDeltaTime;

            if (_pressTimeLeft <= 0)
            {
                PressReset = false;
                
                // 이건 각자 적절하게 맞추세요
                
                // 씬 하나면 비디오매니저 고 타이틀
                VideoManager.Instance.GoTitle();
                
                return;
            
                // 씬 여러개면 로드씬 0
                SceneManager.LoadScene(0);
            
                
            }
        }

        if (!ResetEnable) return;
        
        if (_holdingKeys.Count != 0)
        {
            ResetEnable = _resetEnable;
            return;
        }
        
        _timeLeft -= Time.unscaledDeltaTime;

        if (_timeLeft <= 0)
        {
            ResetEnable = false;
            
            // 이건 각자 적절하게 맞추세요
            
            // 씬 하나면 비디오매니저 고 타이틀
            VideoManager.Instance.GoTitle();
            
            return;
            
            // 씬 여러개면 로드씬 0
            SceneManager.LoadScene(0);
        }
    }
    
}
