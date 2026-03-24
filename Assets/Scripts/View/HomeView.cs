using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PagingTemplate.View
{

    public class HomeView : BaseView
    {
        [Header("=== AI ===")]
        [SerializeField] private VoiceEmotionAnalyzer _voiceEmotionAnalyzer;

        [Header("=== Animation ===")]
        [SerializeField] private Animator _characterAnimator;
        [SerializeField] private Animator _micEffectAnimator;
        [SerializeField] private Animator _loadingAnimator;

        [Header("=== Image ===")]
        [SerializeField] private Image _imgLoading;

        [Header("=== Text ===")]
        [SerializeField] private TMP_Text _txtMsgTop;
        [SerializeField] private TMP_Text _txtMsgBottom;

        [Header("=== CanvasGroup ===")]
        
        [SerializeField] private CanvasGroup _micCanvasGroup;
        [SerializeField] private CanvasGroup _micEffectCanvasGroup;

        protected override void Awake()
        {
            base.Awake();
            // 이미지 초기화

            // 마이크 이펙트 이미지 숨김
            _micEffectCanvasGroup.alpha = 0f;

            // 로딩 이미지 숨김
            _imgLoading.color = new Color(1f, 1f, 1f, 0f);
        }

        public VoiceEmotionAnalyzer VoiceEmotionAnalyzer => _voiceEmotionAnalyzer;
        public Animator CharacterAnimator => _characterAnimator;
        public Animator MicEffectAnimator => _micEffectAnimator;
        public Animator LoadingAnimator => _loadingAnimator;
        public Image ImgLoading => _imgLoading;
        public CanvasGroup MicCanvasGroup => _micCanvasGroup;
        public CanvasGroup MicEffectCanvasGroup => _micEffectCanvasGroup;
        public TMP_Text TxtMsgTop => _txtMsgTop;
        public TMP_Text TxtMsgBottom => _txtMsgBottom;

    }

} // namespace PagingTemplate.View
