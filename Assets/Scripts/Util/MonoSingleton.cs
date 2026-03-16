using UnityEngine;

/// <summary>로그 출력 타입</summary>
public enum ELogType
{
    Normal,
    Warning,
    Error
}

/// <summary>
/// MonoBehaviour 기반 제네릭 싱글톤 베이스 클래스
/// - 씬에 인스턴스가 없으면 자동으로 GameObject를 생성하여 인스턴스를 만든다
/// - DontDestroyOnLoad로 씬 전환 시에도 유지된다
/// - 멀티스레드 환경에서 _lock으로 중복 생성을 방지한다
/// - 앱 종료 시 새 인스턴스 생성을 막기 위해 _isApplicationQuitting 플래그를 사용한다
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    // 멀티스레드 환경에서 Instance 프로퍼티 동시 접근 시 중복 생성 방지용 락 객체
    protected static readonly object _lock = new object();

    // 앱 종료 시작 후 Instance 접근을 막기 위한 플래그
    private static bool _isApplicationQuitting = false;

    [Header("Utility")]
    [SerializeField] protected bool _bDebug = true;     // 디버그 로그 출력 여부

    /// <summary>
    /// 싱글톤 인스턴스 접근자
    /// 인스턴스가 없으면 씬에서 찾고, 없으면 자동 생성한다
    /// 앱 종료 중에는 null을 반환한다
    /// </summary>
    public static T Instance
    {
        get
        {
            // 앱 종료 중에는 null 반환 (OnDestroy 이후 접근 방지)
            if (_isApplicationQuitting)
                return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬에서 기존 인스턴스 탐색
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        // 없으면 새 GameObject를 만들어 컴포넌트 추가
                        GameObject singletonObj = new GameObject();
                        _instance = singletonObj.AddComponent<T>();
                        singletonObj.name = $"[Singleton] {typeof(T)}";
                        DontDestroyOnLoad(singletonObj);
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>인스턴스가 이미 존재하는지 확인 (자동 생성 없이)</summary>
    public static bool HasInstance => _instance != null;

    #region Unity 이벤트

    /// <summary>
    /// 중복 인스턴스 방지 처리
    /// 첫 번째 인스턴스만 유지하고 이후 생성된 것은 파괴한다
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }
        else if (this != _instance)
        {
            // 중복 인스턴스 제거
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 싱글톤 초기화 시 호출되는 추상 메서드
    /// 자식 클래스에서 override하여 초기화 로직 구현
    /// </summary>
    protected abstract void OnSingletonAwake();

    /// <summary>앱 종료 시 플래그 설정 및 자식 클래스 종료 처리 호출</summary>
    private void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
        OnSingletonApplicationQuit();
    }

    /// <summary>
    /// 앱 종료 시 호출되는 추상 메서드
    /// 자식 클래스에서 override하여 리소스 해제 등 종료 처리 구현
    /// </summary>
    protected abstract void OnSingletonApplicationQuit();

    /// <summary>컴포넌트 파괴 시 인스턴스 참조를 초기화하고 자식 처리 호출</summary>
    private void OnDestroy()
    {
        if (this == _instance)
        {
            OnSingletonDestroy();
            _instance = null;
        }
    }

    /// <summary>
    /// 싱글톤 파괴 시 호출되는 추상 메서드
    /// 자식 클래스에서 override하여 정리 작업 구현
    /// </summary>
    protected abstract void OnSingletonDestroy();

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 디버그 빌드 및 에디터에서만 로그를 출력한다
    /// _bDebug가 false면 출력하지 않는다
    /// </summary>
    protected void Log(string msg, ELogType type = ELogType.Normal)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_bDebug) return;

        switch (type)
        {
            case ELogType.Normal:   Debug.Log(msg); break;
            case ELogType.Warning:  Debug.LogWarning(msg); break;
            case ELogType.Error:    Debug.LogError(msg); break;
        }
#endif
    }

    #endregion
}
