using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.UI;
using UnityEngine.Video;


[Serializable]
public class VideoPlayers
{
    public VideoPlayer[] mainPlayers;
    public bool[] isMainLoop;
    public bool[] hasLoop;
    public bool[] dontPlayNextVideo;
    public VideoPlayer[] loopPlayers;
    
    #region 여긴 안읽어도됨. 인스펙터 전용 함수

    private void CheckAndSet(ref bool[] array, int len)
    {
        if (array == null || array.Length != len)
        {
            bool[] newArray = new bool[len];
            if (array != null)
            {
                int copyLength = Mathf.Min(array.Length, len);
                Array.Copy(array, newArray, copyLength);
            }
            array = newArray;
        }
    }
    
    public void ValidateArrays()
    {
        if (mainPlayers == null)
            return;

        int targetLength = mainPlayers.Length;

        CheckAndSet(ref isMainLoop, targetLength);
        CheckAndSet(ref hasLoop, targetLength);
        CheckAndSet(ref dontPlayNextVideo, targetLength);
    }
    
    #endregion

}


public class VideoManager : MonoBehaviour
{
    #region 여기도 안읽어도 쓰는데 크게는 문제없을듯
    
    // currentPage, nextPage값 설정시 순환참조 방지용
    private bool _isUpdatingPage;
    
    // 비디오들을 딕셔너리에 int값 기반으로 저장하는데,
    // 그때 사용할 딕셔너리 int값
    private int[] _dictionaryIndex;
    
    // 비디오가 재생될 때, InputManager의 page, index가 수정되는데
    // 권한이 있는 비디오만 page, index를 수정할 수 있게 하는 리스트
    private readonly HashSet<int> _notAllowedToChangeControl = new();
    
