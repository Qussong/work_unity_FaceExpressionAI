using System;
using System.Collections;
using System.Text;
using PagingTemplate.Util;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Clova STT, OpenAI 감정분석, Clova TTS API를 직접 호출하는 서비스 클래스
/// Python 서버 없이 Unity에서 전체 파이프라인을 처리한다
/// </summary>
public class AIService : MonoBehaviour
{
    private string _openAIApiKey = "";
    private string _naverClientId = "";
    private string _naverClientSecret = "";

    [Header("설정")]
    [SerializeField] private int _timeoutSeconds = 30;
    [SerializeField] private bool _showDebugLog = true;

    // 감정코드 → Clova TTS 감정 파라미터 매핑 (감정코드, 클로바보이스 감정코드)
    private static readonly System.Collections.Generic.Dictionary<int, int> EMOTION_TO_CLOVA = new()
    {
        { 0, 0 },  // 중립 → Clova 중립(0)
        { 1, 2 },  // 기쁨 → Clova 기쁨(2)
        { 2, 1 },  // 슬픔 → Clova 슬픔(1)
        { 3, 3 },  // 화남 → Clova 분노(3)
        { 4, 2 },  // 놀람 → Clova 기쁨(2) — Clova에 놀람 없어서 높은 톤의 기쁨으로 대체
    };

    private const string SYSTEM_PROMPT =
        "당신은 감정 분석 전문가입니다.\n" +
        "사용자의 말을 분석하여 반드시 아래 JSON 형식으로만 응답하세요.\n" +
        "JSON 외에 다른 텍스트는 절대 포함하지 마세요.\n\n" +
        "{\"emotion\": 감정코드(정수), \"response\": \"짧은 공감 응답\"}\n\n" +
        "감정코드:\n" +
        "1 = 기쁨 (행복, 즐거움, 만족, 설렘 등)\n" +
        "2 = 슬픔 (우울, 외로움, 실망, 후회 등)\n" +
        "3 = 화남 (분노, 짜증, 불만, 억울함 등)\n" +
        "4 = 놀람 (충격, 당황, 의외, 신기함 등)\n\n" +
        "규칙:\n" +
        "- emotion은 반드시 1, 2, 3, 4 중 하나의 정수\n" +
        "- response는 20자 내외로 짧고 따뜻하게\n" +
        "- 복합 감정일 경우 가장 강한 감정 하나만 선택";

    // OpenAI API 응답 역직렬화용 클래스
    [Serializable] private class OpenAIChatResponse { public OpenAIChoice[] choices; }
    [Serializable] private class OpenAIChoice { public OpenAIMessage message; }
    [Serializable] private class OpenAIMessage { public string content; }
    [Serializable] private class EmotionAnalysisResult { public int emotion; public string response; }
    [Serializable] private class ClovaSTTResponse { public string text; }

    private void Awake()
    {
        LoadApiKeysFromCSV();
    }

    /// <summary>StreamingAssets/config.csv에서 API 키를 읽어온다</summary>
    private void LoadApiKeysFromCSV()
    {
        var config = CSVParser.Read("config.csv");

        if (config.TryGetValue("OpenAIApiKey", out var openAI)) _openAIApiKey = openAI;
        if (config.TryGetValue("NaverClientId", out var naverId)) _naverClientId = naverId;
        if (config.TryGetValue("NaverClientSecret", out var naverSecret)) _naverClientSecret = naverSecret;

        if (_showDebugLog) Debug.Log("[AIService] config.csv에서 API 키 로드 완료");
    }

    #region 외부 호출 함수

    /// <summary>
    /// 전체 파이프라인 실행: WAV 바이트 → STT → 감정분석 → TTS → VoiceProcessResponse
    /// </summary>
    public IEnumerator ProcessVoice(byte[] wavData, Action<VoiceProcessResponse> onComplete)
    {
        var response = new VoiceProcessResponse();

        // 1. STT — 음성을 텍스트로 변환
        if (_showDebugLog) Debug.Log("[AIService] STT 요청 시작");

        string recognizedText = null;  // STT 인식 결과 텍스트
        string sttError = null;        // STT 오류 메시지 (성공 시 null)
        yield return StartCoroutine(CallClovaSTT(wavData, (text, error) =>
        {
            recognizedText = text;
            sttError = error;
        }));

        // STT API 호출 자체가 실패한 경우 (네트워크 오류, 타임아웃 등)
        if (sttError != null)
        {
            response.error = sttError;
            onComplete?.Invoke(response);
            yield break;
        }

        // STT는 성공했지만 음성이 인식되지 않은 경우 (무음, 잡음 등)
        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            response.error = "음성을 인식하지 못했습니다. 마이크를 확인하고 더 크게 말해주세요.";
            onComplete?.Invoke(response);
            yield break;
        }

        response.recognized_text = recognizedText;
        if (_showDebugLog) Debug.Log($"[AIService] STT 결과: {recognizedText}");

        // 2. 감정 분석 — OpenAI GPT
        if (_showDebugLog) Debug.Log("[AIService] 감정 분석 요청 시작");

        int emotionCode = 0;             // 감정 코드 (0=중립, 분석 완료 시 1~4로 변경)
        string responseText = null;       // GPT가 생성한 공감 응답 텍스트
        string analyzeError = null;       // 감정 분석 오류 메시지 (성공 시 null)
        yield return StartCoroutine(CallOpenAI(recognizedText, (emotion, resp, error) =>
        {
            emotionCode = emotion;
            responseText = resp;
            analyzeError = error;
        }));

