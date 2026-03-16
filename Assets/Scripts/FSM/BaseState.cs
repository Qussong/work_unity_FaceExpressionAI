using UnityEngine;

/// <summary>
/// 모든 상태의 제네릭 베이스 클래스
/// TState : 자기 자신 타입 (로그 출력에 사용)
/// TView  : 이 상태에 대응하는 View 타입 (Enter/Exit 시 자동 Show/Hide)
/// </summary>
public class BaseState<TState, TView> : IState where TView : BaseView
{
    /// <summary>이 상태에 연결된 View 참조</summary>
    protected TView _view;

    public BaseState(TView view)
    {
        _view = view;
    }

    /// <summary>상태 진입 시 View를 표시</summary>
    public virtual void Enter()
    {
        Debug.Log($"[{typeof(TState).Name}] Enter");
        _view.Show();
    }

    /// <summary>상태 종료 시 View를 숨김</summary>
    public virtual void Exit()
    {
        Debug.Log($"[{typeof(TState).Name}] Exit");
        _view.Hide();
    }

    /// <summary>매 프레임 호출 - 자식 클래스에서 override하여 로직 추가</summary>
    public virtual void Update() { }
}
