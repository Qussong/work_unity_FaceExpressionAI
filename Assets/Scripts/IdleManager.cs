using System;
using UnityEngine;

/// <summary>
/// 유휴 타임아웃 관리 싱글톤
/// - 마우스/터치/키보드 입력이 없는 시간을 누적하여 타임아웃 시 StartState로 복귀한다
/// - Pause/Resume으로 일시 중지가 가능하다 (팝업 표시 중 등)
/// </summary>
public class IdleManager : MonoSingleton<IdleManager>
{
    [Header("=== Idle Settings ===")]
    [SerializeField] private float _idleTimeout = 60f;  // 유휴 타임아웃 시간 (초)
    [SerializeField] private bool _isEnabled = true;    // 유휴 감지 활성화 여부

    private float _idleTimer;   // 현재 누적 유휴 시간
    private bool _isPaused;     // 일시 중지 여부

    /// <summary>유휴 타임아웃 발생 시 이벤트</summary>
    public event Action OnIdleTimeout;

    /// <summary>타이머가 리셋될 때 이벤트</summary>
    public event Action OnIdleReset;

    /// <summary>현재 누적 유휴 시간</summary>
    public float IdleTime => _idleTimer;

    /// <summary>타임아웃까지 남은 시간</summary>
    public float RemainingTime => Mathf.Max(0, _idleTimeout - _idleTimer);

    /// <summary>타임아웃 상태인지 여부</summary>
    public bool IsIdle => _idleTimer >= _idleTimeout;

    /// <summary>유휴 감지 활성화 여부</summary>
    public bool IsEnabled => _isEnabled;

    #region Unity 이벤트

    protected override void OnSingletonAwake()
    {
        ResetTimer();
    }

    private void Start()
    {
        // 타임아웃 발생 시 StartState로 복귀
        OnIdleTimeout += NavigationManager.Instance.GoTo<StartState>;
    }

    private void Update()
    {
        if (!_isEnabled || _isPaused) return;

        // 입력이 있으면 타이머 리셋
        if (HasAnyInput())
        {
            ResetTimer();
            return;
        }

        // 유휴 시간 누적
        _idleTimer += Time.deltaTime;

        CheckTimeout();
    }

    protected override void OnSingletonApplicationQuit() { }

    protected override void OnSingletonDestroy() { }

    #endregion

    #region 내부 처리 메서드

    /// <summary>마우스/터치/키보드 입력 중 하나라도 있으면 true</summary>
    private bool HasAnyInput()
    {
        // 마우스 이동
        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f ||
            Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f)
            return true;

        // 마우스 클릭
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            return true;

        // 터치 입력
        if (Input.touchCount > 0)
            return true;

        // 키보드 입력
        if (Input.anyKeyDown)
            return true;

        return false;
    }

    /// <summary>타임아웃 조건 충족 시 이벤트를 발행하고 타이머를 리셋한다</summary>
    private void CheckTimeout()
    {
        if (_idleTimer >= _idleTimeout)
        {
            Log("Idle timeout triggered!");
            OnIdleTimeout?.Invoke();

            // 연속 발생 방지를 위해 즉시 리셋
            ResetTimer();
        }
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>타이머를 0으로 리셋 (사용자 액션 발생 시 호출)</summary>
    public void ResetTimer()
    {
        bool wasActive = _idleTimer > 0;
        _idleTimer = 0f;

        if (wasActive)
        {
            OnIdleReset?.Invoke();
            Log("Timer reset");
        }
    }

    /// <summary>타임아웃 시간을 변경한다 (최소 1초)</summary>
    public void SetTimeout(float seconds)
    {
        _idleTimeout = Mathf.Max(1f, seconds);
        Log($"Timeout set to {_idleTimeout}s");
    }

    /// <summary>유휴 감지를 활성화/비활성화한다</summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (enabled)
            ResetTimer();

        Log($"IdleManager {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>유휴 감지를 일시 중지한다 (팝업 표시 등)</summary>
    public void Pause()
    {
        _isPaused = true;
        Log("Paused");
    }

    /// <summary>일시 중지를 해제하고 타이머를 리셋한다</summary>
    public void Resume()
    {
        _isPaused = false;
        ResetTimer();
        Log("Resumed");
    }

    #endregion
}