    // 현재 페이지, 다음 페이지 등록 함수
    private int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage == value) return;
        
            _currentPage = value;

            if (_isUpdatingPage) return;

            _isUpdatingPage = true;
            UpdateNextPage();
            _isUpdatingPage = false;
        }
    }
    
    
    private void UpdateNextPage()
    {
        if (_currentPage + 1 >= videoPlayers.Length)
            _nextPage = 0;
        else
            _nextPage = _currentPage + 1;
    }

    
    private int NextPage
    {
        get => _nextPage;
        set
        {
            if (_nextPage == value) return;

            if (_isUpdatingPage)
            {
                _nextPage = value;
                return;
            }
            else
            {
                _isUpdatingPage = true;
                UpdateCurrentPage();
                _isUpdatingPage = false;
                
                _nextPage = (value >= videoPlayers.Length) ? 0 : value;
                
            }
        }
    }

    private void UpdateCurrentPage()
    {
        _currentPage = _nextPage;
    }

    // 게임 처음 시작할 때, 비디오를 dictionary에 등록 및 초기설정하는 함수
    // int로 등록하는 기준 : page = 100의 자리, index = 1,10의 자리
    // ex) 0페이지 3인덱스 : 003, 1페이지 5인덱스 : 105, 3페이지 12인덱스 : 312
    private void RegisterVideos()
    {
         if (videoPlayers == null || videoPlayers.Length == 0) return;
        
        _dictionaryIndex = new int[videoPlayers.Length];
        
        for (int i = 0; i < videoPlayers.Length; i++)
        {
            // 100의 자리는 현재 페이지 넘버
            _dictionaryIndex[i] = i * 100;
        }

        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] == null || videoPlayers[i].mainPlayers == null) continue;

            for (int j = 0; j < videoPlayers[i].mainPlayers.Length; j++)
            {
                if (!videoPlayers[i].mainPlayers[j]) continue;
            
                int x = i * 100 + j;
                _videoArrayValue.Add(videoPlayers[i].mainPlayers[j], x);
            }
        }
        
        foreach (var t in videoPlayers)
        {
            if (t == null) continue;

            if (t.mainPlayers != null)
            {
                foreach (var v in t.mainPlayers)
                {
                    if (!v) continue;
                    v.isLooping = false;
                    v.skipOnDrop = false;
                    // 비디오 재생이 완료되면 자동으로 다음비디오 재생
                    v.loopPointReached += AutoPlayNextVideo;
                    
                    // 비디오 시작시 현재 비디오의 page와 index를 InputManager에 등록
                    v.started += RegisterCurrentIndexOnControlManager;
                    
                    // 비디오에 에러가 있을 때 실행
                    v.errorReceived += OnVideoError;
                }
            }

            if (t.loopPlayers != null)
            {
                foreach (var v in t.loopPlayers)
                {
                    if (!v) continue;
                    v.isLooping = false;
                    
                    // mp4 비디오는 loop를 그냥 재생할 경우, 프레임이 튀는 현상이 존재함
                    // 그래서 loop를 수동제어
                    v.loopPointReached += Loop;
                }
            }
        }

        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] == null) continue;
        
            for (int j = 0; j < videoPlayers[i].mainPlayers.Length; j++)
            {
                if (videoPlayers[i].isMainLoop != null && videoPlayers[i].isMainLoop[j])
                {
                    // 인스펙터에서 loop 체크하면 수동 loop 추가
                    IsMainLoopVideo(i, j);
                    continue;
                }

                if (videoPlayers[i].hasLoop != null && videoPlayers[i].hasLoop[j])
                {
                    InputLoopVideo(i, j);
                    continue;
                }

                if (videoPlayers[i].dontPlayNextVideo != null && videoPlayers[i].dontPlayNextVideo[j])
                {
                    // 비디오가 끝나도 다음비디오 재생안함
                    DontPlayNextVideo(i, j);
                }
            }
        }
    }
    
    private void Loop(VideoPlayer source)
    {
        source.frame = 0;
        source.Play();
    }
    
    // ControlManager에 Page, Index 등록하는 코드
    private void RegisterCurrentIndexOnControlManager(VideoPlayer source)
    {
        if (!_notAllowedToChangeControl.Add(_videoArrayValue[source])) return;

        int page = _videoArrayValue[source] / 100;
        int index = _videoArrayValue[source] % 100;

        CurrentPage = page;
        _currentIndex = index;
        
        _inputManager.SetCurrentIndex(page,index);
        Debug.Log("Current Video : " + page + "" + index);
    }
    
    // Loop영상 추가 메서드
    // 이거 쓸 일 거의 없는듯
    private void InputLoopVideo(int page, int index)
    {
        VideoPlayer player = videoPlayers[page].mainPlayers[index];
        // 100,101,102,200,201,202등으로 값 저장됨

        if (_loopAddedVideo.ContainsKey(player)) return;
        
        _loopAddedVideo.Add(player,_dictionaryIndex[page]);
        
        //첫 스트링이 page, 뒷자리 두개 스트링이 index
        player.loopPointReached -= AutoPlayNextVideo;
        player.loopPointReached += PlayLoopPlayer;
        ++_dictionaryIndex[page];
    }

    // 자동재생 
    private void AutoPlayNextVideo(VideoPlayer source)
    {
        Debug.Log($"AutoPlay By {source}");
        PlayNextMainVideo();
    }
    
    // 루프영상 재생
    // 쓸 일 없을듯
    private void PlayLoopPlayer(VideoPlayer source)
    {
        Debug.Log($"SubVideo {source}");
        
        int page = _loopAddedVideo[source] / 100;
        int index = _loopAddedVideo[source] % 100;
        
        VideoPlayer player = videoPlayers[page].loopPlayers[index];
        player.Play();
        player.targetTexture = _currentRenderTexture;
        source.targetTexture = null;
        source.Stop();
        
        Debug.Log($"{player} played, {source} stopped");
    }
    
    // 다음으로 재생시킬 유효한 비디오를 찾아서 prepare함
    // 유효하지 않은 비디오가 도중에 발견될 경우,
    // AutoPlayNextVideo를 제거하여 수동으로 다음비디오 제어하도록 함
    private void PrepareNextVideo(VideoPlayer player)
    {
        _currentVideoPlayer = player;
        int page = _videoArrayValue[player] / 100; 
        int index = _videoArrayValue[player] % 100;
        
        bool removeAutoPlay = false;
        int count = 0;

        // 다음페이지 탐색
        while (true)
        {
            ++count;
            
            // 현재 페이지 길이 확인
            if (videoPlayers[page].mainPlayers.Length != 0)
            {
                ++index;
                
                // 인덱스 범위 밖이면 다음페이지로
                if (index >= videoPlayers[page].mainPlayers.Length)
                {
                    index = -1;
                    page = (page + 1) % videoPlayers.Length;
                    continue;
                }
                
                // 비디오 있으면 루프 중단
                if (videoPlayers[page].mainPlayers[index] != null)
                {
                    break;
                }
                
                // 유효하지 않을경우, 현재 비디오 자동재생 제거
                removeAutoPlay = true;

            }
            else
            {
                // 현재 페이지의 인덱스 길이가 0이면 다음페이지로
                removeAutoPlay = true;
                page = (page + 1) % videoPlayers.Length;
                index = -1;
            }
    
            // 무한 루프 방지를 위한 종료 조건
            if (count >= 100)
            {
                Debug.LogError("There's No Video In All Array");
                return;
            }
        }

        // 유효하지 않은 요소를 발견한 경우 자동재생 이벤트 취소
        if (removeAutoPlay)
        {
            player.loopPointReached -= AutoPlayNextVideo;
        }

        
        _nextVideoPlayer = videoPlayers[page].mainPlayers[index];
        _nextVideoPlayer.Prepare();
        
        if (_loopAddedVideo.ContainsKey(_nextVideoPlayer))
        {
            int page2 = _loopAddedVideo[_nextVideoPlayer] / 100;
            int index2 = _loopAddedVideo[_nextVideoPlayer] % 100;
        
            VideoPlayer loopVideo = videoPlayers[page2].loopPlayers[index2];
        
            loopVideo.Prepare();
        }
        
        Debug.Log("PreparedVideo is + " + page + "" + index);
    }
    
    
    // 재생중인 비디오 정지    
    private void StopVideoArray(VideoPlayer[] players)
    {
        if (players == null || players.Length == 0) return;

        foreach (var player in players)
        {
            if (player == null) continue;

            if (player.isPrepared)
            {
                player.Stop();
            }
            else if (player.isPlaying)
            {
                player.Stop();
                player.targetTexture = null;
            }
        }
    }
    
    // 비디오 루프재생
    private void IsMainLoopVideo(int page,int index)
    {
        VideoPlayer player = videoPlayers[page].mainPlayers[index];
        player.loopPointReached -= AutoPlayNextVideo;
        player.loopPointReached += Loop;
    }
    
    // 자동재생 취소
    private void DontPlayNextVideo(int page,int index)
    {
        VideoPlayer player = videoPlayers[page].mainPlayers[index];
        player.loopPointReached -= AutoPlayNextVideo;
        player.loopPointReached -= Loop;
    }

    // _notAllowedToChangeControl 갱신
    private void RemoveOrAddVideosOnHashSet(int changedPage)
    {
        if (videoPlayers == null) return;

        if (changedPage > _currentPage)
        {
            // currentPage부터 changedPage - 1까지 등록
            for (int page = _currentPage; page < changedPage; page++)
            {
                if (!IsValidPage(page))
                    continue;

                int startIndex = (page == _currentPage) ? _currentIndex : 0;
                int endIndex = videoPlayers[page].mainPlayers.Length - 1;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var video = videoPlayers[page].mainPlayers[i];
                    if (video)
                    {
                        _notAllowedToChangeControl.Add(_videoArrayValue[video]);
                    }
                }
            }
        }
        else if (changedPage < _currentPage)
        {
            // changedPage부터 currentPage까지 제거
            for (int page = changedPage; page <= _currentPage; page++)
            {
                if (!IsValidPage(page))
                    continue;

                int startIndex = 0;
                int endIndex = (page == _currentPage) ? _currentIndex : videoPlayers[page].mainPlayers.Length - 1;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var video = videoPlayers[page].mainPlayers[i];
                    if (video)
                    {
                        _notAllowedToChangeControl.Remove(_videoArrayValue[video]);
                    }
                }
            }
        }
    }

    private bool IsValidPage(int page)
    {
        return page >= 0
               && page < videoPlayers.Length
               && videoPlayers[page] != null
               && videoPlayers[page].mainPlayers != null;
    }
    
    #endregion
    
    public static VideoManager Instance;

    [Header("비디오 플레이어가 없는 페이지를 만났을 때, \n다음 비디오는 자동재생되지않음")]
    [Header("VideoPlayers에 VideoPlayer 자식 붙이면, 비디오 클립 할당")]
    [Header("비디오 재생 인덱싱은 하이어라키에서 수동으로 해야함")][Space]
    
    [SerializeField] private Transform videoPlayersParent;
    private InputManager _inputManager;
    [SerializeField] private PageController pageController;
    
    [Space]
    
    [SerializeField] private VideoPlayers[] videoPlayers;

    [Header("이 트랜스폼의 자식에서 게임오브젝트와 RawImage의 targetTexture 가져옴")]
    [SerializeField] private Transform renderTextureTransform;
    private readonly List<GameObject> _renderTextureObj = new ();
    private readonly List<RenderTexture> _renderTexture = new ();
    
    private VideoPlayer _currentVideoPlayer;
    private VideoPlayer _nextVideoPlayer;
    
    private RenderTexture _currentRenderTexture;
    private int _renderTextureIndex;
    
    private int _currentPage = -1;
    private int _nextPage = -1;
    private int _currentIndex = -1;
    
    // 영상 2차원 매핑 딕셔너리
    private readonly Dictionary<VideoPlayer, int> _videoArrayValue = new();
    
    // 루프영상이 추가된 영상 딕셔너리
    private readonly Dictionary<VideoPlayer, int> _loopAddedVideo = new();
    
    // 비디오 동시재생을 위한 PlayNextVideoOnOther실행시, 기존 비디어플레이어는 여기에 저장됨
    private VideoPlayer _playerToStopLater; 
    private GameObject _renderTextureObjToCloseLater;

    [SerializeField] private ScenesInput0 input;
    
    private void Awake()
    {
        Instance = this;

        InitializeRenderTexture();
    }

    private void Start()
    {
        SetVideoPath(JsonSaver.Instance.GetVideoSetting());
        
        _inputManager =  InputManager.Instance;
    }

    private bool _initialized;

    // 렌더텍스쳐 리스트 생성
    // renderTextureTransform에 RenderTexture를 두면, 리스트에 추가함
    private void InitializeRenderTexture()
    {
        if (_initialized) return;

        _initialized = true;
        
        foreach (Transform v in renderTextureTransform)
        {
            GameObject vg = v.gameObject;
            _renderTextureObj.Insert(0, vg);
            _renderTexture.Insert(0, vg.GetComponent<RawImage>().texture as RenderTexture);
        }
    }
    
    // 비디오 초기설정
    // 비디오명과 비디오플레이어 게임오브젝트 이름 맞춰줘야함
    private void SetVideoPath(VideoSetting[] settings)
    {
        if (videoPlayers.Length == 0) return;
        if (settings.Length == 0) return;

        // 이름기반 경로
        var videoDict = new Dictionary<string, string>();
        // 이름기반 볼륨
        var volumeDict = new Dictionary<string, float>();

        // 파일명 기반으로 경로 생성
        foreach (var v in settings)
        {
            // 확장자 앞 이름을 s 로 등록
            string s = v.fileName.Split(".")[0];
            
            // 파일명 기반으로 경로 딕셔너리 생성
            videoDict[s] = Path.Combine(Application.streamingAssetsPath, "Videos" , v.fileName);
            volumeDict[s] = v.volume;
        }
        
        // transform 의 child에서 videoPlayer 설정
        foreach (Transform child in videoPlayersParent)
        {
            VideoPlayer vp = child.GetComponent<VideoPlayer>();

            if (vp != null)
            {
                // 이름 기반으로 url 등록
                //vp.url = videoDict[vp.name];
                vp.audioOutputMode = VideoAudioOutputMode.Direct;
                vp.SetDirectAudioVolume(0, volumeDict[vp.name]);
            }
            else
            {
                Debug.LogWarning($"{vp}에 비디오플레이어가 없음");
            }
        }
        // 렌더텍스쳐 할당
        InitializeRenderTexture();
        
        // 비디오 초기화
        RegisterVideos();
        
        // 비디오 콜백 등록
        RegisterEvents();
        
        // 비디오 실행
        StartVideo();
    }
    
    private void StartVideo()
    {
        videoPlayers[0].mainPlayers[0].prepareCompleted += InitializeVideo;
        videoPlayers[0].mainPlayers[0].Prepare();
    }
    
    // 렌더텍스쳐 0번 켜고, 나머지 끔
    private void InitializeRenderTextureObj()
    {
        if (_renderTextureObj.Count > 0)
        {
            _renderTextureObj[0].SetActive(true);
            for (int i = 1; i < _renderTextureObj.Count; i++)
            {
                _renderTextureObj[i].SetActive(false);
            }
        }
    }
    
    // 비디오 초기설정
    private void InitializeVideo(VideoPlayer source)
    {
        Debug.Log("Initializing Video");
        source.prepareCompleted -= InitializeVideo;
        _notAllowedToChangeControl.Clear();
        source.Play();
        source.targetTexture = _renderTexture[0];
        _currentRenderTexture = _renderTexture[0];
        _renderTextureIndex = 0;
        _currentIndex = 0;
        CurrentPage = 0;
        PrepareNextVideo(source);
        InitializeRenderTextureObj();
    }
    
    // 여기에 재생시킬 함수들 등록하면 됨
    private void RegisterEvents()
    {
        videoPlayers[1].mainPlayers[0].started += PrePareEndingVideo;
    }
    
    // 각 행성별 마지막 비디오 재생시, 홈화면
    private void LastVideoStarted(VideoPlayer v)
    {
        ChangeNextVideo(1, 0);
    }

    // 비디오 선택 0123
    // 비디오는 2345니까, +2
    /*public void SelectVideo(int val)
    {
        int val2 = val + 2;
        ChangeNextVideo(val2,0);
        for (int i = 2; i < videoPlayers.Length; i++)
        {
            if (i != val2)
            {
                videoPlayers[i].mainPlayers[^1].Stop();
            }
        }
        PlayNextMainVideo();
        pageController.OpenAndClosePage(val2);
    }*/

    private void OpenPage1(VideoPlayer v)
    {
        pageController.OpenAndClosePage(1);
    }

    // 다음페이지 불러옴
    private void LoadNextPage(VideoPlayer source)
    {
        pageController.LoadNextPage();
    }

    private void PrepareEnding(VideoPlayer source)
    {
        ChangeNextVideo(0, 0);
    }
    
    public void PrePareEndingVideo(VideoPlayer v)
    {
        VideoPlayer vp = videoPlayers[0].mainPlayers[0];
        
        if (vp)
        {
            vp.Prepare();
        }
    }

    public void PrePareEndingVideo()
    {
        VideoPlayer vp = videoPlayers[0].mainPlayers[0];
        
        if (vp)
        {
            vp.Prepare();
        }
    }

    // 비디오 재생/퍼즈
    public void PauseVideo(bool pressed)
    {
        if (pressed)
        {
            _currentVideoPlayer.Play();
        }
        else
        {
            _currentVideoPlayer.Pause();
        }
    }
    
    // 모든 비디어 플레이어에 자동추가돼있음
    // 비디오에 오류가 있을경우, 프로그램 종료
    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"Video Error! 메시지: {message}, 파일: {source}");
        
