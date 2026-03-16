/// <summary>
/// 모든 상태가 구현해야 하는 인터페이스
/// FSM의 각 상태는 Enter / Update / Exit 세 단계로 동작한다
/// </summary>
public interface IState
{
    /// <summary>상태 진입 시 한 번 호출</summary>
    void Enter();

    /// <summary>상태가 활성화된 매 프레임 호출</summary>
    void Update();

    /// <summary>상태 종료 시 한 번 호출</summary>
    void Exit();
}
