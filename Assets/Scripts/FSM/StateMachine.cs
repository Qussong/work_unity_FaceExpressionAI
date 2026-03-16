using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유한 상태 기계(FSM) 핵심 컴포넌트
/// 상태를 Dictionary로 관리하고 전환 시 Exit → Enter 순서를 보장한다
/// NavigationManager가 소유하며, GoTo<T>()를 통해 외부에서 전환을 요청한다
/// </summary>
public class StateMachine : MonoBehaviour
{
    /// <summary>현재 활성화된 상태</summary>
    private IState _currentState;

    /// <summary>타입을 키로 상태 인스턴스를 저장하는 딕셔너리</summary>
    private Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

    /// <summary>상태 전환 시 발생하는 이벤트 (이전 상태, 새 상태)</summary>
    public event Action<IState, IState> OnStateChanged;

    /// <summary>현재 상태 읽기 전용 프로퍼티</summary>
    public IState CurrentState => _currentState;

    #region Unity 이벤트

    private void Update()
    {
        // 현재 상태의 Update를 매 프레임 호출
        _currentState?.Update();
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>
    /// 상태를 딕셔너리에 등록한다
    /// 같은 타입은 중복 등록하지 않는다
    /// </summary>
    public void AddState<T>(T state) where T : IState
    {
        var type = typeof(T);
        if (!_states.ContainsKey(type))
        {
            _states[type] = state;
        }
    }

    /// <summary>
    /// 지정한 타입의 상태로 전환한다
    /// 현재 상태와 같으면 무시, 없는 타입이면 에러 로그 출력
    /// </summary>
    public void ChangeState<T>() where T : IState
    {
        var type = typeof(T);

        if (!_states.TryGetValue(type, out IState newState))
        {
            Debug.LogError($"[StateMachine] 등록되지 않은 상태: {type.Name}");
            return;
        }

        // 이미 같은 상태면 전환하지 않음
        if (_currentState == newState) return;

        var oldState = _currentState;

        // 이전 상태 종료 → 새 상태 진입
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();

        // 전환 이벤트 발행
        OnStateChanged?.Invoke(oldState, _currentState);

        Debug.Log($"[StateMachine] {oldState?.GetType().Name ?? "None"} → {_currentState.GetType().Name}");
    }

    /// <summary>
    /// 등록된 상태 인스턴스를 타입으로 조회한다
    /// </summary>
    public T GetState<T>() where T : IState
    {
        var type = typeof(T);
        if (_states.TryGetValue(type, out IState state))
        {
            return (T)state;
        }
        return default;
    }

    /// <summary>
    /// 현재 상태가 지정한 타입인지 확인한다
    /// </summary>
    public bool IsCurrentState<T>() where T : IState
    {
        return _currentState is T;
    }

    #endregion
}
