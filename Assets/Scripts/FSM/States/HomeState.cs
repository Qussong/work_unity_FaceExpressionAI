using PagingTemplate.View;
using PagingTemplate.Model;

namespace PagingTemplate.FSM.States
{

public class HomeState : BaseState<HomeState, HomeView>
{
    public HomeState(HomeView view, PageData data) : base(view, data)
    {
    }

    public override void Init()
    {
        base.Init();
        // 최초 1회: 리소스 로딩, 이벤트 바인딩 등
    }

    public override void Enter()
    {
        base.Enter();
        // 매번 진입 시: UI 리셋, 값 초기화 등
    }

    public override void Exit()
    {
        base.Exit();
    }

    protected override void OnNextClicked()
    {
        // GoTo<ContentState>();
    }
}

} // namespace PagingTemplate.FSM.States
