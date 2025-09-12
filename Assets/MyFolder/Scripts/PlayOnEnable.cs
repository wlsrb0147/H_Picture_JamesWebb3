using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Debug = DebugEx;

public class PlayOnEnable : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture renderTexture;

    private void Awake()
    {
        videoPlayer.started += VideoPlayerOnstarted;
        videoPlayer.loopPointReached += source =>
        {
            gameObject.SetActive(false);
        };
    }

    private void VideoPlayerOnstarted(VideoPlayer source)
    {
        StartCoroutine(DelayLittle());
        Invoke(nameof(SetObjectOff),1f);
    }

    private void SetObjectOff()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator DelayLittle()
    {
        yield return new WaitForSeconds(0.5f);
        PageController.Instance.LoadNextPage();
    }

    private void OnEnable()
    {
        videoPlayer.Play();
    }

    private void OnDisable()
    {
        videoPlayer.Stop();
        renderTexture.Release();
    }
}
