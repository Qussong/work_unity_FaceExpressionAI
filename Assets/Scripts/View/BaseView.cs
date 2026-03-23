using System;
using UnityEngine;
using UnityEngine.UI;

namespace PagingTemplate.View
{

public abstract class BaseView : MonoBehaviour
{
    [Header("=== Base View Settings ===")]
    [SerializeField] protected GameObject _rootPanel;       // View의 루트 패널 (활성화/비활성화 대상)
    [SerializeField] protected bool _showOnAwake = false;    // Awake 시 자동 표시 여부

    [Header("=== Navigation Buttons ===")]
    [SerializeField] private Button _btnPrev;               // 이전 버튼 (없으면 null)
    [SerializeField] private Button _btnHome;               // 홈 버튼 (없으면 null)
    [SerializeField] private Button _btnNext;               // 다음 버튼 (없으면 null)

    public event Action OnShow;                             // View가 표시될 때 발생하는 이벤트
    public event Action OnHide;                             // View가 숨겨질 때 발생하는 이벤트

    /// <summary>
    /// 네비게이션 버튼 이벤트 (State에서 구독)
    /// </summary>
    public event Action OnPrevClicked;
    public event Action OnHomeClicked;
    public event Action OnNextClicked;

    public bool IsVisible { get; private set; } = true;     // 현재 View의 표시 상태

    #region 유니티 이벤트 함수

    protected virtual void Awake()
    {
        // rootPanel 자동 할당 (미설정 시)
        if (_rootPanel == null)
        {
            _rootPanel = gameObject;
        }

        // 네비게이션 버튼 바인딩
        _btnPrev?.onClick.AddListener(() => OnPrevClicked?.Invoke());
        _btnHome?.onClick.AddListener(() => OnHomeClicked?.Invoke());
        _btnNext?.onClick.AddListener(() => OnNextClicked?.Invoke());

        // 초기 상태 설정
        if (_showOnAwake)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    #endregion

    #region 외부 호출 함수

    /// <summary>
    /// 화면 표시
    /// </summary>
    public virtual void Show()
    {
        if (_rootPanel != null)
        {
            _rootPanel.SetActive(true);
        }

        IsVisible = true;
        OnShow?.Invoke();
    }

    /// <summary>
    /// 화면 숨김
    /// </summary>
    public virtual void Hide()
    {
        if (_rootPanel != null)
        {
            _rootPanel.SetActive(false);
        }

        IsVisible = false;
        OnHide?.Invoke();
    }

    /// <summary>
    /// 표시 상태 토글 (On/Off)
    /// </summary>
    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }



    #endregion

}

} // namespace PagingTemplate.View