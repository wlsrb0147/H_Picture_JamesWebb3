using System;
using System.Runtime.InteropServices;

public static class Plugin {
    // 네이티브에서 보낼 메시지 구조체
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct StateChangedMessage {
        [FieldOffset(0)] public ushort type;
        [FieldOffset(4)] public ushort state;
        [FieldOffset(4)] public long   hresult;
        [FieldOffset(4)] public Description description;
        [FieldOffset(4)] public long   position;
    }
    public delegate void StateChangedCallback(StateChangedMessage msg);
    
    // **추가**: 네이티브 PlaybackState 코드 매핑
    public enum PlaybackState : ushort {
        None      = 0,
        Opening   = 1,
        Buffering = 2,
        Playing   = 3,
        Paused    = 4,
        Ended     = 5
    }

    // 네이티브 함수 불러오기
    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long CreateMediaPlayback(StateChangedCallback callback);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern void ReleaseMediaPlayback();

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long CreatePlaybackTexture(uint width, uint height, out IntPtr nativeTex);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long LoadContent([MarshalAs(UnmanagedType.BStr)] string sourceURL);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long Play();

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long Pause();

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long Stop();

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long GetDuration(out long duration);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long GetPosition(out long position);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long GetPlaybackRate(out double rate);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long SetPlaybackRate(double rate);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern long SetPosition(long position);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern void SetTimeFromUnity(float t);

    [DllImport("MediaPlayback", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetRenderEventFunc();
}

// Description 구조체 (비디오 메타데이터)
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Description {
    public uint  width;
    public uint  height;
    public long  duration;
    public byte  isSeekable;
}
