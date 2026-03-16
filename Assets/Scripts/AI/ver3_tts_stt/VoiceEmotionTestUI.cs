using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 음성 감정 분석 메인 UI 컨트롤러 (v3)
/// - 녹음 버튼 / 중지 버튼으로 음성 입력을 제어한다
/// - 분석 진행 상태에 따라 마이크 애니메이션, 로딩 애니메이션을 전환한다
/// - 결과 수신 시 꿈별이 캐릭터의 감정 애니메이션 트리거를 호출한다
/// - 시리얼 포트 버튼 입력도 동일하게 처리한다 (data[1] == 1)
///
/// 상태 흐름:
///   Ready → (녹음 버튼) → Recording → (중지 버튼) → Analyzing → Complete → Ready
/// </summary>
public class VoiceEmotionTestUI : MonoBehaviour
{
    [Header("=== Component ===")]
    [SerializeField] private VoiceEmotionAnalyzer _analyzer;    // 음성 분석기
    public Animator _aniDreamStar;                              // 꿈별이 캐릭터 애니메이터

    [Header("=== Button ===")]
    [SerializeField] private Button _recordButton;  // 녹음 시작 버튼
    [SerializeField] private Button _stopButton;    // 녹음/재생 중지 버튼

    [Header("=== Text ===")]
    [SerializeField] private TextMeshProUGUI _statusText;       // 현재 처리 상태 표시
    [SerializeField] private TextMeshProUGUI _recognizedText;   // STT 인식 텍스트
    [SerializeField] private TextMeshProUGUI _emotionText;      // 감정 분석 결과
    [SerializeField] private TextMeshProUGUI _responseText;     // AI 공감 응답 메시지
    public TMP_Text _txtMsg;                                    // 사용자 안내 메시지

    [Header("=== String ===")]
    public string _strReady;                                    // 대기 상태 안내 문구
    public List<string> _strRecordList = new List<string>();    // 녹음 중 교대로 표시할 문구 목록
    public string _strAnalyzing;                                // 분석 중 안내 문구

    [Header("=== Image ===")]
    [SerializeField] private Image _recordingIndicator; // 녹음 중 표시 이미지
    public Image _imgMic;                               // 마이크 아이콘 이미지
    public GameObject _objWifiImgContainer;             // 마이크 와이파이 이펙트 컨테이너
    public Image _imgLoading;                           // 분석 중 로딩 이미지

    [Header("=== Sprites ===")]
    public Sprite _sprLoading;                          // 로딩 스프라이트

    /// <summary>마지막으로 수신한 처리 응답 데이터</summary>
    public VoiceProcessResponse _response;

    private bool _bRecording = false;   // 현재 녹음 중 여부 (시리얼 버튼 제어용)
    private bool _bAnalyzing = false;   // 현재 분석 중 여부 (시리얼 버튼 제어용)

    // 녹음 중 안내 문구를 2.5초마다 교대 표시하기 위한 변수
    private float _elapsedTime = 0f;
    private bool _commentFlag = false;

    [Header("=== Animator ===")]
    public Animator _animMic;       // 마이크 와이파이 애니메이터
    public Animator _animLoading;   // 로딩 스피너 애니메이터

    // 애니메이터 파라미터 상수
    private const string STATE_RECORDING = "MicBlink";
    private const string STATE_ANALYZING = "LoadingRotate";
    private const string PARAM_IS_RECORDING = "IsRecording";
    private const string PARAM_IS_ANALYZING = "IsAnalyzing";

    #region Unity 이벤트

    private void Start()
    {
        // 버튼 이벤트 연결
        _recordButton.onClick.AddListener(OnRecordButtonClick);
        _stopButton.onClick.AddListener(OnStopButtonClick);

        // 분석기 이벤트 연결
        _analyzer._OnRecordingStarted += OnRecordingStarted;
        _analyzer._OnRecordingStopped += OnRecordingStopped;
        _analyzer._OnProcessComplete += OnProcessComplete;
        _analyzer._OnAudioPlayComplete += OnAudioPlayComplete;

        // 시리얼 포트 수신 이벤트 연결
        SerialManager.Instance.ReceiveDataHandler += OnClickRecordBtn;

        // UI 초기 상태 설정
        UpdateUI(false);
        _statusText.text = "대기 중 - 녹음 버튼을 누르세요";
        ClearResults();

        _txtMsg.text = _strReady;

        // 마이크 이미지 표시, 로딩 이미지 숨김
        _imgMic.GetComponent<CanvasGroup>().Activate();
        _animMic.SetBool(PARAM_IS_RECORDING, false);
        _imgLoading.GetComponent<CanvasGroup>().DeActivate();
        _animLoading.SetBool(PARAM_IS_ANALYZING, false);

        if (_recordingIndicator != null)
            _recordingIndicator.gameObject.SetActive(false);
    }

