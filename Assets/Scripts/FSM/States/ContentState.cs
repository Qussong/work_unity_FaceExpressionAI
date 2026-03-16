using UnityEngine;

/// <summary>
/// 콘텐츠 진행 상태
/// - 사용자 인터랙션(음성 녹음 → 감정 분석 → TTS 재생)이 이루어지는 메인 상태
/// - 실제 UI/분석 처리는 VoiceEmotionTestUI가 담당하며, 이 상태는 View 표시만 관리한다
/// </summary>
public class ContentState : BaseState<ContentState, ContentView>
{
    public ContentState(ContentView view) : base(view) { }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
    }
}
