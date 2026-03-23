using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PagingTemplate.Util;
using PagingTemplate.View;

namespace PagingTemplate.Model
{

/// <summary>
/// DataConfig.json → CSV 로딩 → PageData 변환 → View 타입별 관리
///
/// DataConfig.json 형식:
///     { "views": [
///         { "viewType": "StartView", "csvFiles": ["StartData.csv"] },
///         ...
///     ]}
///
/// 사용법:
///     var repo = new DataRepository();
///     PageData data = repo.GetData<ContentView>();
///     string title = data.Get("Title");
/// </summary>
public class DataRepository
{
    private const string ConfigFileName = "DataConfig.json";

    // View 타입 → PageData 매핑
    private Dictionary<Type, PageData> _dataMap = new Dictionary<Type, PageData>();

    public DataRepository()
    {
        LoadAll();
    }

    #region JSON 설정 모델

    [Serializable]
    private class DataConfig
    {
        public ViewEntry[] views;
    }

    [Serializable]
    private class ViewEntry
    {
        public string viewType;
        public string[] csvFiles;
    }

    #endregion

    #region 내부 호출 함수

    /// <summary>
    /// DataConfig.json을 읽어 모든 View에 대응하는 CSV 로딩
    /// </summary>
    private void LoadAll()
    {
        // 1. StreamingAssets에서 DataConfig.json 경로 조합 및 존재 확인
        string configPath = Path.Combine(Application.streamingAssetsPath, ConfigFileName);

        if (!File.Exists(configPath))
        {
            Debug.LogError($"[DataRepository] 설정 파일 없음: {configPath}");
            return;
        }

        // 2. JSON 파일 읽기 → DataConfig 객체로 역직렬화
        string json = File.ReadAllText(configPath);
        DataConfig config = JsonUtility.FromJson<DataConfig>(json);

        if (config?.views == null)
        {
            Debug.LogError("[DataRepository] DataConfig.json 파싱 실패");
            return;
        }

        // 3. 각 ViewEntry를 순회하며 viewType 문자열 → Type 변환 후 CSV 로딩
        foreach (var entry in config.views)
        {
            Type viewType = FindType(entry.viewType);
            if (viewType == null)
            {
                Debug.LogWarning($"[DataRepository] 타입을 찾을 수 없음: {entry.viewType}");
                continue;
            }

            _dataMap[viewType] = LoadPageData(entry.csvFiles);
        }
    }

    /// <summary>
    /// 타입 이름으로 모든 로드된 어셈블리에서 타입 검색
    /// (네임스페이스 유무와 관계없이 동작)
    /// </summary>
    private Type FindType(string typeName)
    {
        // 풀네임으로 먼저 시도
        Type type = Type.GetType(typeName);
        if (type != null) return type;

        // 모든 어셈블리에서 검색
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null) return type;
        }

        return null;
    }

    /// <summary>
    /// 여러 CSV 파일 → PageData 변환 (후순위 파일이 키 충돌 시 덮어씀)
    /// </summary>
    private PageData LoadPageData(string[] fileNames)
    {
        var pageData = new PageData();

        foreach (string fileName in fileNames)
        {
            var raw = CSVParser.Read(fileName);
            pageData.SetFromDictionary(raw);
        }

        return pageData;
    }

    #endregion

    #region 외부 호출 함수

    /// <summary>
    /// View 타입에 대응하는 PageData 조회
    /// </summary>
    /// <typeparam name="TView">BaseView를 상속한 View 타입</typeparam>
    /// <returns>해당 View의 PageData (미등록 시 null)</returns>
    public PageData GetData<TView>() where TView : BaseView
    {
        _dataMap.TryGetValue(typeof(TView), out PageData data);
        return data;
    }

    #endregion
}

} // namespace PagingTemplate.Model
