using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CSV 파일 파서 싱글톤
/// StreamingAssets 폴더에서 CSV 파일을 읽어 딕셔너리로 저장한다
///
/// CSV 파일 형식:
///     헤더행 (첫 번째 행은 무시)
///     키, 값
///     키, 값
///     ...
/// </summary>
public class CSVParser : MonoSingleton<CSVParser>
{
    protected override void OnSingletonAwake() { }

    protected override void OnSingletonApplicationQuit() { }

    protected override void OnSingletonDestroy() { }

    #region 내부 처리 메서드

    /// <summary>
    /// 절대 경로의 CSV 파일을 읽어 딕셔너리에 저장한다
    /// 첫 번째 행(헤더)은 건너뛰고, 쉼표로 분리된 첫 두 열을 키/값으로 사용한다
    /// </summary>
    private bool ReadFile(string path, Dictionary<string, string> container)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using (StreamReader sr = File.OpenText(path))
            {
                string openText = sr.ReadToEnd();

                // Windows(\r\n)와 Unix(\n) 개행 모두 처리
                string[] lines = openText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i++)
                {
                    // 첫 번째 행(헤더) 건너뜀
                    if (i < 1) continue;

                    string line = lines[i];

                    // 공백만 있는 행 건너뜀
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] columns = line.Split(',');

                    // 최소 2열(키, 값) 필요
                    if (columns.Length < 2) continue;

                    string key = columns[0].Trim();
                    string value = columns[1].Trim();

                    // 중복 키는 첫 번째 값을 유지
                    if (!container.ContainsKey(key))
                        container.Add(key, value);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>
    /// StreamingAssets 폴더의 CSV 파일을 읽어 딕셔너리에 저장한다
    /// </summary>
    /// <param name="fileName">파일명 (예: "config.csv")</param>
    /// <param name="dataContainer">결과를 저장할 딕셔너리</param>
    /// <returns>읽기 성공 여부</returns>
    public bool ReadCSVFile(string fileName, Dictionary<string, string> dataContainer)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        return ReadFile(path, dataContainer);
    }

    #endregion
}