#if UNITY_EDITOR 
        // 에디터에서 실행 중이라면 Play 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // 빌드된 실행 파일이라면 종료
    // Application.Quit();
#endif
    }
    
    // 초기화
    // 이거만 호출하면 초기화 완벽하게 될 수 있게 작성해놓음
    private void GoTitle(VideoPlayer source) => GoTitleInternal();
    public void GoTitle() => GoTitleInternal();
    private void GoTitleInternal(VideoPlayer source = null)
    {
        Debug.Log(source ? $"GoTitle By {source}" : "GoTitle By Function");
        ChangeNextVideo(0, 0);
        CurrentPage = 0;
        _renderTextureIndex = 0;
        _currentRenderTexture = _renderTexture[0];
        _notAllowedToChangeControl.Clear();
        PlayNextMainVideo();
        pageController.GoTitle();
        InitializeRenderTextureObj();
    }
    
    // 다음 비디오 재생
    public void PlayNextMainVideo()
    {
        _inputManager.AcceptInput = false;
        _nextVideoPlayer.Play();
        Debug.Log($"Play Video : {_nextVideoPlayer}");
        _nextVideoPlayer.targetTexture = _currentRenderTexture;
        
        if (_loopAddedVideo.ContainsKey(_currentVideoPlayer))
        {
            int page = _loopAddedVideo[_currentVideoPlayer] / 100;
            int index = _loopAddedVideo[_currentVideoPlayer] % 100;
            
            VideoPlayer player = videoPlayers[page].loopPlayers[index];

            if (player.targetTexture)
            {
                player.targetTexture = null;
            }
            
            player.Stop();
        }
        
        _currentVideoPlayer.targetTexture = null;
        _currentVideoPlayer.Stop();
        Debug.Log($"Stop Video : {_currentVideoPlayer}");
            
        _currentVideoPlayer = _nextVideoPlayer;
        
        // 다음 비디오 prepare
        PrepareNextVideo(_currentVideoPlayer);
    }
    
    // 다른 RenderTexture에 비디오 재생
    // 현재 비디오를 stop하진않음
    public void PlayNextVideoOnOtherPage()
    {
        _inputManager.AcceptInput = false;
        Debug.Log("MoviePlayer Page Index : " +  NextPage);
        _renderTextureObjToCloseLater = _renderTextureObj[_renderTextureIndex];
        ++_renderTextureIndex;
        _playerToStopLater = _currentVideoPlayer;
        _nextVideoPlayer.Play();
        _nextVideoPlayer.targetTexture = _renderTexture[_renderTextureIndex];
        _currentRenderTexture = _renderTexture[_renderTextureIndex];
        _renderTextureObj[_renderTextureIndex].SetActive(true);
        if (_loopAddedVideo.ContainsKey(_currentVideoPlayer))
        {
            int page = _loopAddedVideo[_currentVideoPlayer] / 100;
            int index = _loopAddedVideo[_currentVideoPlayer] % 100;
            
            VideoPlayer player = videoPlayers[page].loopPlayers[index];

            if (player.targetTexture)
            {
                player.targetTexture = null;
            }
            
            player.Stop();
        }
        
        _currentVideoPlayer = _nextVideoPlayer;
        
        ++NextPage;
        
        PrepareNextVideo(_currentVideoPlayer);
        
    }
    
    // Fading 같은 전환효과를 줄때, 비디오를 잠시 두개 재생해야할 경우가 생김
    // 그 때, 전환효과가 끝나면 이거로 이전 비디오 끄고, 게임오브젝트 비활성화함
    private void CloseSavedObj()
    {
        _renderTextureObjToCloseLater.SetActive(false);
        _playerToStopLater.Stop();
        _playerToStopLater.targetTexture = null;
        
        if (_loopAddedVideo.ContainsKey(_playerToStopLater))
        {
            int page = _loopAddedVideo[_playerToStopLater] / 100;
            int index = _loopAddedVideo[_playerToStopLater] % 100;
            
            VideoPlayer player = videoPlayers[page].loopPlayers[index];

            if (player.targetTexture)
            {
                player.targetTexture = null;
            }
            
            player.Stop();
        }
    }

    // 비디오를 갑자기 바꿔야 할 때 실행
    public void PlayMediumVideo(int page, int index)
    {
        StopAllVideo();
        RemoveOrAddVideosOnHashSet(page);
        
        videoPlayers[page].mainPlayers[index].Play();
        videoPlayers[page].mainPlayers[index].targetTexture = _currentRenderTexture;
        
        PrepareNextVideo(videoPlayers[page].mainPlayers[index]);
    }
    
    // 현재 비디오가 끝나고 재생될 비디오 변경
    public void ChangeNextVideo(int page, int index)
    {
        if (videoPlayers[page].mainPlayers[index] == _nextVideoPlayer) return;
        if (_currentVideoPlayer != videoPlayers[page].mainPlayers[index])
        {
            _nextVideoPlayer.Stop();
        }
        
        RemoveOrAddVideosOnHashSet(page);
        
        _nextVideoPlayer = videoPlayers[page].mainPlayers[index];
        _nextVideoPlayer.Prepare();
    }
    
    // 비디오 강제 초기화
    public void ResetVideo()
    {
        StopAllVideo();

        _notAllowedToChangeControl.Clear();
        videoPlayers[0].mainPlayers[0].Play();
        videoPlayers[0].mainPlayers[0].targetTexture = _renderTexture[0];
        
        _currentRenderTexture = _renderTexture[0];
        _renderTextureIndex = 0;
        CurrentPage = 0;
        _currentIndex = 0;
        
        InitializeRenderTextureObj();
        
        PrepareNextVideo(videoPlayers[0].mainPlayers[0]);
    }

    // 렌더텍스쳐 색상 Color로 변경
    /*public void ChangeTextureToSingleColor(Color color)
    {
        _currentVideoPlayer.Stop();
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = _currentRenderTexture;
        GL.Clear(false,true, color);
        RenderTexture.active = rt;
    }*/


    
    // 모든 비디오 정지
    private void StopAllVideo()
    {
        if (videoPlayers == null || videoPlayers.Length == 0)
        {
            Debug.LogWarning("videoPlayers가 비어 있습니다.");
            return;
        }

        foreach (var v in videoPlayers)
        {
            if (v == null) continue;

            StopVideoArray(v.mainPlayers);
            StopVideoArray(v.loopPlayers);
        }
    }
    
    #region 여기도 안읽어도됨. 인스펙터 전용 함수
    
    private void OnValidate()
    {
        if (videoPlayers == null)
            return;

        // videoPlayers 배열의 각 요소에 대해 ValidateArrays() 호출
        foreach (var v in videoPlayers)
        {
            v?.ValidateArrays();
        }
    }
    
    #endregion

}