    /// <summary>녹음 중 안내 문구를 2.5초마다 교대로 전환</summary>
    private void Update()
    {
        if (!_bRecording) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= 2.5f)
        {
            _txtMsg.text = _strRecordList[_commentFlag ? 0 : 1];
            _commentFlag = !_commentFlag;
            _elapsedTime = 0f;
        }
    }

    private void OnDestroy()
    {
        _analyzer._OnRecordingStarted -= OnRecordingStarted;
        _analyzer._OnRecordingStopped -= OnRecordingStopped;
        _analyzer._OnProcessComplete -= OnProcessComplete;
        _analyzer._OnAudioPlayComplete -= OnAudioPlayComplete;
    }

    #endregion

    #region 버튼 이벤트

    /// <summary>녹음 버튼 클릭 - 이전 결과를 초기화하고 녹음 시작</summary>
    private void OnRecordButtonClick()
    {
        ClearResults();
        _analyzer.StartRecording();
    }

    /// <summary>중지 버튼 클릭 - 녹음 중이면 처리 시작, 재생 중이면 정지</summary>
    private void OnStopButtonClick()
    {
        if (_analyzer.IsRecording)
            _analyzer.StopRecordingAndProcess();
        else if (_analyzer.IsPlaying)
            _analyzer.StopAudio();
    }

    #endregion

    #region 분석기 이벤트 핸들러

    /// <summary>녹음 시작 시 - 마이크 애니메이션 ON, UI 상태 업데이트</summary>
    private void OnRecordingStarted()
    {
        UpdateUI(true);
        _bRecording = true;
        _statusText.text = "녹음 중... 말씀하세요";

        _animMic.SetBool(PARAM_IS_RECORDING, true);
        _animMic.Play(STATE_RECORDING);

        if (_recordingIndicator != null)
            _recordingIndicator.gameObject.SetActive(true);
    }

    /// <summary>녹음 중지 시 - 로딩 애니메이션 ON, 분석 시작 안내</summary>
    private void OnRecordingStopped()
    {
        _bRecording = false;
        _bAnalyzing = true;
        _statusText.text = "처리 중...";
        _txtMsg.text = _strAnalyzing;

        // 마이크 애니메이션 OFF, 로딩 애니메이션 ON
        _animMic.SetBool(PARAM_IS_RECORDING, false);
        _imgMic.GetComponent<CanvasGroup>().DeActivate();
        _imgLoading.GetComponent<CanvasGroup>().Activate();
        _animLoading.SetBool(PARAM_IS_ANALYZING, true);
        _animLoading.Play(STATE_ANALYZING);

        if (_recordingIndicator != null)
            _recordingIndicator.gameObject.SetActive(false);
    }

    /// <summary>
    /// 서버 응답 수신 시 - 결과 텍스트 표시 및 캐릭터 감정 애니메이션 트리거
    /// 감정 코드: 1=기쁨(tHappy), 2=슬픔(tSad), 3=화남(tAngry), 4=놀람(tSurprised)
    /// </summary>
    private void OnProcessComplete(VoiceProcessResponse response)
    {
        UpdateUI(false);
        _bAnalyzing = false;

        // 로딩 애니메이션 OFF
        _imgLoading.GetComponent<CanvasGroup>().DeActivate();
        _animLoading.SetBool(PARAM_IS_ANALYZING, false);
        _txtMsg.text = "";

        if (!string.IsNullOrEmpty(response.error))
        {
            _statusText.text = $"오류: {response.error}";
            AudioPlayFailed();
            return;
        }

        // 인식 텍스트, 감정, AI 응답 표시
        if (_recognizedText != null)
            _recognizedText.text = $"인식: {response.recognized_text}";

        if (_emotionText != null)
        {
            string emotionName = VoiceEmotionAnalyzer.GetEmotionName(response.emotion);
            _emotionText.text = $"감정: {emotionName}";

            // 캐릭터 감정 애니메이션 트리거
            switch (response.emotion)
            {
                case 1: _aniDreamStar.SetTrigger("tHappy");     break;
                case 2: _aniDreamStar.SetTrigger("tSad");       break;
                case 3: _aniDreamStar.SetTrigger("tAngry");     break;
                case 4: _aniDreamStar.SetTrigger("tSurprised"); break;
            }
        }

        if (_responseText != null)
            _responseText.text = $"응답: {response.response}";

        _statusText.text = !string.IsNullOrEmpty(response.audio) ? "음성 재생 중..." : "완료 (음성 없음)";
    }

    /// <summary>TTS 재생 완료 시 - 마이크 이미지 복원, 대기 안내 표시</summary>
    private void OnAudioPlayComplete()
    {
        _statusText.text = "완료 - 다시 녹음하려면 버튼을 누르세요";
        _imgMic.GetComponent<CanvasGroup>().Activate();
        _txtMsg.text = _strReady;
    }

    #endregion

    #region 내부 처리 메서드

    /// <summary>오디오 출력 실패 시 UI를 대기 상태로 복원</summary>
    private void AudioPlayFailed()
    {
        _statusText.text = "실패 - 다시 녹음하려면 버튼을 누르세요";
        _imgMic.GetComponent<CanvasGroup>().Activate();
        _txtMsg.text = _strReady;
    }

    /// <summary>
    /// 버튼 인터랙션 상태 업데이트
    /// isRecording=true : 녹음 중 (녹음 버튼 비활성, 중지 버튼 활성)
    /// isRecording=false: 대기/재생 중 (녹음 버튼 활성, 중지 버튼은 재생 여부에 따라 결정)
    /// </summary>
    private void UpdateUI(bool isRecording)
    {
        _recordButton.interactable = !isRecording;
        _stopButton.interactable = isRecording || _analyzer.IsPlaying;
    }

    /// <summary>결과 표시 텍스트를 모두 초기화한다</summary>
    private void ClearResults()
    {
        if (_recognizedText != null) _recognizedText.text = "";
        if (_emotionText != null) _emotionText.text = "";
        if (_responseText != null) _responseText.text = "";
    }

    /// <summary>
    /// 시리얼 포트 버튼 수신 핸들러
    /// data[1] == 1 일 때만 동작하며, 상태에 따라 녹음 시작 또는 중지를 처리한다
    /// </summary>
    private void OnClickRecordBtn(byte[] data)
    {
        if (data[1] != 1) return;

        if (!_bRecording && !_bAnalyzing)
            OnRecordButtonClick();
        else
            OnStopButtonClick();
    }

    #endregion
}
