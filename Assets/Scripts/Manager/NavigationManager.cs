using UnityEngine;
using PagingTemplate.Util;
using PagingTemplate.FSM;
using PagingTemplate.FSM.States;
using PagingTemplate.Model;
using PagingTemplate.View;

namespace PagingTemplate.Manager
{

public class NavigationManager : MonoSingleton<NavigationManager>
{
    /// <summary>
    /// 필드
    /// </summary>
    [Header("Views")]
    [SerializeField] private HomeView _startView;
    // [SerializeField] private ContentView _contentView;
    // [SerializeField] private ResultView _resultView;

    /// <summary>
    /// 프로퍼티
    /// </summary>
    public StateMachine StateMachine { get; private set; }

    #region 유니티 이벤트 함수

    protected override void OnSingletonAwake()
    {
        // 비활성 상태의 View를 강제 활성화 (Awake 호출 보장, _showOnAwake=false이므로 자동 Hide됨)
        ActivateAllViews();

        // StateMachine 컴포넌트 추가
        StateMachine = gameObject.AddComponent<StateMachine>();

        // 상태 등록
        RegisterState();

        // 상태 변경 이벤트 구독
        StateMachine.OnStateChanged += HandleStateChanged;
    }

    private void Start()
    {
        // 초기 상태
        GoTo<HomeState>();

        // 무입력 타임아웃 시 홈으로 복귀
        IdleManager.Instance.OnIdleTimeout += HandleIdleTimeout;
    }

    protected override void OnSingletonDestroy()
    {
        if (StateMachine != null)
        {
            StateMachine.OnStateChanged -= HandleStateChanged;
        }

        var idleManager = IdleManager.Instance;
        if (idleManager != null)
        {
            idleManager.OnIdleTimeout -= HandleIdleTimeout;
        }
    }

    #endregion

    #region 내부 호출 함수

    /// <summary>
    /// 비활성 상태의 View GameObject를 강제 활성화하여 Awake() 호출을 보장
    /// </summary>
    private void ActivateAllViews()
    {
        _startView.gameObject.SetActive(true);
        // _contentView.gameObject.SetActive(true);
        // _resultView.gameObject.SetActive(true);
    }

    private void RegisterState()
    {
        var repo = new DataRepository();

        StateMachine.AddState(new HomeState(_startView, repo.GetData<HomeView>()));
        // StateMachine.AddState(new ContentState(_contentView, repo.GetData<ContentView>()));
        // StateMachine.AddState(new ResultState(_resultView, repo.GetData<ResultView>()));
    }

    private void HandleStateChanged(IState oldState, IState newState)
    {
        // IdleManager 타이머 리셋
        IdleManager.Instance?.ResetTimer();

        // 상태 변경 시 추가 처리

    }

    /// <summary>
    /// 무입력 타임아웃 발생 시 홈(StartState)으로 복귀
    /// </summary>
    private void HandleIdleTimeout()
    {
        GoTo<HomeState>();
    }

    #endregion

    #region 외부 호출 함수

    public void GoTo<T>() where T : IState
    {
        StateMachine.ChangeState<T>();
    }

    #endregion

}

} // namespace PagingTemplate.Manager
