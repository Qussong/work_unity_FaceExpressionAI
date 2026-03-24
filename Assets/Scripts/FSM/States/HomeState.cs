using PagingTemplate.View;
using PagingTemplate.Model;

namespace PagingTemplate.FSM.States
{

    public class HomeState : BaseState<HomeState, HomeView>
    {
        public HomeState(HomeView view, PageData data) : base(view, data)
        {
        }

        public override void Init()
        {
            base.Init();
            // 최초 1회: 리소스 로딩, 이벤트 바인딩 등

            _view.TxtMsgTop.text = "이런 질문은 어떄요?\n\"안녕 꿈별아, 오늘 기분 어때?\"";
            _view.TxtMsgBottom.text = "버튼을 꾹 누른채로 AI 꿈별이에게 말을 걸어봐요!";

            _view.VoiceEmotionAnalyzer._OnRecordingStarted += () =>
            {
                _view.TxtMsgTop.text = "";
                _view.TxtMsgBottom.text = "AI 꿈별이가 듣고 있어요!\n이야기가 끝났다면 버튼에서 손을 떼어주세요.";
            };  // 녹음 시작
            _view.VoiceEmotionAnalyzer._OnRecordingStopped += () =>
            {
                _view.TxtMsgBottom.text = "";
            };  // 녹음 중지
            _view.VoiceEmotionAnalyzer._OnRecordingFailed += (str) =>
            {

            };  // 녹음 실패 (빈 데이터 등)
            _view.VoiceEmotionAnalyzer._OnProcessComplete += (response) =>
            {

            };  // API 처리 성공 (STT→감정분석→TTS)
            _view.VoiceEmotionAnalyzer._OnProcessFailed += (str) =>
            {

            };  // API 처리 실패
            _view.VoiceEmotionAnalyzer._OnAudioPlayComplete += () =>
            {
                
            };  // TTS 재생 완료
            _view.VoiceEmotionAnalyzer._OnReset += () =>
            {
                _view.TxtMsgTop.text = "이런 질문은 어떄요?\n\"안녕 꿈별아, 오늘 기분 어때?\"";
                _view.TxtMsgBottom.text = "버튼을 꾹 누른채로 AI 꿈별이에게 말을 걸어봐요!";
            };  // 초기 상태로 복귀 (실패 또는 재생 완료 시)
            
        }

        public override void Enter()
        {
            base.Enter();
            // 매번 진입 시: UI 리셋, 값 초기화 등
        }

        public override void Exit()
        {
            base.Exit();
        }

        protected override void OnNextClicked()
        {
            // GoTo<ContentState>();
        }
    }

} // namespace PagingTemplate.FSM.States
