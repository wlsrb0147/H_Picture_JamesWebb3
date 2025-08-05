using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class DebugExAliasInjector
{
    static DebugExAliasInjector()
    {
        var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (var path in files)
        {
            var lines = File.ReadAllLines(path).ToList();

            // 이미 alias가 선언되어 있으면 스킵
            if (lines.Any(l => l.Contains("using Debug = DebugEx;")))
                continue;

            // "using UnityEngine;" 찾기
            int unityIndex = lines.FindIndex(line => line.Trim() == "using UnityEngine;");
            if (unityIndex == -1) continue; // UnityEngine이 없으면 스킵

            // 바로 다음 줄에 삽입
            lines.Insert(unityIndex + 1, "using Debug = DebugEx;");
            File.WriteAllLines(path, lines);
        }

        AssetDatabase.Refresh();
    }
}