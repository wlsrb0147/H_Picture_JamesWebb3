using System;
using System.Collections;
using UnityEngine;
using Debug = DebugEx;

public class GPUVideoPlayer : MonoBehaviour {
    public enum State { Idle, Loaded, Playing, Paused, Stopped, Ended, Failed }

    public State currentState { get; private set; }
    public event Action<State> onStateChanged;

    Description m_Desc;
    Texture2D   m_Texture;
    IntPtr      m_NativeTex;
    Plugin.StateChangedCallback m_Callback;

    void Awake() {
        // 네이티브 초기화 및 콜백 등록
        m_Callback = new Plugin.StateChangedCallback(OnStateChanged);
        Plugin.CreateMediaPlayback(m_Callback);
    }

    public void Load(string path) {
        Plugin.LoadContent(path);
    }

    public bool Play() {
        // ① 로드 완료 전엔 실행 금지
        if (currentState != State.Loaded) {
            Debug.LogError("Play 호출 시점: 비디오가 아직 로드되지 않았습니다.");
            return false;
        }
        // ② 네이티브 Play 호출
        if (Plugin.Play() != 0) {
            Debug.LogError("네이티브 Play 실패");
            return false;
        }
        // ③ 텍스처 미생성 시에만 생성
        if (m_Texture == null) {
            long hr = Plugin.CreatePlaybackTexture(m_Desc.width, m_Desc.height, out m_NativeTex);
            if (hr != 0 || m_NativeTex == IntPtr.Zero) {
                Debug.LogError($"CreatePlaybackTexture 실패: hr={hr}, nativeTex={m_NativeTex}");
                return false;
            }
            m_Texture = Texture2D.CreateExternalTexture(
                (int)m_Desc.width, (int)m_Desc.height,
                TextureFormat.RGBA32, false, false, m_NativeTex
            );
        }
        ChangeState(State.Playing);
        return true;
    }

    public bool Pause() {
        if (Plugin.Pause() == 0) {
            ChangeState(State.Paused);
            return true;
        }
        return false;
    }

    public bool Stop() {
        if (Plugin.Stop() == 0) {
            ChangeState(State.Stopped);
            return true;
        }
        return false;
    }

    IEnumerator Start() {
        while (true) {
            yield return new WaitForEndOfFrame();
            Plugin.SetTimeFromUnity(Time.timeSinceLevelLoad);
            GL.IssuePluginEvent(Plugin.GetRenderEventFunc(), 1);
        }
    }

    void OnStateChanged(Plugin.StateChangedMessage msg) {
        switch (msg.type) {
            case 1: // Opened
                m_Desc = msg.description;
                ChangeState(State.Loaded);
                break;
            case 2: // Failed
                ChangeState(State.Failed);
                break;
            case 3: // StateChanged
                var playbackState = (Plugin.PlaybackState)msg.state;
                if (playbackState == Plugin.PlaybackState.Playing) {
                    ChangeState(State.Playing);
                }
                else if (playbackState == Plugin.PlaybackState.Paused) {
                    ChangeState(State.Paused);
                }
                else if (playbackState == Plugin.PlaybackState.Ended) {
                    ChangeState(State.Ended);
                }
                break;
        }
    }

    void ChangeState(State s) {
        currentState = s;
        onStateChanged?.Invoke(s);
    }

    void OnDisable() {
        Plugin.ReleaseMediaPlayback();
    }

    // 외부에서 텍스처 얻기
    public Texture2D MediaTexture => m_Texture;
    public Description MediaDescription => m_Desc;
}
