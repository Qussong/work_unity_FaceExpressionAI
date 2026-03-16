using System;
using UnityEngine;

/// <summary>
/// 모든 View의 추상 베이스 클래스
/// _rootPanel의 SetActive로 화면 표시/숨김을 처리하며,
/// Show/Hide 이벤트를 외부에 노출한다
/// </summary>
public abstract class BaseView : MonoBehaviour
{
    [Header("=== Base View Settings ===")]
    [SerializeField] protected GameObject _rootPanel;       // Show/Hide 대상 루트 패널
    [SerializeField] protected bool _showOnAwake = false;   // Awake 시 자동 표시 여부

    /// <summary>View가 표시될 때 발생하는 이벤트</summary>
    public event Action OnShow;

    /// <summary>View가 숨겨질 때 발생하는 이벤트</summary>
    public event Action OnHide;

    /// <summary>현재 View 표시 여부</summary>
    public bool IsVisible { get; private set; } = true;

    #region Unity 이벤트

    protected virtual void Awake()
    {
        // _rootPanel 미설정 시 자신의 GameObject를 사용
        if (_rootPanel == null)
        {
            _rootPanel = gameObject;
        }

        // 초기 표시 상태 결정
        if (_showOnAwake)
            Show();
        else
            Hide();
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>화면 표시 (rootPanel 활성화 + OnShow 이벤트)</summary>
    public virtual void Show()
    {
        if (_rootPanel != null)
            _rootPanel.SetActive(true);

        IsVisible = true;
        OnShow?.Invoke();
    }

    /// <summary>화면 숨김 (rootPanel 비활성화 + OnHide 이벤트)</summary>
    public virtual void Hide()
    {
        if (_rootPanel != null)
            _rootPanel.SetActive(false);

        IsVisible = false;
        OnHide?.Invoke();
    }

    /// <summary>현재 표시 상태를 반전 (Show ↔ Hide 토글)</summary>
    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    /// <summary>UI 초기화 - 자식 클래스에서 override하여 사용</summary>
    public virtual void ResetView() { }

    #endregion
}
