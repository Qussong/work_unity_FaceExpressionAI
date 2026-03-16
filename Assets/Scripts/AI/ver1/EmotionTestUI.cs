using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 감정 분석 v1 테스트용 UI 컨트롤러
/// - 텍스트를 직접 입력하여 감정 분석 결과를 확인하는 디버그/테스트 화면
/// </summary>
public class EmotionTestUI : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private EmotionAnalyzer analyzer;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button analyzeButton;
    [SerializeField] private TextMeshProUGUI resultText;

    private void Start()
    {
        analyzeButton.onClick.AddListener(OnAnalyzeButtonClick);
        analyzer.OnAnalysisComplete += OnAnalysisComplete;
    }

    private void OnDestroy()
    {
        analyzer.OnAnalysisComplete -= OnAnalysisComplete;
    }

    /// <summary>분석 버튼 클릭 - 입력 텍스트로 감정 분석 요청</summary>
    private void OnAnalyzeButtonClick()
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text))
        {
            resultText.text = "텍스트를 입력해주세요.";
            return;
        }

        resultText.text = "분석 중...";
        analyzeButton.interactable = false;

        analyzer.Analyze(text);
    }

    /// <summary>분석 완료 시 결과를 화면에 표시</summary>
    private void OnAnalysisComplete(EmotionResponse response)
    {
        analyzeButton.interactable = true;

        if (!string.IsNullOrEmpty(response.error))
        {
            resultText.text = $"오류: {response.error}";
            return;
        }

        string emotionName = EmotionAnalyzer.GetEmotionName(response.emotion);
        resultText.text = $"감정: [{response.emotion}] {emotionName}\n응답: {response.response}";
    }
}
