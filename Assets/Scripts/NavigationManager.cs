using UnityEngine;

/// <summary>
/// FSM(유한 상태 기계) 총괄 싱글톤 매니저
/// - StateMachine 컴포넌트를 소유하고 상태를 등록한다
/// - GoTo<T>()로 외부에서 상태 전환을 요청한다
/// - 상태 전환 시 IdleManager 타이머를 리셋한다
/// </summary>
public class NavigationManager : MonoSingleton<NavigationManager>
{
    [Header("Views")]
    [SerializeField] private StartView _startView;
    [SerializeField] private ContentView _contentView;
    [SerializeField] private ResultView _resultView;

    /// <summary>FSM 인스턴스 (외부에서 상태 조회 시 사용)</summary>
    public StateMachine StateMachine { get; private set; }

    #region Unity 이벤트

    protected override void OnSingletonAwake()
    {
        // StateMachine을 동일 GameObject에 컴포넌트로 추가
        StateMachine = gameObject.AddComponent<StateMachine>();

        // 사용할 상태 등록
        RegisterStates();

        // 상태 전환 이벤트 구독
        StateMachine.OnStateChanged += HandleStateChanged;
    }

    private void Start()
    {
        // 앱 시작 시 초기 상태 진입
        GoTo<StartState>();
    }

    protected override void OnSingletonApplicationQuit() { }

    protected override void OnSingletonDestroy()
    {
        if (StateMachine != null)
            StateMachine.OnStateChanged -= HandleStateChanged;
    }

    #endregion

    #region 내부 처리 메서드

    /// <summary>모든 상태 인스턴스를 생성하고 StateMachine에 등록한다</summary>
    private void RegisterStates()
    {
        StateMachine.AddState(new StartState(_startView));
        StateMachine.AddState(new ContentState(_contentView));
        StateMachine.AddState(new ResultState(_resultView));
    }

    /// <summary>상태 전환 시 호출 - IdleManager 타이머 리셋</summary>
    private void HandleStateChanged(IState oldState, IState newState)
    {
        IdleManager.Instance?.ResetTimer();
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>
    /// 지정한 타입의 상태로 전환한다
    /// 예: NavigationManager.Instance.GoTo<ContentState>();
    /// </summary>
    public void GoTo<T>() where T : IState
    {
        StateMachine.ChangeState<T>();
    }

    #endregion
}
