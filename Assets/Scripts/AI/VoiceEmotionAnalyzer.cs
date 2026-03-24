using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 음성 처리 응답 데이터
/// </summary>
[Serializable]
public class VoiceProcessResponse
{
    public string recognized_text;  // Clova STT 인식 결과
    public int emotion;             // 0=중립, 1=기쁨, 2=슬픔, 3=화남, 4=놀람
    public string response;         // GPT-4o-mini 공감 응답 메시지
    public string audio;            // base64 인코딩된 TTS MP3
    public string error;            // 전체 오류 메시지
    public string tts_error;        // TTS만 실패했을 때의 오류 메시지
}

/// <summary>
/// 마이크 녹음 → STT → 감정 분석 → TTS 전체 파이프라인 클래스
///
/// 처리 흐름:
///   StartRecording() → (사용자 발화) → StopRecordingAndProcess()
///   → WAV 변환 + 자동 게인 보정
///   → AIService로 STT + 감정분석 + TTS 직접 처리
///   → VoiceProcessResponse 수신 → TTS 음성 재생
/// </summary>
public class VoiceEmotionAnalyzer : MonoBehaviour
{
    [Header("AI 서비스")]
    [SerializeField] private AIService _aiService;

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
    public event Action<string> _OnRecordingFailed;                 // 녹음 실패 시 (빈 데이터 등)
    public event Action<VoiceProcessResponse> _OnProcessComplete;   // API 처리 성공 시
    public event Action<string> _OnProcessFailed;                   // API 처리 실패 시 (STT/OpenAI/TTS 오류)
    public event Action _OnAudioPlayComplete;                       // TTS 재생 완료 시
    public event Action _OnReset;                                   // 초기 상태로 복귀 시

    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private bool _isProcessing = false;
    private string _selectedMicrophone;

    #region 프로퍼티

    /// <summary>현재 녹음 중인지 여부</summary>
    public bool IsRecording => _isRecording;

    /// <summary>현재 처리 중인지 여부 (API 호출 진행 중)</summary>
    public bool IsProcessing => _isProcessing;

    /// <summary>현재 TTS 재생 중인지 여부</summary>
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    #endregion

    #region 유니티 이벤트

    private void Awake()
    {
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (_aiService == null)
            _aiService = GetComponent<AIService>();

        InitMicrophone();

        _OnProcessFailed += (error) => Debug.LogWarning($"[VoiceEmotionAnalyzer] API 처리 실패: {error}");
    }

    #endregion

    #region 외부 호출 함수

    /// <summary>마이크 녹음을 시작한다</summary>
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

        // 마이크 준비 완료까지 대기 (최대 1초)
        float waitStart = Time.realtimeSinceStartup;
        while (Microphone.GetPosition(_selectedMicrophone) <= 0)
        {
            if (Time.realtimeSinceStartup - waitStart > 1f)
            {
                Debug.LogError("[VoiceEmotionAnalyzer] 마이크 응답 타임아웃");
                Microphone.End(_selectedMicrophone);
                _OnRecordingFailed?.Invoke("마이크가 응답하지 않습니다");
                _OnReset?.Invoke();
                return;
            }
        }

