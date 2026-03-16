using UnityEngine.UI;

/// <summary>
/// 결과 표시 화면 View
/// - 감정 분석 완료 후 결과(인식 텍스트, 감정, AI 응답)를 보여주는 화면
/// - 유휴 타임아웃 시 StartView로 전환된다
/// </summary>
public class ResultView : BaseView
{
    public Image _imgBackground;   // 배경 이미지
}
