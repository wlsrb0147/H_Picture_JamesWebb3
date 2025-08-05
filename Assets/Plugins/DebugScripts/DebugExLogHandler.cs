using Debug = DebugEx;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

public class DebugExLogHandler : ILogHandler
{
    private readonly ILogHandler _defaultHandler = UnityEngine.Debug.unityLogger.logHandler;

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        string message = string.Format(format, args);
        string time    = DateTime.Now.ToString("HH:mm:ss");
        string level   = logType == LogType.Warning ? "[Warning]" :
                         logType == LogType.Error   ? "[Error]"   : "[Log]";
        string prefix  = $"[{time}] {level}";
        _defaultHandler.LogFormat(logType, context, "{0} {1}", prefix, message);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        // 1) 시간·레벨 프리픽스
        string time   = DateTime.Now.ToString("HH:mm:ss");
        string prefix = $"[{time}] [Error]";

        // 2) 예외 메시지 헤더
        var sb = new StringBuilder();
        sb.AppendLine($"{exception.GetType().Name}: {exception.Message}");

        // 3) 스택 프레임만 뽑아서 파싱 (파일/줄 정보 제외)
        var st = new StackTrace(exception, false);
        foreach (var frame in st.GetFrames() ?? Enumerable.Empty<StackFrame>())
        {
            var m = frame.GetMethod();
            var decl = m.DeclaringType;
            if (decl == null) 
                continue;

            // Unity 내부 또는 핸들러 자기 자신은 제외
            if (decl == typeof(DebugExLogHandler) ||
                decl.FullName.StartsWith("UnityEngine"))
                continue;

            // 파라미터 타입·이름 목록 생성
            var ps = m.GetParameters()
                .Select(p => $"{p.ParameterType.Name} {p.Name}");
            string paramList = string.Join(", ", ps);

            // "  at Class.Method (Type name)" 형태로 추가
            sb.AppendLine($"  at {decl.Name}.{m.Name} ({paramList})");
        }

        // 4) 한 번에 출력
        _defaultHandler.LogFormat(LogType.Error, context, "{0} {1}", prefix, sb.ToString());
    }
}
