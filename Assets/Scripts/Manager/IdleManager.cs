using System;
using UnityEngine;
using PagingTemplate.Util;

namespace PagingTemplate.Manager
{

public class IdleManager : MonoSingleton<IdleManager>
{
    /// <summary>
    /// 필드
    /// </summary>
    [Header("=== Idle Settings ===")]
    [SerializeField] private float _idleTimeout = 60f;
    [SerializeField] private bool _isEnabled = true;

    private float _idleTimer;
    private bool _isPaused;
    private bool _isIdle;
    public event Action OnIdleTimeout;          // 타임아웃 발생
    public event Action OnIdleReset;            // 타이머 리셋됨

    /// <summary>
    /// 프로퍼티
    /// </summary>
    public float IdleTime => _idleTimer;
    public float RemainingTime => Mathf.Max(0, _idleTimeout - _idleTimer);
    public bool IsIdle => _isIdle;
    public bool IsEnabled => _isEnabled;

    #region 유니티 이벤트 함수 + 추상 함수 구현

    protected override void OnSingletonAwake()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (!_isEnabled || _isPaused) return;

        // 입력 감지
        if (HasAnyInput())
        {
            ResetTimer();
            return;
        }

        // 타이머 증가
        _idleTimer += Time.deltaTime;

        // 타임아웃 체크
        CheckTimeout();
    }

    #endregion

    #region 내부 호출 함수

    /// <summary>
    /// 모든 종류의 입력 감지
    /// </summary>
    private bool HasAnyInput()
    {
        // 마우스 이동
        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f ||
            Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f)
        {
            return true;
        }

        // 마우스 클릭
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            return true;
        }

        // 터치 입력
        if (Input.touchCount > 0)
        {
            return true;
        }

        // 키보드 입력
        if (Input.anyKeyDown)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 타임아웃 체크
    /// </summary>
    private void CheckTimeout()
    {
        if (_idleTimer >= _idleTimeout)
        {
            _isIdle = true;
            Log("Idle timeout triggered!");
            OnIdleTimeout?.Invoke();

            // 타임아웃 후 자동 리셋 (연속 발생 방지)
            ResetTimer();
        }
    }

    #endregion

    #region 외부 호출 함수

    /// <summary>
    /// 타이머 리셋 (사용자 액션 시 호출)
    /// </summary>
    public void ResetTimer()
    {
        bool wasActive = _idleTimer > 0;

        _idleTimer = 0f;
        _isIdle = false;

        if (wasActive)
        {
            OnIdleReset?.Invoke();
            Log("Timer reset");
        }
    }

    /// <summary>
    /// 타임아웃 시간 설정
    /// </summary>
    public void SetTimeout(float seconds)
    {
        _idleTimeout = Mathf.Max(1f, seconds);
        Log($"Timeout set to {_idleTimeout}s");
    }

    /// <summary>
    /// 활성화/비활성화
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;

        if (enabled)
        {
            ResetTimer();
        }

        Log($"IdleManager {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 일시정지 (팝업 등에서 사용)
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
        Log("Paused");
    }

    /// <summary>
    /// 재개
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        ResetTimer(); // 재개 시 리셋
        Log("Resumed");
    }

    #endregion

}

} // namespace PagingTemplate.Manager