        _isRecording = true;

        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] 녹음 시작");

        _OnRecordingStarted?.Invoke();
    }

    /// <summary>녹음을 중지하고 AI 파이프라인을 실행한다</summary>
    public void StopRecordingAndProcess()
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[VoiceEmotionAnalyzer] 녹음 중이 아닙니다");
            return;
        }

        int position = Microphone.GetPosition(_selectedMicrophone);
        Microphone.End(_selectedMicrophone);
        _isRecording = false;

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] 녹음 중지 (샘플: {position})");

        _OnRecordingStopped?.Invoke();

        if (position == 0)
        {
            Debug.LogWarning("[VoiceEmotionAnalyzer] 녹음된 데이터가 없습니다");
            _OnRecordingFailed?.Invoke("녹음된 데이터가 없습니다");
            _OnReset?.Invoke();
            return;
        }

        // 녹음된 구간만 추출
        float[] samples = new float[position];
        _recordingClip.GetData(samples, 0); // 오디오 클립 복사

        // 자동 게인 보정
        ApplyAutoGain(samples);

        // WAV 변환
        byte[] wavData = ConvertToWav(samples, _sampleRate);

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] WAV 변환 완료: {wavData.Length} bytes");

        StartCoroutine(ProcessPipeline(wavData));
    }

    /// <summary>녹음을 취소한다 (처리 없이 중지)</summary>
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

    /// <summary>감정 코드를 한국어 문자열로 변환</summary>
    public static string GetEmotionName(int emotionCode)
    {
        return emotionCode switch
        {
            0 => "중립",
            1 => "기쁨",
            2 => "슬픔",
            3 => "화남",
            4 => "놀람",
            _ => "알 수 없음"
        };
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

    #endregion

    #region 내부 처리 함수

    /// <summary>첫 번째 마이크를 기본 선택한다</summary>
    private void InitMicrophone()
    {
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

    /// <summary>자동 게인 보정 (목표 0.35, 최대 150배)</summary>
    private void ApplyAutoGain(float[] samples)
    {
        float maxLevel = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > maxLevel) maxLevel = abs;
        }

        float targetMax = 0.35f;
        float gain = maxLevel > 0 ? Mathf.Clamp(targetMax / maxLevel, 1f, 150f) : 1f;

        if (_showDebugLog)
            Debug.Log($"[VoiceEmotionAnalyzer] 자동 게인 보정: x{gain:F2}");

        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);

        if (_showDebugLog)
        {
            float newMax = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = Mathf.Abs(samples[i]);
                if (abs > newMax) newMax = abs;
            }

            if (newMax < 0.05f)
                Debug.LogWarning("[VoiceEmotionAnalyzer] 보정 후에도 레벨이 낮습니다! 마이크를 확인하세요.");
        }
    }

    /// <summary>AIService를 통해 STT → 감정분석 → TTS 파이프라인 실행</summary>
    private IEnumerator ProcessPipeline(byte[] wavData)
    {
        _isProcessing = true;

        if (_showDebugLog)
            Debug.Log("[VoiceEmotionAnalyzer] AI 파이프라인 시작");

        VoiceProcessResponse response = null;
        yield return StartCoroutine(_aiService.ProcessVoice(wavData, (result) =>
        {
            response = result;
        }));

        _isProcessing = false;

        // 처리 실패 시
        if (response == null || !string.IsNullOrEmpty(response.error))
        {
            string errorMsg = response?.error ?? "알 수 없는 오류";
            _OnProcessFailed?.Invoke(errorMsg);
            _OnReset?.Invoke();
            yield break;
        }

        // TTS 실패 시 전체 실패 처리
        if (!string.IsNullOrEmpty(response.tts_error))
        {
            _OnProcessFailed?.Invoke(response.tts_error);
            _OnReset?.Invoke();
            yield break;
        }

        // 처리 성공
        _OnProcessComplete?.Invoke(response);

        // TTS 오디오 재생
        if (!string.IsNullOrEmpty(response.audio))
            yield return StartCoroutine(PlayAudioFromBase64(response.audio));
        else if (_showDebugLog)
            Debug.LogWarning("[VoiceEmotionAnalyzer] TTS 오디오 없음 — 재생 건너뜀");
    }

    /// <summary>float 샘플 배열을 RIFF WAV 바이트 배열로 변환한다</summary>
    private byte[] ConvertToWav(float[] samples, int sampleRate)
    {
        int channels = 1;
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int subChunk2Size = samples.Length * channels * bitsPerSample / 8;
        int chunkSize = 36 + subChunk2Size;

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            foreach (float sample in samples)
            {
                short pcmSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(pcmSample);
            }

            return memoryStream.ToArray();
        }
    }

    /// <summary>Base64 MP3 → 임시 파일 → AudioSource 재생</summary>
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

                yield return new WaitForSeconds(clip.length);

                _OnAudioPlayComplete?.Invoke();
                _OnReset?.Invoke();
            }
            else
            {
                Debug.LogError($"[VoiceEmotionAnalyzer] 오디오 로드 실패: {www.error}");
                _OnAudioPlayComplete?.Invoke();
                _OnReset?.Invoke();
            }
        }
    }

    #endregion
}
