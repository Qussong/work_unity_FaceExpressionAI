using UnityEngine;

/// <summary>
/// 시작 화면 상태
/// - 앱 최초 진입 또는 유휴 타임아웃 후 복귀 시 활성화
/// - Space 키로 디버그 컨테이너(테스트 UI)를 토글할 수 있다
/// </summary>
public class StartState : BaseState<StartState, StartView>
{
    /// <summary>디버그 컨테이너 표시 여부</summary>
    private bool _bDebug = false;

    public StartState(StartView view) : base(view) { }

    /// <summary>진입 시 디버그 컨테이너를 숨기고 플래그를 초기화</summary>
    public override void Enter()
    {
        base.Enter();

        _view._objDebugContainer.GetComponent<CanvasGroup>().DeActivate();
        _bDebug = false;
    }

    public override void Exit()
    {
        base.Exit();
    }

    /// <summary>Space 키 입력으로 디버그 컨테이너 ON/OFF 토글</summary>
    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _bDebug = !_bDebug;

            if (_bDebug)
                _view._objDebugContainer.GetComponent<CanvasGroup>().Activate();
            else
                _view._objDebugContainer.GetComponent<CanvasGroup>().DeActivate();
        }
    }
}
