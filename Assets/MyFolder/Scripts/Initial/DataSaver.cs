using System;
using UnityEngine;
using Debug = DebugEx;

public class DataSaver : MonoBehaviour
{
    private static DataSaver _instance;
    public static DataSaver Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DataSaver>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
        private set => _instance = value;
    }
   
    // 그냥 데이터 저장하고 불러오는 곳
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
}
