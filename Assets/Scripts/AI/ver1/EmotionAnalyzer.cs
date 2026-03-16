using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 감정 분석 API 응답 데이터
/// </summary>
[Serializable]
public class EmotionResponse
{
    public int emotion;      // 1=기쁨, 2=슬픔, 3=화남, 4=놀람
    public string response;  // AI 응답 메시지
    public string error;     // 에러 메시지 (있을 경우)
}

/// <summary>
/// 감정 분석 요청 데이터
/// </summary>
[Serializable]
public class EmotionRequest
{
    public string text;
}

/// <summary>
/// Python 서버와 통신하여 감정 분석을 수행하는 클래스
/// </summary>
public class EmotionAnalyzer : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://localhost:5000/analyze";
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;
    
    /// <summary>
    /// 감정 분석 완료 시 호출되는 이벤트
    /// </summary>
    public event Action<EmotionResponse> OnAnalysisComplete;
    
    /// <summary>
    /// 감정 코드를 문자열로 변환
    /// </summary>
    public static string GetEmotionName(int emotionCode)
    {
        return emotionCode switch
        {
            1 => "기쁨",
            2 => "슬픔",
            3 => "화남",
            4 => "놀람",
            _ => "알 수 없음"
        };
    }
    
    /// <summary>
    /// 텍스트를 분석하여 감정을 반환 (코루틴)
    /// </summary>
    public void Analyze(string text, Action<EmotionResponse> callback = null)
    {
        StartCoroutine(AnalyzeCoroutine(text, callback));
    }
    
    private IEnumerator AnalyzeCoroutine(string text, Action<EmotionResponse> callback)
    {
        if (string.IsNullOrEmpty(text))
        {
            var errorResponse = new EmotionResponse { error = "텍스트가 비어있습니다" };
            callback?.Invoke(errorResponse);
            OnAnalysisComplete?.Invoke(errorResponse);
            yield break;
        }
        
        // 요청 데이터 생성
        var requestData = new EmotionRequest { text = text };
        string jsonData = JsonUtility.ToJson(requestData);
        
        if (showDebugLog)
            Debug.Log($"[EmotionAnalyzer] 요청: {jsonData}");
        
        // HTTP POST 요청
        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            EmotionResponse response;
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                if (showDebugLog)
                    Debug.LogError($"[EmotionAnalyzer] 오류: {request.error}");
                
                response = new EmotionResponse { error = request.error };
            }
            else
            {
                string responseText = request.downloadHandler.text;
                
                if (showDebugLog)
                    Debug.Log($"[EmotionAnalyzer] 응답: {responseText}");
                
                try
                {
                    response = JsonUtility.FromJson<EmotionResponse>(responseText);
                }
                catch (Exception e)
                {
                    response = new EmotionResponse { error = $"JSON 파싱 실패: {e.Message}" };
                }
            }
            
            // 콜백 호출
            callback?.Invoke(response);
            OnAnalysisComplete?.Invoke(response);
        }
    }
}
