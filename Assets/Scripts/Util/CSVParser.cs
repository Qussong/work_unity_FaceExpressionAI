using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PagingTemplate.Util
{

/// <summary>
/// CSV 파일 파싱 유틸리티 (static)
///
/// 지원 CSV 형태 (2열 Key-Value) :
///     Key, Value
///     데이터 이름1, 데이터1
///     데이터 이름2, 데이터2
///
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// StreamingAssets 폴더 내 CSV 파일을 읽어 Dictionary로 반환
    /// </summary>
    /// <param name="fileName">파일명 (예: "StartData.csv")</param>
    /// <returns>파싱된 key-value Dictionary (실패 시 빈 Dictionary)</returns>
    public static Dictionary<string, string> Read(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        return ParseFile(path);
    }

    /// <summary>
    /// 지정 경로의 CSV 파일을 파싱
    /// - 0번 줄(헤더) 스킵
    /// - 첫 번째 쉼표 기준으로 key/value 분리 (value에 쉼표 허용)
    /// - 중복 키는 최초 값 유지
    /// </summary>
    private static Dictionary<string, string> ParseFile(string path)
    {
        var result = new Dictionary<string, string>();

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[CSVParser] 파일 없음: {path}");
            return result;
        }

        try
        {
            string[] lines = File.ReadAllLines(path);

            // 헤더(0번 줄) 스킵 → 1번부터 시작
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 첫 번째 쉼표 기준 분리 (value에 쉼표 포함 대응)
                int commaIndex = line.IndexOf(',');
                if (commaIndex < 0) continue;

                string key = line.Substring(0, commaIndex).Trim();
                string value = line.Substring(commaIndex + 1).Trim();

                // 빈 키 스킵
                if (string.IsNullOrEmpty(key)) continue;
                // 중복 키는 최초 값 유지
                if (!result.ContainsKey(key))
                    result.Add(key, value);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CSVParser] 파싱 실패 ({path}): {e.Message}");
        }

        return result;
    }
}

} // namespace PagingTemplate.Util
