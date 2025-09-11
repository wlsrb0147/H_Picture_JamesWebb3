using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// 사용하는 오디오소스 이름을 Enum에 등록해야함
public enum AudioName
{
    Button = 0,
    South = 1,
    Saturn = 2,
    BlackHole = 3,
    Pillar = 4,
    StickHorizon = 5,
    StickUp = 6,
    Take = 7,
}

public class AudioManager : MonoBehaviour
{
    // 만들 오디오소스 게임오브젝트
    [SerializeField] private GameObject audioSourcePrefab;
    
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
        private set => _instance = value;
    }
    
    [Header("AudioSourceParent와 VideoSourceParent에\n AudioSource 자식 추가하면 자동등록됨\nAudioSource는 이 스크립트에 Enum 등록 필요")]
    [Header("json에 음원파일 넣으면, AudioSource 생성함")]
    [Space]
    
    private AudioSource[] _audioSources;
    public AudioSetting[] AudioSetting { get; set; }
    
    private Dictionary<AudioName, AudioSource> _audioSourcesDict;
    private Dictionary<string, AudioSource> _audioSourcesFromString;
    
    private bool _dataSet;

    
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

    #region 오디오 초기설정

    private void Start()
    {
        // 오디오 설정 등록
        SetAudioSetting(JsonSaver.Instance.GetAudioSetting());
    }

    // 오디오 클립, 오디오, 비디오 볼륨 세팅
    private void SetAudioSetting(AudioSetting[] audioSetting)
    {
        if (!_dataSet)
        {
            // 오디오 사용을 위한 딕셔너리 생성
            BuildAudioDictionaries();
        }

        // 중복 딕셔너리 생성 방지
        _dataSet = true;

        // 오디오 클립, 오디오 볼륨 설정
        StartCoroutine(LoadAndSetAudio(audioSetting));
    }
    
        
    // 딕셔너리 설정
    private void BuildAudioDictionaries()
    {
        _audioSourcesDict = new Dictionary<AudioName, AudioSource>();
        _audioSourcesFromString = new Dictionary<string, AudioSource>();

        // Enum 기준으로 Foreach 실행
        foreach (AudioName audioEnum in Enum.GetValues(typeof(AudioName)))
        {
            // 오디오소스를 자식으로 생성
            GameObject go = Instantiate(audioSourcePrefab, transform);
            
            // 게임오브젝트 이름 등록
            go.name = audioEnum.ToString();

            // AudioSource 컴포넌트 가져오기
            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogError($"{go.name}에 AudioSource 컴포넌트가 없습니다.");
                continue;
            }

            // 오디오 기본 loop설정은 true
            source.loop = true;
            source.playOnAwake = false;
            
            // 딕셔너리에 등록
            _audioSourcesDict[audioEnum] = source;
            _audioSourcesFromString[audioEnum.ToString()] = source;
        }
    }

    // 비디오 루프 켜기/끄기
    public void ChangeAudioLoop(AudioName audioEnum, bool loop)
    {
        if (_audioSourcesDict.TryGetValue(audioEnum, out AudioSource source))
        {
            source.loop = loop;
        }
        else
        {
            Debug.LogWarning($"Audio does not exist! : {audioEnum}");
        }
    }
    

    // Json에서 오디오 데이터 읽어오는건 코루틴으로 실행해야함
    private IEnumerator LoadAndSetAudio(AudioSetting[] audioSetting)
    {
        AudioSetting = audioSetting;
        
        foreach (var setting in AudioSetting)
        {
            // 파일명을 . 기반으로 분리, 파일명/확장자로 나뉘어짐
            string[] type = setting.fileName.Split(".");
            
            // 파일명 기반으로 딕셔너리에서 오디오소스 가져와서 볼륨 설정
            if (_audioSourcesFromString.TryGetValue(type[0], out AudioSource source))
            {
                source.volume = setting.volume;
            }
            else
            {
                Debug.LogWarning($"{type[0]}이 없습니다");
                continue;
            }
            
            // 오디오소스 경로 지정
            string path = Path.Combine(Application.streamingAssetsPath, "Audio" ,setting.fileName);
            
            // 오디오소스 확장자 관계없이 가져옴
            using var request = UnityWebRequestMultimedia.GetAudioClip(path,AudioType.UNKNOWN);
                
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"{path} does not Exist!");
                continue;
            }
               
            // 클립 할당
            source.clip = DownloadHandlerAudioClip.GetContent(request);
        
        }
    }
    
    #endregion
    
    
    // 오디오 재생 구간
    
    // 오디오 Play/Stop 설정
    public void PlayAudio(AudioName audioName, bool playSound)
    {
        if (_audioSourcesDict.TryGetValue(audioName, out AudioSource source))
        {
            if (source.clip == null)
            {
                // 오디오소스는 json에서 읽어오는데 시간이 걸려서 바로 실행시 null 발생가능
                // 그래서 null check 후 클립 없으면 없으면 잠깐 기다리고 실행함
                StartCoroutine(WaitAndPlay(source, playSound));
            }
            else
            {
                PlayAudio(source, playSound);
            }
        }
        else
        {
            Debug.LogWarning($"Audio does not exist! : {audioName}");
        }
    }
    
    // 코루틴이라 오디오 불러오는데 시간이 좀 걸려서, 기다렸다가 오디오 실행함
    private IEnumerator WaitAndPlay(AudioSource source, bool playSound)
    {
        float elapsed = 0f;
        // source.clip이 null인 동안, 그리고 타임아웃 전까지 매 프레임 대기
        while (source.clip == null && elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (source.clip == null)
        {
            Debug.LogWarning($"[{source}] 타임아웃, 2초 후에도 clip이 할당되지 않았습니다.");
            yield break;
        }

        PlayAudio(source, playSound);
    }

    // 오디오 실행
    private void PlayAudio(AudioSource source, bool playSound)
    {
        if (playSound)
            source.Play();
        else
            source.Stop();
    }
    


    // 오디오 한번 재생
    // 호출 : AudioManager.Instance.PlayOneShotAudio(AudioName.Sound);
    public void PlayOneShotAudio(AudioName audioName)
    {
        if (_audioSourcesDict.TryGetValue(audioName, out AudioSource source))
        {
            source.PlayOneShot(source.clip);
        }
        else
        {
            Debug.LogWarning($"Audio does not exist! : {audioName}");
        }
    }

    // 오디오 두개 이어서 재생
    private IEnumerator PlayContinuously(AudioSource firstAudio, AudioSource secondAudio)
    {
        firstAudio.Play();
        yield return new WaitForSeconds(firstAudio.clip.length);
        secondAudio.Play();
    }

    // 오디오 딜레이 주고 실행
    public void PlayAudioDelay(AudioName audioName, float delaySeconds)
    {
        StartCoroutine(DelayAudio(audioName, delaySeconds));
    }
    
    private IEnumerator DelayAudio(AudioName audioName, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        PlayAudio(audioName, true);
    }

    // 오디오 전부 정지
    public void StopAllAudio()
    {
        //sample.Stop();
    }

    // 오디오 소스 컴포넌트 받기
    public AudioSource GetAudioSource(AudioName audioName)
    {
        if (_audioSourcesDict.TryGetValue(audioName, out AudioSource source))
        {
            return source;
        }
        else
        {
            Debug.LogWarning($"Audio does not exist! : {audioName}");
            return null;
        }
    }
    
    // 오디오 Fade
    /*public void FadeAudio(AudioName audioName, float value, float fadeSeconds)
    {
        if (_audioSources.TryGetValue(audioName, out AudioSource source))
        {
            source.DOFade(value, fadeSeconds);
        }
        else
        {
            Debug.LogWarning($"Audio does not exist! : {audioName}");
        }
    }*/
}
