using UnityEngine;

namespace PagingTemplate.Util
{

    public enum ELogType
    {
        Normal,
        Warning,
        Error
    }

    /// <summary>
    /// MonoBehaviour 기반 싱글톤 베이스 클래스
    ///
    /// - 스레드 안전한 Instance 접근
    /// - DontDestroyOnLoad 자동 적용
    /// - 앱 종료 시 재생성 방지
    /// - 에디터 Domain Reload 대응
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isApplicationQuitting = false;

        [Header("Utility")]
        [SerializeField] protected bool _bDebug = true;

        /// <summary>
        /// 싱글톤 인스턴스 접근자
        /// 인스턴스가 없으면 씬 검색 → 자동 생성 순으로 획득
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting) return null;

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObj = new GameObject($"[Singleton] {typeof(T)}");
                            _instance = singletonObj.AddComponent<T>();
                            DontDestroyOnLoad(singletonObj);
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// 인스턴스 존재 여부 확인 (인스턴스 생성 없이)
        /// </summary>
        public static bool HasInstance => _instance != null;

        #region 유니티 이벤트 함수

        /// <summary>
        /// 싱글톤 초기화
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
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 앱 종료 시 자동 호출
        /// </summary>
        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
            OnSingletonApplicationQuit();
        }

        /// <summary>
        /// 오브젝트 파괴 시 자동 호출
        /// </summary>
        private void OnDestroy()
        {
            if (this == _instance)
            {
                OnSingletonDestroy();
                _instance = null;
            }
        }

        #endregion

        #region 서브클래스 오버라이드

        /// <summary>
        /// 싱글톤 초기화 시 호출 (Awake 대체)
        /// 서브클래스에서 반드시 구현
        /// </summary>
        protected abstract void OnSingletonAwake();

        /// <summary>
        /// 앱 종료 시 호출 (필요한 서브클래스만 override)
        /// </summary>
        protected virtual void OnSingletonApplicationQuit() { }

        /// <summary>
        /// 싱글톤 파괴 시 호출 (필요한 서브클래스만 override)
        /// </summary>
        protected virtual void OnSingletonDestroy() { }

        #endregion

        #region Utility

        /// <summary>
        /// 조건부 로그 출력 (에디터 및 개발 빌드에서만 동작)
        /// </summary>
        protected void Log(string msg, ELogType type = ELogType.Normal)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_bDebug) return;

            switch (type)
            {
                case ELogType.Normal: Debug.Log(msg); break;
                case ELogType.Warning: Debug.LogWarning(msg); break;
                case ELogType.Error: Debug.LogError(msg); break;
            }
#endif
        }

        #endregion
    }

} // namespace PagingTemplate.Util
