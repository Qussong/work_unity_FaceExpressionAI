using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 감정 분석 + TTS v2 테스트용 UI 컨트롤러
/// - 텍스트를 직접 입력하여 감정 분석 결과와 TTS 음성을 확인하는 디버그/테스트 화면
/// </summary>
public class EmotionTTSTestUI : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private EmotionAnalyzerTTS analyzer;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button analyzeButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        analyzeButton.onClick.AddListener(OnAnalyzeButtonClick);
        stopButton.onClick.AddListener(OnStopButtonClick);

        analyzer.OnAnalysisComplete += OnAnalysisComplete;
        analyzer.OnAudioPlayComplete += OnAudioPlayComplete;

        statusText.text = "대기 중";
    }

    private void OnDestroy()
    {
        analyzer.OnAnalysisComplete -= OnAnalysisComplete;
        analyzer.OnAudioPlayComplete -= OnAudioPlayComplete;
    }

    /// <summary>분석 버튼 클릭 - 입력 텍스트로 감정 분석 + TTS 재생 요청</summary>
    private void OnAnalyzeButtonClick()
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text))
        {
            resultText.text = "텍스트를 입력해주세요.";
            return;
        }

        statusText.text = "분석 중...";
        resultText.text = "";
        analyzeButton.interactable = false;

        analyzer.AnalyzeAndSpeak(text);
    }

    /// <summary>정지 버튼 클릭 - 재생 중인 오디오 정지</summary>
    private void OnStopButtonClick()
    {
        analyzer.StopAudio();
        statusText.text = "정지됨";
    }

    /// <summary>분석 완료 시 결과 표시 (TTS 재생 전에 호출됨)</summary>
    private void OnAnalysisComplete(EmotionTTSResponse response)
    {
        analyzeButton.interactable = true;

        if (!string.IsNullOrEmpty(response.error))
        {
            resultText.text = $"오류: {response.error}";
            statusText.text = "오류 발생";
            return;
        }

        string emotionName = EmotionAnalyzerTTS.GetEmotionName(response.emotion);
        resultText.text = $"감정: {emotionName}\n응답: {response.response}";
        statusText.text = !string.IsNullOrEmpty(response.audio) ? "음성 재생 중..." : "분석 완료 (음성 없음)";
    }

    /// <summary>TTS 오디오 재생 완료 시 상태 텍스트 업데이트</summary>
    private void OnAudioPlayComplete()
    {
        statusText.text = "완료";
    }
}
