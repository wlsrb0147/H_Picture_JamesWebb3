using UnityEngine;
using Debug = DebugEx;
using UnityEngine.UI;

public class VideoDisplay : MonoBehaviour {
    public GPUVideoPlayer player;
    public RawImage display;

    void Start() {
        // ① 콜백 등록 (Load() 이전)
        player.onStateChanged += OnPlayerStateChanged;

        // ② 비디오 로드 시작 (네이티브가 Opened 메시지 보내면 Loaded 상태로 전환됨)
        player.Load(Application.streamingAssetsPath + "/Videos/000_ML.mp4");
    }

    void OnPlayerStateChanged(GPUVideoPlayer.State state) {
        switch (state) {
            case GPUVideoPlayer.State.Loaded:
                // 로드 완료 후에만 Play 호출
                player.Play();
                break;

            case GPUVideoPlayer.State.Playing:
                // Play가 성공해서 Playing 상태가 되면 텍스처 바인딩
                display.texture = player.MediaTexture;
                break;
        }
    }

    void OnDestroy() {
        // 메모리 누수 방지: 콜백 해제
        player.onStateChanged -= OnPlayerStateChanged;
    }
}
