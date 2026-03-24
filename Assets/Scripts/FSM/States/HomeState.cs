using PagingTemplate.View;
using PagingTemplate.Model;
using UnityEngine;

namespace PagingTemplate.FSM.States
{

    public class HomeState : BaseState<HomeState, HomeView>
    {
        #region public

        public HomeState(HomeView view, PageData data) : base(view, data)
        {
        }

        public override void Init()
        {
            base.Init();
            // 최초 1회: 리소스 로딩, 이벤트 바인딩 등

            SerialManager.Instance.ReceiveDataHandler += OnSerialReceive;

            _view.TxtMsgTop.text = GetRandomQuestion();
            _view.TxtMsgBottom.text = "버튼을 꾹 누른채로 AI 꿈별이에게 말을 걸어봐요!";

            // 녹음 시작
            _view.VoiceEmotionAnalyzer._OnRecordingStarted += () =>
            {
                // 메세지 업데이트
                _view.TxtMsgTop.text = "";
                _view.TxtMsgBottom.text = "AI 꿈별이가 듣고 있어요!\n이야기가 끝났다면 버튼에서 손을 떼어주세요.";
            };

            // 녹음 중지
            _view.VoiceEmotionAnalyzer._OnRecordingStopped += () =>
            {
                _view.TxtMsgBottom.text = "";

                // 마이크 이미지 숨김
                _view.MicCanvasGroup.alpha = 0f;

                // 로딩 이미지 숨김해제
                _view.ImgLoading.color = new Color(1f, 1f, 1f, 1f);
                // 로딩 애니메이션 재생
                _view.LoadingAnimator.SetBool("isAnalyzing", true);
            };

            // 녹음 실패 (빈 데이터 등)
            _view.VoiceEmotionAnalyzer._OnRecordingFailed += (str) =>
            {

            };

            // API 처리 성공 (STT→감정분석→TTS)
            _view.VoiceEmotionAnalyzer._OnProcessComplete += (response) =>
            {
                // 로딩 애니메이션 숨김
                _view.ImgLoading.color = new Color(1f, 1f, 1f, 0f);
                // 로딩 애니메이션 재생종료
                _view.LoadingAnimator.SetBool("isAnalyzing", false);

                string trigger = response.emotion switch
                {
                    1 => "tHappy",
                    2 => "tSad",
                    3 => "tAngry",
                    4 => "tSurprised",
                    _ => null
                };
                if (trigger != null)
                    _view.CharacterAnimator.SetTrigger(trigger);

            };

            // API 처리 실패
            _view.VoiceEmotionAnalyzer._OnProcessFailed += (str) =>
            {
                // 로딩 애니메이션 숨김
                _view.ImgLoading.color = new Color(1f, 1f, 1f, 0f);
                // 로딩 애니메이션 재생종료
                _view.LoadingAnimator.SetBool("isAnalyzing", false);

                _view.CharacterAnimator.SetTrigger("tRefuse");
            };

            // TTS 재생 완료
            _view.VoiceEmotionAnalyzer._OnAudioPlayComplete += () =>
            {

            };

            // 초기 상태로 복귀 (실패 또는 재생 완료 시)
            _view.VoiceEmotionAnalyzer._OnReset += () =>
            {
                // 메세지 박스 초기화
                _view.TxtMsgTop.text = GetRandomQuestion();
                _view.TxtMsgBottom.text = "버튼을 꾹 누른채로 AI 꿈별이에게 말을 걸어봐요!";

                // 마이크 이미지 숨김해제
                _view.MicCanvasGroup.alpha = 1f;

                // 로딩 이미지 위치 초기화
                _view.ImgLoading.transform.rotation = Quaternion.identity;
            };

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

        #endregion

        #region protected

        protected override void OnNextClicked()
        {
            // GoTo<ContentState>();
        }

        #endregion

        #region private

        private static readonly string[] _exampleQuestions = new[]
        {
            "안녕 꿈별아, 오늘 기분 어때?",
            "안녕 꿈별아! 만나서 정말 반가워.",
            "꿈별아, 나 오늘 마음이 조금 속상해.",
            "나 오늘 친구랑 싸워서 너무 화가 나!",
            "꿈별아, 너 정말 신기하다!",
        };

        private string GetRandomQuestion()
        {
            int index = UnityEngine.Random.Range(0, _exampleQuestions.Length);
            return $"이런 질문은 어때요?\n\"{_exampleQuestions[index]}\"";
        }

        private void OnSerialReceive(byte[] data)
        {
            if (1 == data[1])
            {
                // 버튼 누름 (마이크 활성화, 녹음 시작)
                _view.VoiceEmotionAnalyzer.StartRecording();
                _view.MicEffectAnimator.SetBool("isRecording", true);
            }
            else if (0 == data[1])
            {
                // 버튼 땜 (마이크 비활성화, 녹음 종료)
                _view.MicEffectAnimator.SetBool("isRecording", false);
                _view.MicEffectCanvasGroup.alpha = 0f;
                _view.VoiceEmotionAnalyzer.StopRecordingAndProcess();
            }
        }

        #endregion
    }

} // namespace PagingTemplate.FSM.States
