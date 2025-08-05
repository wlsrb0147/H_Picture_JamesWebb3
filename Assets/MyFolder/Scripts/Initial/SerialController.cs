using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugEx;
using System.Threading;
using UnityEngine.InputSystem;


// 아두이노를 뉴인풋에 대응시키기 위한 구조체
public struct InputData : IEquatable<InputData>
{
    //누른 버튼
    public Key Key;
    //눌렸는지, 풀었는지
    public bool IsPressed;

    // 내부 데이터가 같으면 true
    public bool Equals(InputData other)
    {
        return Key == other.Key && IsPressed == other.IsPressed;
    }

    // 기본 딕셔너리 비교 : IEqualityComparer<TKey>를 전달하지 않으면, 내부적으로 EqualityComparer<TKey>.Default 사용
    // 현재 코드에선 IEquatable<InputData>를 사용하고있기에, Equals와 GetHashCode로 Dictionary 비교
    
    // == 를 했을 때, InputData 형식이고, 내부 데이터가 같다면 true
    public override bool Equals(object obj)
    {
        return obj is InputData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        // Key 와 IsPressed 기반의 해시코드 사용
        return HashCode.Combine((int)Key, IsPressed);
    }
}

public class SerialController : MonoBehaviour
{
    // 잘못된 입력 들어왔을 때 값
    private readonly InputData _none = new ()
    {
        Key = Key.None,
        IsPressed = false
    };
    
    [Tooltip("Port name with which the SerialPort object will be created.")]
    
    // Json에서 재할당해서 COM3은 무시해도됨
    public string[] portName = {"COM3"};

    [Tooltip("Baud rate that the serial device is using to transmit data.")]
    public int baudRate = 9600;

    [Tooltip("After an error in the serial communication, or an unsuccessful " +
             "connect, how many milliseconds we should wait.")]
    public int reconnectionDelay = 1000;

    [Tooltip("Maximum number of unread data messages in the queue. " +
             "New messages will be discarded.")]
    public int maxUnreadMessages = 1;
    
    public const string SERIAL_DEVICE_CONNECTED = "__Connected__";
    public const string SERIAL_DEVICE_DISCONNECTED = "__Disconnected__";

    protected Thread[] thread;
    protected SerialThreadLines[] serialThread;

    // 포트 설정전까지는 _isReady False, 설정완료되면 true 전환
    private bool _isReady;
    
    private ArduinoSetting _arduinoSetting;

    private InputManager _inputManager;

    // 가독성을 위한 bool
    private const bool Pressed = true;
    private const bool Released = false;
    
    // 쓰레드 생성순서 보장용
    private readonly List<string> _nameList = new ();

    // 아두이노 테스트용 딕서녀리 
    // 키 입력을 string으로 변환
    private readonly Dictionary<InputData, string> _keyToString = new ();
    
    // 내가 받을 스트링 딕셔너리
    // string을 key 입력으로 변환
    private readonly Dictionary<string, InputData> _stringToKey = new ();
    
    private static SerialController _instance;
    public static SerialController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SerialController>();
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
    }

    // 아두이노 정보 등록
    private void SetArduinoData(ArduinoSetting setting)
    {
        if (!gameObject.activeSelf) return;
        
        _arduinoSetting = setting;
        
        maxUnreadMessages = 120;
        
        thread = new Thread[setting.portNames.Length];
        serialThread = new SerialThreadLines[setting.portNames.Length];
        
        for (int i = 0; i < setting.portNames.Length; i++)
        {
            CreateSerialThread(i, setting.portNames[i]);
        }
        
        SetDictionary();
        _isReady = true;
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;
        SetArduinoData(JsonSaver.Instance.GetArduinoSetting());
    }

    // 필수. 아두이노 데이터 등록해줘야함
    private void SetDictionary()
    {
        _keyToString.Clear();
        _stringToKey.Clear();

        if (_arduinoSetting == null)
        {
            Debug.LogWarning("ArduinoSetting is null or empty. Skipping dictionary setup.");
            return;
        }
        
        // 키, Press/Release / 아두이노 string값 세개 등록
        RegisterKeyCodes(Key.Space, Pressed, _arduinoSetting.spacePressed);
        RegisterKeyCodes(Key.Space, Released, _arduinoSetting.spaceReleased);
    }
    
    private void RegisterKeyCodes(Key key,bool isPressed , string value)
    {
        InputData data = new InputData
        {
            Key = key,
            IsPressed = isPressed
        };

        _keyToString[data] = value;
        _stringToKey[value] = data;
    }
    

    private void CreateSerialThread(int index, string port)
    {
        Debug.Log($"index :{index} port : {port}");
        serialThread[index] = new SerialThreadLines(port, 
            baudRate, 
            reconnectionDelay,
            maxUnreadMessages);
        thread[index] = new Thread(serialThread[index].RunForever);
        thread[index].Start();
        
        // 내가 적은 리스트 순서대로 쓰레드 생성되게, 리스트 순서에 안맞으면 null로 리스트 확장 후 할당
        while (_nameList.Count <= index)
        {
            _nameList.Add(null);  // 리스트 확장
        }
        _nameList[index] = port;
    }
    
    void OnDisable()
    {
        for (int i = 0; i < serialThread.Length; i++)
        {
            if (serialThread[i] != null)
            {
                serialThread[i].RequestStop();
                serialThread[i] = null;
            }
        }

        for (int i = 0; i < thread.Length; i++)
        {
            if (thread[i] != null)
            {
                thread[i].Join();
                thread[i] = null;
            }
        }
    }
    
    void Update()
    {
        if (!_isReady) return;
        
        if (serialThread.Length == 0)
            return;

        for (int i = 0; i < serialThread.Length; i++)
        {
            ReadSerialMessage(i);
        }
    }

    private void ReadSerialMessage(int index)
    {
        string message = (string)serialThread[index].ReadMessage();
        
        if (message == null) return;

        if (ReferenceEquals(message, SERIAL_DEVICE_CONNECTED))
        {
            Debug.Log("On connection event : true On " + _nameList[index]);
        }
        else if (ReferenceEquals(message, SERIAL_DEVICE_DISCONNECTED))
        {
            Debug.Log("On connection event : false On " + _nameList[index]);
        }
        else
        {
            // string기반으로 InputData 생성
            InputData key = ConvertStringToKey(message);
            Debug.Log($"Get Message From {_nameList[index]} : {message}");
            // inputManager에 key 입력
            _inputManager.ArduinoInputControl(key,index);
        }
    }
    
    private InputData ConvertStringToKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return _none;

        input = input.Trim().Replace("\n", "");

        return _stringToKey.GetValueOrDefault(input, _none);
    }

    // 아두이노에 InputData를 string으로 바꿔서 전달
    // 그냥 빈 아두이노에 테스트 할 때, 아두이노에서 받은 String 그대로 반환하게 코드 짜면 이 코드로 아두이노 테스트 통신 가능
    public void SendArduinoKey(int index, InputData data)
    {
        string s = _keyToString.GetValueOrDefault(data, data.Key.ToString());
        
        SendSerialMessage(index, s);
    }
    
    // serialThread[index] 에 message 보냄
    // 여러 아두이노 쓸 때 index값이 필요함
    public void SendSerialMessage(int index, string message)
    {
        if (serialThread[index] == null)
        {
            Debug.Log("serialThread is null");
        }
        
        Debug.Log($"Send Arduino{index} : {message}");
        serialThread[index].SendMessage(message);
    }
}
