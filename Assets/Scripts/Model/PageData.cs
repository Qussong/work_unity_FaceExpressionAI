using System.Collections.Generic;

namespace PagingTemplate.Model
{

/// <summary>
/// View 하나에 대응하는 데이터 묶음
/// CSV에서 읽어온 string key-value 쌍을 보관
/// </summary>
public class PageData
{
    private Dictionary<string, string> _data = new Dictionary<string, string>();

    #region 외부 호출 함수

    /// <summary>
    /// string 값 조회
    /// </summary>
    /// <param name="key">CSV의 Key 값</param>
    /// <param name="defaultValue">키가 없을 경우 반환할 기본값</param>
    public string Get(string key, string defaultValue = "")
    {
        return _data.TryGetValue(key, out string value) ? value : defaultValue;
    }

    /// <summary>
    /// 플래그 값 조회 ("true"/"1" → true, 그 외 → false)
    /// </summary>
    /// <param name="key">CSV의 Key 값</param>
    /// <param name="defaultValue">키가 없을 경우 반환할 기본값</param>
    public bool GetFlag(string key, bool defaultValue = false)
    {
        if (!_data.TryGetValue(key, out string value)) return defaultValue;
        return value == "1" || value.ToLower() == "true";
    }

    /// <summary>
    /// 키 존재 여부 확인
    /// </summary>
    public bool Has(string key) => _data.ContainsKey(key);

    /// <summary>
    /// Dictionary로부터 일괄 세팅 (DataRepository에서 호출)
    /// </summary>
    public void SetFromDictionary(Dictionary<string, string> source)
    {
        foreach (var kvp in source)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }

    #endregion
}

} // namespace PagingTemplate.Model
