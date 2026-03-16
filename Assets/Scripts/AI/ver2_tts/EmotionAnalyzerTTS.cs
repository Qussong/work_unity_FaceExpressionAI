using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 감정 분석 + TTS 응답 데이터
/// </summary>
[Serializable]
public class EmotionTTSResponse
{
    public int emotion;      // 1=기쁨, 2=슬픔, 3=화남, 4=놀람
    public string response;  // AI 응답 메시지
    public string audio;     // base64 인코딩된 MP3 오디오
    public string error;     // 전체 오류 메시지
    public string tts_error; // TTS만 실패했을 때의 오류 메시지
}

/// <summary>
/// 감정 분석 + TTS 통합 클래스 (v2)
/// - /analyze-with-tts : 텍스트 입력 → 감정 분석 + TTS 음성 반환
/// - /analyze          : 텍스트 입력 → 감정 분석만 (음성 없음)
/// </summary>
public class EmotionAnalyzerTTS : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://localhost:5000";

    [Header("오디오")]
    [SerializeField] private AudioSource audioSource;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    /// <summary>감정 분석 완료 시 이벤트 (TTS 재생 전에 발행)</summary>
    public event Action<EmotionTTSResponse> OnAnalysisComplete;

    /// <summary>TTS 오디오 재생 완료 시 이벤트</summary>
    public event Action OnAudioPlayComplete;

    private void Awake()
    {
        // AudioSource가 없으면 자동 추가
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>감정 코드를 한국어 문자열로 변환</summary>
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
    /// 텍스트를 분석하고 TTS 음성까지 재생한다 (메인 사용 메서드)
    /// OnAnalysisComplete 이벤트 발행 후 오디오 재생을 이어서 수행한다
    /// </summary>
    public void AnalyzeAndSpeak(string text, Action<EmotionTTSResponse> callback = null)
    {
        StartCoroutine(AnalyzeWithTTSCoroutine(text, callback));
    }

    /// <summary>감정 분석만 수행 (TTS 재생 없음)</summary>
    public void AnalyzeOnly(string text, Action<EmotionTTSResponse> callback = null)
    {
        StartCoroutine(AnalyzeCoroutine(text, callback));
    }

    /// <summary>현재 재생 중인 오디오를 정지한다</summary>
    public void StopAudio()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    /// <summary>오디오 재생 여부</summary>
    public bool IsPlaying => audioSource.isPlaying;

    #region 코루틴

    /// <summary>/analyze-with-tts 엔드포인트에 요청하여 감정 분석 + TTS 재생</summary>
    private IEnumerator AnalyzeWithTTSCoroutine(string text, Action<EmotionTTSResponse> callback)
    {
        if (string.IsNullOrEmpty(text))
        {
            var errorResponse = new EmotionTTSResponse { error = "텍스트가 비어있습니다" };
            callback?.Invoke(errorResponse);
            OnAnalysisComplete?.Invoke(errorResponse);
            yield break;
        }

        var requestData = new EmotionRequest { text = text };
        string jsonData = JsonUtility.ToJson(requestData);

        if (showDebugLog)
            Debug.Log($"[EmotionAnalyzerTTS] 요청: {jsonData}");

        string url = $"{serverUrl}/analyze-with-tts";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            EmotionTTSResponse response;

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (showDebugLog)
                    Debug.LogError($"[EmotionAnalyzerTTS] 오류: {request.error}");

                response = new EmotionTTSResponse { error = request.error };
                callback?.Invoke(response);
                OnAnalysisComplete?.Invoke(response);
                yield break;
            }

            if (showDebugLog)
                Debug.Log("[EmotionAnalyzerTTS] 응답 수신 (오디오 데이터 포함)");

            try
            {
                response = JsonUtility.FromJson<EmotionTTSResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                response = new EmotionTTSResponse { error = $"JSON 파싱 실패: {e.Message}" };
                callback?.Invoke(response);
                OnAnalysisComplete?.Invoke(response);
                yield break;
            }

            // 분석 결과 이벤트 발행 (오디오 재생 전)
            callback?.Invoke(response);
            OnAnalysisComplete?.Invoke(response);

            // TTS 오디오 재생
            if (!string.IsNullOrEmpty(response.audio))
            {
                yield return StartCoroutine(PlayAudioFromBase64(response.audio));
            }
            else if (!string.IsNullOrEmpty(response.tts_error))
            {
                Debug.LogWarning($"[EmotionAnalyzerTTS] TTS 오류: {response.tts_error}");
            }
        }
    }

    /// <summary>/analyze 엔드포인트에 요청하여 감정 분석만 수행</summary>
    private IEnumerator AnalyzeCoroutine(string text, Action<EmotionTTSResponse> callback)
    {
        if (string.IsNullOrEmpty(text))
        {
            var errorResponse = new EmotionTTSResponse { error = "텍스트가 비어있습니다" };
            callback?.Invoke(errorResponse);
            yield break;
        }

        var requestData = new EmotionRequest { text = text };
        string jsonData = JsonUtility.ToJson(requestData);
        string url = $"{serverUrl}/analyze";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            EmotionTTSResponse response;

            if (request.result != UnityWebRequest.Result.Success)
            {
                response = new EmotionTTSResponse { error = request.error };
            }
            else
            {
                try
                {
                    response = JsonUtility.FromJson<EmotionTTSResponse>(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    response = new EmotionTTSResponse { error = $"JSON 파싱 실패: {e.Message}" };
                }
            }

            callback?.Invoke(response);
            OnAnalysisComplete?.Invoke(response);
        }
    }

    /// <summary>
    /// Base64 인코딩된 MP3 데이터를 임시 파일로 저장 후 AudioSource로 재생한다
    /// Unity는 MP3를 메모리에서 직접 로드할 수 없으므로 임시 파일 경유가 필요하다
    /// </summary>
    private IEnumerator PlayAudioFromBase64(string base64Audio)
    {
        if (showDebugLog)
            Debug.Log("[EmotionAnalyzerTTS] 오디오 재생 시작");

        byte[] audioBytes = Convert.FromBase64String(base64Audio);

        // 임시 파일에 저장 (UnityWebRequest로 로드하기 위해 필요)
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "tts_temp.mp3");
        System.IO.File.WriteAllBytes(tempPath, audioBytes);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();

                if (showDebugLog)
                    Debug.Log($"[EmotionAnalyzerTTS] 오디오 재생 중 (길이: {clip.length}초)");

                // 재생 완료까지 대기
                yield return new WaitForSeconds(clip.length);

                OnAudioPlayComplete?.Invoke();

                if (showDebugLog)
                    Debug.Log("[EmotionAnalyzerTTS] 오디오 재생 완료");
            }
            else
            {
                Debug.LogError($"[EmotionAnalyzerTTS] 오디오 로드 실패: {www.error}");
            }
        }
    }

    #endregion
}
