using Debug = DebugEx;
// DebugExInitializer.cs
using UnityEngine;

public static class DebugExInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Init()
    {
        // 에디터가 아닐 때만 활성화
        if (Application.isEditor)
            return;

        global::UnityEngine.Debug.unityLogger.logHandler = new DebugExLogHandler();
        global::UnityEngine.Debug.unityLogger.logEnabled = true;
    }
}