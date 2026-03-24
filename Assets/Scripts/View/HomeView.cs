using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PagingTemplate.View
{

    public class HomeView : BaseView
    {
        [Header("=== AI ===")]
        [SerializeField] private VoiceEmotionAnalyzer _voiceEmotionAnalyzer;

        [Header("=== Text ===")]
        [SerializeField] private TMP_Text _txtMsgTop;
        [SerializeField] private TMP_Text _txtMsgBottom;

        public VoiceEmotionAnalyzer VoiceEmotionAnalyzer => _voiceEmotionAnalyzer;
        public TMP_Text TxtMsgTop => _txtMsgTop;
        public TMP_Text TxtMsgBottom => _txtMsgBottom;

    }

} // namespace PagingTemplate.View
