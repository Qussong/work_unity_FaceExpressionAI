using UnityEngine;

/// <summary>
/// 결과 표시 상태
/// - 감정 분석 완료 후 결과 화면을 보여주는 상태
/// - 유휴 타임아웃 발생 시 IdleManager에 의해 StartState로 복귀한다
/// </summary>
public class ResultState : BaseState<ResultState, ResultView>
{
    public ResultState(ResultView view) : base(view) { }

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
