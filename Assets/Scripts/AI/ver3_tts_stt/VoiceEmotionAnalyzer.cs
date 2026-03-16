using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 음성 처리 응답 데이터 (Python 서버 /process-voice 응답)
/// </summary>
[Serializable]
public class VoiceProcessResponse
{
    public string recognized_text;  // Clova STT 인식 결과
    public int emotion;             // 1=기쁨, 2=슬픔, 3=화남, 4=놀람
    public string response;         // GPT-4o-mini 공감 응답 메시지
    public string audio;            // base64 인코딩된 TTS MP3
    public string error;            // 전체 오류 메시지
    public string tts_error;        // TTS만 실패했을 때의 오류 메시지
}

/// <summary>
/// 음성 처리 요청 데이터 (Python 서버 /process-voice 요청)
/// </summary>
[Serializable]
public class VoiceProcessRequest
{
    public string audio;  // base64 인코딩된 WAV 오디오
}

/// <summary>
/// 마이크 녹음 → STT → 감정 분석 → TTS 전체 파이프라인 클래스 (v3)
///
/// 처리 흐름:
///   StartRecording() → (사용자 발화) → StopRecordingAndProcess()
///   → WAV 변환 + 자동 게인 보정 → Base64 인코딩
///   → POST /process-voice → STT + 감정분석 + TTS (Python 서버)
///   → VoiceProcessResponse 수신 → TTS 음성 재생
/// </summary>
public class VoiceEmotionAnalyzer : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string _serverUrl = "http://localhost:5000";

    [Header("마이크 설정")]
    [SerializeField] private int _recordingDuration = 10;  // 최대 녹음 시간 (초)
    [SerializeField] private int _sampleRate = 48000;      // 캡처 샘플레이트 (Hz)

    [Header("오디오")]
    [SerializeField] private AudioSource _audioSource;

    [Header("디버그")]
    [SerializeField] private bool _showDebugLog = true;

    // 이벤트
    public event Action _OnRecordingStarted;                        // 녹음 시작 시
    public event Action _OnRecordingStopped;                        // 녹음 중지 시
    public event Action<VoiceProcessResponse> _OnProcessComplete;   // 서버 응답 수신 시
    public event Action _OnAudioPlayComplete;                       // TTS 재생 완료 시

    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private string _selectedMicrophone;

    /// <summary>현재 녹음 중인지 여부</summary>
    public bool IsRecording => _isRecording;

    /// <summary>현재 TTS 재생 중인지 여부</summary>
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    private void Awake()
    {
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // 첫 번째 마이크를 기본 선택
        if (Microphone.devices.Length > 0)
        {
            if (_showDebugLog)
            {
                Debug.Log("[마이크 목록]");
                foreach (var mic in Microphone.devices)
                    Debug.Log(mic);
            }

            _selectedMicrophone = Microphone.devices[0];
            if (_showDebugLog)
                Debug.Log($"[VoiceEmotionAnalyzer] 마이크 선택: {_selectedMicrophone}");
        }
        else
        {
            Debug.LogError("[VoiceEmotionAnalyzer] 마이크를 찾을 수 없습니다!");
        }
    }

    /// <summary>사용 가능한 마이크 목록을 반환한다</summary>
    public string[] GetAvailableMicrophones()
    {
        return Microphone.devices;
    }

    /// <summary>사용할 마이크 디바이스를 변경한다</summary>
    public void SelectMicrophone(string deviceName)
    {
        _selectedMicrophone = deviceName;
        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] 마이크 변경: {deviceName}");
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
    /// 마이크 녹음을 시작한다
    /// 이미 녹음 중이거나 마이크가 선택되지 않았으면 무시한다
    /// </summary>
    public void StartRecording()
    {
        if (_isRecording)
        {
            Debug.LogWarning("[VoiceEmotionAnalyzer] 이미 녹음 중입니다");
            return;
        }

        if (string.IsNullOrEmpty(_selectedMicrophone))
        {
            Debug.LogError("[VoiceEmotionAnalyzer] 마이크가 선택되지 않았습니다");
            return;
        }

        _recordingClip = Microphone.Start(_selectedMicrophone, false, _recordingDuration, _sampleRate);

        // 마이크 준비 완료까지 대기 (첫 샘플이 들어올 때까지)
        while (Microphone.GetPosition(_selectedMicrophone) <= 0) { }

        _isRecording = true;

        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] 녹음 시작");

        _OnRecordingStarted?.Invoke();
    }

    /// <summary>
    /// 녹음을 중지하고 서버로 오디오를 전송한다
    /// WAV 변환, 자동 게인 보정, Base64 인코딩을 수행한 후 POST /process-voice 요청을 보낸다
    /// </summary>
    public void StopRecordingAndProcess()
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[VoiceEmotionAnalyzer] 녹음 중이 아닙니다");
            return;
        }

        // 현재 녹음 위치(샘플 수) 저장 후 마이크 중지
        int position = Microphone.GetPosition(_selectedMicrophone);
        Microphone.End(_selectedMicrophone);
        _isRecording = false;

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] 녹음 중지 (샘플: {position})");

        _OnRecordingStopped?.Invoke();

        if (position == 0)
        {
            Debug.LogWarning("[VoiceEmotionAnalyzer] 녹음된 데이터가 없습니다");
            return;
        }

        // 녹음된 구간만 추출 (position 이후는 무음)
        float[] samples = new float[position];
        _recordingClip.GetData(samples, 0);

        // 최대 샘플 레벨 측정
        float maxLevel = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > maxLevel) maxLevel = abs;
        }

        // 자동 게인 보정: 목표 최대 레벨 0.35까지 증폭 (최대 150배)
        // STT 인식률 향상을 위해 낮은 볼륨의 음성을 자동으로 키운다
        float targetMax = 0.35f;
        float gain = maxLevel > 0 ? Mathf.Clamp(targetMax / maxLevel, 1f, 150f) : 1f;

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] 자동 게인 보정: x{gain:F2}");

        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);

        // 보정 후 레벨 확인 (디버그용)
        if (_showDebugLog)
        {
            float newMax = 0f;
            float newSum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = Mathf.Abs(samples[i]);
                if (abs > newMax) newMax = abs;
                newSum += abs;
            }
            Debug.Log($"[VoiceEmotionAnalyzer] (보정 후) 최대: {newMax:F4}, 평균: {newSum / samples.Length:F6}");

            if (newMax < 0.05f)
                Debug.LogWarning("[VoiceEmotionAnalyzer] 보정 후에도 레벨이 낮습니다! 마이크를 확인하세요.");
        }

        // float 샘플 배열을 16-bit PCM WAV로 변환
        byte[] wavData = ConvertToWav(samples, _sampleRate);

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] WAV 변환 완료: {wavData.Length} bytes");

        StartCoroutine(SendToServer(wavData));
    }

    /// <summary>녹음을 취소한다 (서버 전송 없이 중지)</summary>
    public void CancelRecording()
    {
        if (!_isRecording) return;

        Microphone.End(_selectedMicrophone);
        _isRecording = false;

        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] 녹음 취소");

        _OnRecordingStopped?.Invoke();
    }

    /// <summary>재생 중인 TTS 오디오를 정지한다</summary>
    public void StopAudio()
    {
        if (_audioSource.isPlaying)
            _audioSource.Stop();
    }

    #region 내부 처리 메서드

    /// <summary>
    /// float 샘플 배열을 RIFF WAV 바이트 배열로 변환한다
    /// 포맷: 모노, 16-bit PCM, 지정된 샘플레이트
    /// </summary>
    private byte[] ConvertToWav(float[] samples, int sampleRate)
    {
        int channels = 1;        // 모노
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int subChunk2Size = samples.Length * channels * bitsPerSample / 8;
        int chunkSize = 36 + subChunk2Size;

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            // RIFF 청크
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt 서브청크
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);               // PCM 포맷 크기
            writer.Write((short)1);         // AudioFormat = PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // data 서브청크
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            // float → 16-bit PCM 변환
            foreach (float sample in samples)
            {
                short pcmSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(pcmSample);
            }

            return memoryStream.ToArray();
        }
    }

    /// <summary>
    /// WAV 데이터를 Base64로 인코딩하여 서버에 POST 요청을 보낸다
    /// 응답으로 인식 텍스트, 감정 코드, AI 응답, TTS 오디오를 수신한다
    /// </summary>
    private IEnumerator SendToServer(byte[] wavData)
    {
        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] 서버로 전송 중...");

        string base64Audio = Convert.ToBase64String(wavData);
        var requestData = new VoiceProcessRequest { audio = base64Audio };
        string jsonData = JsonUtility.ToJson(requestData);
        string url = $"{_serverUrl}/process-voice";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            VoiceProcessResponse response;

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (_showDebugLog)
                    Debug.LogWarning($"[VoiceEmotionAnalyzer] 서버 오류: {request.error}");

                response = new VoiceProcessResponse { error = request.error };
                _OnProcessComplete?.Invoke(response);
                yield break;
            }

            if (_showDebugLog)
                Debug.Log("[VoiceEmotionAnalyzer] 응답 수신");

            try
            {
                response = JsonUtility.FromJson<VoiceProcessResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                response = new VoiceProcessResponse { error = $"JSON 파싱 실패: {e.Message}" };
                _OnProcessComplete?.Invoke(response);
                yield break;
            }

            // 처리 완료 이벤트 발행 (UI 업데이트 등)
            _OnProcessComplete?.Invoke(response);

            // TTS 오디오 재생
            if (!string.IsNullOrEmpty(response.audio))
                yield return StartCoroutine(PlayAudioFromBase64(response.audio));
        }
    }

    /// <summary>
    /// Base64 인코딩된 MP3 데이터를 임시 파일로 저장 후 AudioSource로 재생한다
    /// Unity는 MP3를 메모리에서 직접 로드할 수 없으므로 임시 파일 경유가 필요하다
    /// </summary>
    private IEnumerator PlayAudioFromBase64(string base64Audio)
    {
        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] 오디오 재생 시작");

        byte[] audioBytes = Convert.FromBase64String(base64Audio);
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "tts_response.mp3");
        System.IO.File.WriteAllBytes(tempPath, audioBytes);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                _audioSource.clip = clip;
                _audioSource.Play();

                if (_showDebugLog)
                    Debug.Log($"[VoiceEmotionAnalyzer] 재생 중 (길이: {clip.length}초)");

                // 재생 완료까지 대기
                yield return new WaitForSeconds(clip.length);

                _OnAudioPlayComplete?.Invoke();
            }
            else
            {
                Debug.LogError($"[VoiceEmotionAnalyzer] 오디오 로드 실패: {www.error}");
            }
        }
    }

    #endregion
}
