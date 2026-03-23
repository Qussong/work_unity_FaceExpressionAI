using UnityEngine;
using PagingTemplate.View;
using PagingTemplate.Model;
using PagingTemplate.Manager;

namespace PagingTemplate.FSM
{

public class BaseState<TState, TView> : IState where TView : BaseView
{
    protected TView _view;
    protected PageData _data;       // 이 State(View)에 대응하는 데이터 묶음

    public BaseState(TView view, PageData data)
    {
        _view = view;
        _data = data;
    }

    public virtual void Init()
    {
        Debug.Log($"[{typeof(TState).Name}] Init");

        // 이벤트 구독 (Init은 최초 1회만 호출됨)
        _view.OnPrevClicked += OnPrevClicked;
        _view.OnHomeClicked += OnHomeClicked;
        _view.OnNextClicked += OnNextClicked;
    }

    public virtual void Enter()
    {
        Debug.Log($"[{typeof(TState).Name}] Enter");
        BindView();                // Presenter가 View에 데이터 세팅
        _view.Show();
    }

    public virtual void Exit()
    {
        Debug.Log($"[{typeof(TState).Name}] Exit");
        _view.Hide();
    }

    /// <summary>
    /// 이벤트 구독 해제 (프로그램 종료 시 호출)
    /// </summary>
    public virtual void Dispose()
    {
        _view.OnPrevClicked -= OnPrevClicked;
        _view.OnHomeClicked -= OnHomeClicked;
        _view.OnNextClicked -= OnNextClicked;
    }

    public virtual void Update()
    {
        //
    }

    /// <summary>
    /// Presenter가 Model 데이터를 View에 세팅 (서브클래스에서 override)
    /// 예: _view.SetTitle(_data.title); _view.SetBackground(_data.sprite);
    /// </summary>
    protected virtual void BindView() { }

    /// <summary>
    /// 상태 전환 편의 메서드
    /// </summary>
    protected void GoTo<T>() where T : IState => NavigationManager.Instance.GoTo<T>();

    /// <summary>
    /// 이전 버튼 클릭 시 호출 (서브클래스에서 override)
    /// </summary>
    protected virtual void OnPrevClicked() { }

    /// <summary>
    /// 홈 버튼 클릭 시 호출 (서브클래스에서 override)
    /// </summary>
    protected virtual void OnHomeClicked() { }

    /// <summary>
    /// 다음 버튼 클릭 시 호출 (서브클래스에서 override)
    /// </summary>
    protected virtual void OnNextClicked() { }
}

} // namespace PagingTemplate.FSM
