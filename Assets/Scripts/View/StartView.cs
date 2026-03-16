using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시작 화면 View
/// - 안내 메시지, 마이크 이미지, 디버그 컨테이너를 보유한다
/// - 디버그 컨테이너는 StartState에서 Space 키로 토글된다
/// </summary>
public class StartView : BaseView
{
    [Header("=== Text ===")]
    public TMP_Text txtMsg;             // 화면 안내 메시지

    [Header("=== Image ===")]
    public Image _imgMic;              // 마이크 아이콘 이미지

    [Header("=== Container ===")]
    public GameObject _objDebugContainer;  // 디버그(테스트) UI 컨테이너
}