        // 감정 분석 실패 시 파이프라인 중단
        if (analyzeError != null)
        {
            response.error = analyzeError;
            onComplete?.Invoke(response);
            yield break;
        }

        response.emotion = emotionCode;
        response.response = responseText;
        if (_showDebugLog) Debug.Log($"[AIService] 감정: {emotionCode}, 응답: {responseText}");

        // 3. TTS — 응답을 음성으로 변환
        if (_showDebugLog) Debug.Log("[AIService] TTS 요청 시작");

        string audioBase64 = null;  // TTS 결과 MP3 (base64 인코딩)
        string ttsError = null;     // TTS 오류 메시지 (성공 시 null)
        yield return StartCoroutine(CallClovaTTS(responseText, emotionCode, (audio, error) =>
        {
            audioBase64 = audio;
            ttsError = error;
        }));

        // TTS 실패해도 텍스트 결과는 반환 (음성만 없음)
        if (ttsError != null)
        {
            response.tts_error = ttsError;
        }
        else
        {
            response.audio = audioBase64;
        }

        if (_showDebugLog) Debug.Log("[AIService] 전체 파이프라인 완료");
        onComplete?.Invoke(response);
    }

    #endregion

    #region 내부 API 호출 함수

    /// <summary>Naver Clova STT API — WAV 바이트를 텍스트로 변환</summary>
    private IEnumerator CallClovaSTT(byte[] audioData, Action<string, string> onComplete)
    {
        string url = "https://naveropenapi.apigw.ntruss.com/recog/v1/stt?lang=Kor";

        if (_showDebugLog) Debug.Log($"[AIService] STT API 호출 - 오디오 크기: {audioData.Length} bytes");

        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(audioData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("X-NCP-APIGW-API-KEY-ID", _naverClientId);
            request.SetRequestHeader("X-NCP-APIGW-API-KEY", _naverClientSecret);
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (_showDebugLog) Debug.Log($"[AIService] STT 응답 수신 - 상태: {request.result}, 코드: {request.responseCode}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AIService] STT 실패 - {request.error}");
                onComplete?.Invoke(null, $"Clova STT 오류: {request.error}");
                yield break;
            }

            if (_showDebugLog) Debug.Log($"[AIService] STT 응답 본문: {request.downloadHandler.text}");

            try
            {
                var result = JsonUtility.FromJson<ClovaSTTResponse>(request.downloadHandler.text);
                onComplete?.Invoke(result.text, null);
            }
            catch (Exception e)
            {
                onComplete?.Invoke(null, $"STT 응답 파싱 실패: {e.Message}");
            }
        }
    }

    /// <summary>OpenAI GPT-4o-mini — 텍스트에서 감정 분류 + 공감 응답 생성</summary>
    private IEnumerator CallOpenAI(string userInput, Action<int, string, string> onComplete)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // JsonUtility는 중첩 구조 직렬화가 불편하므로 직접 JSON 문자열 구성
        string escapedSystemPrompt = EscapeJson(SYSTEM_PROMPT);
        string escapedUserInput = EscapeJson(userInput);

        string jsonBody =
            "{" +
                "\"model\":\"gpt-4o-mini\"," +
                "\"response_format\":{\"type\":\"json_object\"}," +
                "\"messages\":[" +
                    "{\"role\":\"system\",\"content\":\"" + escapedSystemPrompt + "\"}," +
                    "{\"role\":\"user\",\"content\":\"" + escapedUserInput + "\"}" +
                "]," +
                "\"temperature\":0.7," +
                "\"max_tokens\":150" +
            "}";

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_openAIApiKey}");
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(0, null, $"OpenAI API 오류: {request.error}");
                yield break;
            }

            try
            {
                var chatResponse = JsonUtility.FromJson<OpenAIChatResponse>(request.downloadHandler.text);
                string content = chatResponse.choices[0].message.content;

                var emotionResult = JsonUtility.FromJson<EmotionAnalysisResult>(content);
                int emotion = Mathf.Clamp(emotionResult.emotion, 1, 4);
                onComplete?.Invoke(emotion, emotionResult.response, null);
            }
            catch (Exception e)
            {
                onComplete?.Invoke(0, null, $"감정 분석 응답 파싱 실패: {e.Message}");
            }
        }
    }

    /// <summary>Naver Clova Voice Premium — 텍스트를 감정 반영 MP3 음성으로 변환</summary>
    private IEnumerator CallClovaTTS(string text, int emotionCode, Action<string, string> onComplete)
    {
        string url = "https://naveropenapi.apigw.ntruss.com/tts-premium/v1/tts";

        int clovaEmotion = EMOTION_TO_CLOVA.TryGetValue(emotionCode, out int mapped) ? mapped : 0;

        string formData =
            "speaker=nara" +
            "&text=" + UnityWebRequest.EscapeURL(text) +
            "&volume=0" +
            "&speed=0" +
            "&pitch=0" +
            "&emotion=" + clovaEmotion +
            "&emotion-strength=2" +
            "&format=mp3";

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(formData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("X-NCP-APIGW-API-KEY-ID", _naverClientId);
            request.SetRequestHeader("X-NCP-APIGW-API-KEY", _naverClientSecret);
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(null, $"Clova TTS 오류: {request.error}");
                yield break;
            }

            string audioBase64 = Convert.ToBase64String(request.downloadHandler.data);
            onComplete?.Invoke(audioBase64, null);
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>JSON 문자열 내 특수문자 이스케이프</summary>
    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        var sb = new StringBuilder(str.Length);
        foreach (char c in str)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    #endregion
}
