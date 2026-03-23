namespace PagingTemplate.FSM
{

public interface IState
{
    void Init();    // 최초 1회 초기화 (콘텐츠 시작 시)
    void Enter();   // 상태 진입 시 매번 호출 (반복 플레이 시 리셋 등)
    void Update();
    void Exit();
    void Dispose(); // 이벤트 해제 (프로그램 종료 시)
}

} // namespace PagingTemplate.FSM
