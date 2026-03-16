using UnityEngine.UI;

/// <summary>
/// 콘텐츠 진행 화면 View
/// - 음성 녹음 및 감정 분석이 진행되는 동안 표시되는 화면
/// - 실제 UI 업데이트는 VoiceEmotionTestUI가 직접 처리한다
/// </summary>
public class ContentView : BaseView
{
    public Image _imgBackground;   // 배경 이미지
}
