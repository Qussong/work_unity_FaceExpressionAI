# 🎭 감정 AI 대화 키오스크 — 기술 명세서

> 인천 학생과학관 감정 인식 AI 대화 키오스크 프로젝트의 기술 설계 문서

| 항목 | 내용 |
|------|------|
| 작성일 | 2026-03-25 |
| 버전 | 3.0 |
| 프로젝트 | work_unity_FaceExpressionAI |

---

## 📚 목차

- [1. 프로젝트 개요](#-1-프로젝트-개요)
- [2. 기술 스택](#-2-기술-스택)
- [3. 시스템 아키텍처](#-3-시스템-아키텍처)
- [4. 핵심 설계 패턴](#-4-핵심-설계-패턴)
- [5. 모듈별 클래스 명세](#-5-모듈별-클래스-명세)
- [6. 데이터 흐름](#-6-데이터-흐름)
- [7. 화면/기능 전환 흐름](#-7-화면기능-전환-흐름)
- [8. 라이프사이클](#-8-라이프사이클)
- [9. 확장 가이드](#-9-확장-가이드)
- [10. 설계 결정 및 트레이드오프](#-10-설계-결정-및-트레이드오프)

---

## 📌 1. 프로젝트 개요

### 🎯 배경 및 목적

인천 학생과학관에 설치되는 감정 인식 AI 대화 키오스크. 체험자가 물리 버튼을 누른 채 음성으로 말하면, AI가 감정을 분석하고 캐릭터 애니메이션 + 공감 음성으로 응답하는 체험형 프로그램이다.

### 🧩 핵심 기능

| 기능 | 설명 |
|------|------|
| 시리얼 버튼 입력 | 물리 버튼 press/release로 녹음 시작/종료 제어 |
| 음성 녹음 | 48kHz 모노, 자동 게인 보정 (목표 0.35, 최대 150배) |
| STT | Naver Clova Speech로 한국어 음성→텍스트 변환 |
| 감정 분석 | OpenAI GPT-4o-mini로 5종 감정 분류 + 공감 응답 생성 |
| TTS | Naver Clova Voice Premium으로 감정별 음성 합성 |
| 캐릭터 반응 | 감정별 애니메이션 트리거 (성공 5종 + 실패 1종) |
| 유휴 감지 | 60초 무입력 시 자동 홈 복귀 |

### 🖥 타겟 환경

| 항목 | 내용 |
|------|------|
| 플랫폼 | Windows PC (키오스크) |
| 입력 | 시리얼 통신 물리 버튼 (9600bps, 0xFA 헤더) + Space바 에뮬레이션 |
| 출력 | 캐릭터 애니메이션 + TTS 음성 + UI 텍스트 |
| 데이터 저장 | StreamingAssets (DataConfig.json + CSV) |

---

## 🧰 2. 기술 스택

| 분류 | 항목 |
|------|------|
| 엔진 | Unity |
| 언어 | C# |
| UI | Unity UI (Canvas, Image, TMP_Text, CanvasGroup) |
| 애니메이션 | Unity Animator (트리거/Bool 파라미터) |
| 이벤트 시스템 | C# `event Action` 기반 |
| 외부 API | Naver Clova STT, Naver Clova Voice Premium, OpenAI GPT-4o-mini |
| HTTP | UnityWebRequest |
| 시리얼 통신 | System.IO.Ports.SerialPort |
| 데이터 포맷 | JSON (API 통신, DataConfig), CSV (View 데이터) |
| 오디오 | 48kHz 모노 PCM → RIFF WAV, base64 MP3 디코딩 |

---

## 🏗 3. 시스템 아키텍처

### 🧱 레이어 구조

```
┌─────────────────────────────────────────────────┐
│                   Hardware                       │
│              SerialManager (버튼)                 │
└──────────────────────┬──────────────────────────┘
                       │ ReceiveDataHandler
┌──────────────────────▼──────────────────────────┐
│                 FSM Layer                        │
│     StateMachine ← NavigationManager (싱글톤)     │
│         │                                        │
│     HomeState (Presenter)                        │
│         │  이벤트 구독 + UI/애니메이션 제어            │
└────┬────┴───────────────────────────────────┬───┘
     │                                        │
┌────▼────────────┐                 ┌─────────▼───┐
│   View Layer    │                 │  AI Layer    │
│   HomeView      │                 │  VoiceEmotion│
│   (UI 필드 노출)  │◄── 이벤트 ───── │  Analyzer    │
│   BaseView      │                 │  AIService   │
└─────────────────┘                 └──────┬──────┘
                                           │
                                    ┌──────▼──────┐
                                    │ External API │
                                    │ Clova STT    │
                                    │ OpenAI GPT   │
                                    │ Clova TTS    │
                                    └─────────────┘
┌─────────────────────────────────────────────────┐
│                 Support Layer                    │
│  Model: DataRepository, PageData, CSVParser      │
│  Manager: IdleManager (유휴 감지)                  │
│  Util: MonoSingleton<T>                          │
└─────────────────────────────────────────────────┘
```

### 📁 디렉토리 구조

```
Assets/Scripts/
├── AI/
│   ├── AIService.cs                 # 외부 API 직접 호출
│   └── VoiceEmotionAnalyzer.cs     # 녹음 + 파이프라인 오케스트레이션
├── FSM/
│   ├── IState.cs                   # 상태 인터페이스
│   ├── BaseState.cs               # 제네릭 Presenter 베이스
│   ├── StateMachine.cs            # 상태 등록/전환 엔진
│   └── States/
│       └── HomeState.cs           # 홈 화면 Presenter
├── View/
│   ├── BaseView.cs                # View 추상 베이스
│   └── HomeView.cs               # 홈 화면 View
├── Model/
│   ├── PageData.cs                # Key-Value 데이터 컨테이너
│   └── DataRepository.cs         # JSON+CSV → PageData 변환
├── Manager/
│   ├── NavigationManager.cs       # FSM 총괄 싱글톤
│   └── IdleManager.cs            # 유휴 타임아웃 감지
└── Util/
    ├── MonoSingleton.cs           # 제네릭 싱글톤 베이스
    ├── SerialManager.cs           # 시리얼 포트 통신
    └── CSVParser.cs              # CSV 파서
```

### 🗂 네임스페이스 체계

```
PagingTemplate
├── FSM
│   ├── IState
│   ├── BaseState<TState, TView>
│   ├── StateMachine
│   └── States
│       └── HomeState
├── View
│   ├── BaseView
│   └── HomeView
├── Model
│   ├── PageData
│   └── DataRepository
├── Manager
│   ├── NavigationManager
│   └── IdleManager
└── Util
    ├── MonoSingleton<T>
    └── CSVParser

(글로벌 네임스페이스)
├── AIService
├── VoiceEmotionAnalyzer
├── VoiceProcessResponse
└── SerialManager
```

---

## 🧩 4. 핵심 설계 패턴

### FSM (Finite State Machine)

상태 기반으로 화면 전환과 로직을 관리한다.

```csharp
// StateMachine — 상태 등록 및 전환
public void ChangeState<T>() where T : IState
{
    var newState = _states[typeof(T)];
    _currentState?.Exit();

    if (!_initializedStates.Contains(typeof(T)))
    {
        newState.Init();
        _initializedStates.Add(typeof(T));
    }

    newState.Enter();
    OnStateChanged?.Invoke(_currentState, newState);
    _currentState = newState;
}
```

| 역할 | 클래스 | 책임 |
|------|--------|------|
| 인터페이스 | `IState` | Init/Enter/Update/Exit/Dispose 정의 |
| 상태 엔진 | `StateMachine` | 상태 등록, 전환, 라이프사이클 관리 |
| 구체 상태 | `HomeState` | 홈 화면 로직 (이벤트 바인딩, UI 제어) |

### MVP (Model-View-Presenter)

BaseState가 Presenter 역할을 하며, View와 Model을 중개한다.

```csharp
// BaseState — Presenter로서 View와 Model 연결
public class BaseState<TState, TView> : IState where TView : BaseView
{
    protected TView _view;    // View 참조
    protected PageData _data; // Model 참조

    public virtual void Enter()
    {
        BindView();   // Model → View 바인딩
        _view.Show();
    }
}
```

| 역할 | 클래스 | 책임 |
|------|--------|------|
| Model | `PageData` | CSV 기반 Key-Value 데이터 |
| View | `HomeView` | UI 필드 노출 (SerializeField + getter) |
| Presenter | `HomeState` | 이벤트 구독, UI 업데이트, 애니메이션 트리거 |

### Singleton

전역 관리자를 MonoSingleton으로 구현한다.

```csharp
// 스레드 안전한 싱글톤 접근
public static T Instance
{
    get
    {
        if (_isApplicationQuitting) return null;
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    var obj = new GameObject($"[Singleton] {typeof(T)}");
                    _instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
}
```

| 역할 | 클래스 | 책임 |
|------|--------|------|
| 베이스 | `MonoSingleton<T>` | 스레드 안전, DontDestroyOnLoad, 자동 생성 |
| FSM 관리 | `NavigationManager` | StateMachine 소유, 상태 등록/전환 |
| 유휴 감지 | `IdleManager` | 무입력 타이머, 타임아웃 이벤트 |
| 시리얼 통신 | `SerialManager` | 포트 연결, 데이터 송수신 |

### Observer (이벤트 기반)

`event Action`으로 컴포넌트 간 느슨한 결합을 유지한다.

```csharp
// VoiceEmotionAnalyzer — 7개 이벤트로 상태 전달
public event Action _OnRecordingStarted;
public event Action _OnRecordingStopped;
public event Action<string> _OnRecordingFailed;
public event Action<VoiceProcessResponse> _OnProcessComplete;
public event Action<string> _OnProcessFailed;
public event Action _OnAudioPlayComplete;
public event Action _OnReset;

// HomeState — 이벤트 구독
_view.VoiceEmotionAnalyzer._OnProcessComplete += (response) =>
{
    _view.CharacterAnimator.SetTrigger(trigger);
};
```

---

## 🧱 5. 모듈별 클래스 명세

### AIService

외부 API(Clova STT, OpenAI, Clova TTS)를 직접 호출하는 서비스 클래스.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_openAIApiKey` | string | OpenAI API 키 |
| `_naverClientId` | string | Naver API 클라이언트 ID |
| `_naverClientSecret` | string | Naver API 시크릿 |
| `_timeoutSeconds` | int | API 타임아웃 (기본 30초) |
| `EMOTION_TO_CLOVA` | Dictionary<int,int> | 감정코드 → Clova TTS 감정 매핑 |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `ProcessVoice(byte[], Action<VoiceProcessResponse>)` | IEnumerator | 전체 파이프라인: STT → 감정분석 → TTS |
| `CallClovaSTT(byte[], Action<string,string>)` | IEnumerator | Clova STT API 호출 |
| `CallOpenAI(string, Action<int,string,string>)` | IEnumerator | GPT-4o-mini 감정 분석 |
| `CallClovaTTS(string, int, Action<string,string>)` | IEnumerator | Clova TTS 음성 합성 |

감정코드 → Clova TTS 매핑:

| 감정코드 | 감정 | Clova TTS |
|---------|------|-----------|
| 0 | 중립 | 0 (중립) |
| 1 | 기쁨 | 2 (기쁨) |
| 2 | 슬픔 | 1 (슬픔) |
| 3 | 화남 | 3 (분노) |
| 4 | 놀람 | 2 (기쁨) |

---

### VoiceEmotionAnalyzer

마이크 녹음, 오디오 처리, AI 파이프라인 오케스트레이션 클래스.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_aiService` | AIService | API 호출 서비스 |
| `_recordingDuration` | int | 최대 녹음 시간 (10초) |
| `_sampleRate` | int | 샘플레이트 (48000Hz) |
| `_audioSource` | AudioSource | TTS 재생용 |

| 이벤트 | 타입 | 발생 시점 |
|--------|------|----------|
| `_OnRecordingStarted` | Action | 녹음 시작 |
| `_OnRecordingStopped` | Action | 녹음 중지 |
| `_OnRecordingFailed` | Action\<string\> | 녹음 실패 (빈 데이터, 마이크 타임아웃) |
| `_OnProcessComplete` | Action\<VoiceProcessResponse\> | API 처리 성공 |
| `_OnProcessFailed` | Action\<string\> | API/TTS 처리 실패 |
| `_OnAudioPlayComplete` | Action | TTS 재생 완료 |
| `_OnReset` | Action | 초기 상태 복귀 (실패/재생완료 후) |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `StartRecording()` | void | 마이크 녹음 시작 |
| `StopRecordingAndProcess()` | void | 녹음 중지 → AI 파이프라인 실행 |
| `CancelRecording()` | void | 녹음 취소 (처리 없이) |
| `StopAudio()` | void | TTS 재생 정지 |
| `GetEmotionName(int)` | string | 감정코드 → 한국어 이름 |

---

### VoiceProcessResponse

AI 파이프라인 응답 데이터 클래스.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `recognized_text` | string | STT 인식 결과 텍스트 |
| `emotion` | int | 감정 코드 (0~4) |
| `response` | string | GPT 공감 응답 메시지 |
| `audio` | string | base64 인코딩 TTS MP3 |
| `error` | string | 전체 파이프라인 오류 |
| `tts_error` | string | TTS만 실패했을 때 오류 |

---

### IState

FSM 상태 인터페이스.

| 메서드 | 설명 |
|--------|------|
| `Init()` | 최초 1회 초기화 (이벤트 바인딩 등) |
| `Enter()` | 상태 진입 시마다 호출 (UI 리셋 등) |
| `Update()` | 매 프레임 호출 |
| `Exit()` | 상태 이탈 시 호출 |
| `Dispose()` | 앱 종료 시 이벤트 해제 |

---

### BaseState\<TState, TView\>

제네릭 Presenter 베이스. `TView`는 `BaseView` 제약.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_view` | TView | 연결된 View (protected) |
| `_data` | PageData | 연결된 데이터 모델 (protected) |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `Init()` | void | View 버튼 이벤트 구독 |
| `Enter()` | void | BindView() → Show() |
| `Exit()` | void | View 숨김 |
| `BindView()` | void | Model → View 바인딩 (오버라이드용) |
| `GoTo<T>()` | void | 상태 전환 단축 메서드 |

---

### StateMachine

상태 등록/전환 엔진.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_states` | Dictionary\<Type, IState\> | 등록된 상태 맵 |
| `_initializedStates` | HashSet\<Type\> | Init 완료된 상태 추적 |
| `OnStateChanged` | Action\<IState, IState\> | 상태 전환 이벤트 |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `AddState<T>(T)` | void | 상태 등록 |
| `ChangeState<T>()` | void | 상태 전환 (Exit → Init(첫회) → Enter) |
| `GetState<T>()` | T | 등록된 상태 조회 |
| `IsCurrentState<T>()` | bool | 현재 상태 확인 |

---

### HomeState

홈 화면 Presenter. 이벤트 바인딩과 UI/애니메이션 제어를 담당한다.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_exampleQuestions` | static string[] | 랜덤 예시 질문 5종 |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `Init()` | void | 시리얼 수신 + VoiceEmotionAnalyzer 이벤트 구독 |
| `Update()` | void | Space바 입력으로 시리얼 버튼 동작 에뮬레이션 |
| `GetRandomQuestion()` | string | 랜덤 예시 질문 반환 |
| `OnSerialReceive(byte[])` | void | 버튼 press/release 처리 |

애니메이션 트리거 매핑:

| 감정코드 | 트리거 |
|---------|--------|
| 1 (기쁨) | `tHappy` |
| 2 (슬픔) | `tSad` |
| 3 (화남) | `tAngry` |
| 4 (놀람) | `tSurprised` |
| 실패 | `tRefuse` |

---

### BaseView

UI View 추상 베이스.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_rootPanel` | GameObject | Show/Hide 대상 패널 |
| `_showOnAwake` | bool | Awake 시 자동 표시 |
| `_btnPrev/Home/Next` | Button | 네비게이션 버튼 (선택) |

| 이벤트 | 설명 |
|--------|------|
| `OnShow` | View 표시 시 |
| `OnHide` | View 숨김 시 |
| `OnPrevClicked` | 이전 버튼 클릭 |
| `OnHomeClicked` | 홈 버튼 클릭 |
| `OnNextClicked` | 다음 버튼 클릭 |

---

### HomeView

홈 화면 View. Inspector로 컴포넌트를 연결하고 getter로 노출한다.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_voiceEmotionAnalyzer` | VoiceEmotionAnalyzer | AI 파이프라인 |
| `_characterAnimator` | Animator | 캐릭터 애니메이션 |
| `_micEffectAnimator` | Animator | 마이크 이펙트 애니메이션 |
| `_loadingAnimator` | Animator | 로딩 회전 애니메이션 |
| `_imgLoading` | Image | 로딩 이미지 |
| `_txtMsgTop` | TMP_Text | 상단 메시지 |
| `_txtMsgBottom` | TMP_Text | 하단 메시지 |
| `_micCanvasGroup` | CanvasGroup | 마이크 UI 그룹 |
| `_micEffectCanvasGroup` | CanvasGroup | 마이크 이펙트 그룹 |

Awake 초기화: 마이크 이펙트 alpha=0, 로딩 이미지 alpha=0

---

### NavigationManager

FSM 총괄 싱글톤.

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `OnSingletonAwake()` | void | View 활성화 → StateMachine 생성 → 상태 등록 |
| `GoTo<T>()` | void | 상태 전환 |
| `HandleIdleTimeout()` | void | 유휴 타임아웃 → HomeState 전환 |

초기화 순서: ActivateAllViews() → StateMachine 생성 → RegisterState() → Start에서 HomeState 진입

---

### IdleManager

무입력 타임아웃 감지 싱글톤.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_idleTimeout` | float | 타임아웃 (기본 60초) |
| `OnIdleTimeout` | Action | 타임아웃 이벤트 |

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `ResetTimer()` | void | 타이머 리셋 |
| `SetTimeout(float)` | void | 타임아웃 변경 |
| `Pause() / Resume()` | void | 일시정지/재개 |

---

### SerialManager

시리얼 포트 통신 싱글톤.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `_portName` | string | 포트 이름 (config.csv에서 로드) |
| `_baudRate` | int | 통신 속도 (9600) |
| `_useSerial` | bool | 시리얼 통신 사용 여부 (config.csv `bUseSerial`로 설정) |
| `_sendHeader` | byte | 송신 헤더 (0xFA) |
| `_receiveHeader` | byte | 수신 헤더 (0xFA) |
| `SendDataHandler` | Action\<byte[]\> | 송신 이벤트 |
| `ReceiveDataHandler` | Action\<byte[]\> | 수신 이벤트 |

데이터 프로토콜: `[헤더(1byte)][데이터(N bytes)]`

초기화 흐름: `LoadPortFromCSV()` → `bUseSerial`이 false이면 포트 연결·수신 루프를 건너뜀

스레드 안전: 코루틴 수신 → `lock(_lock)` → Queue → Update에서 메인 스레드 처리

---

### PageData / DataRepository / CSVParser

| 클래스 | 역할 |
|--------|------|
| `PageData` | Key-Value 딕셔너리 래퍼 (Get, GetFlag, Has) |
| `DataRepository` | DataConfig.json → CSV 파일 로드 → View별 PageData 매핑 |
| `CSVParser` | StreamingAssets에서 CSV 읽기 (헤더 스킵, 첫 콤마 기준 분할) |

---

## 🔄 6. 데이터 흐름

### AI 파이프라인

```
시리얼 버튼 press (data[1]==1)
    │
    ▼
HomeState.OnSerialReceive()
    ├─ VoiceEmotionAnalyzer.StartRecording()
    └─ MicEffectAnimator.SetBool("isRecording", true)
    │
    ▼ (사용자 발화)
시리얼 버튼 release (data[1]==0)
    │
    ▼
HomeState.OnSerialReceive()
    ├─ MicEffectAnimator.SetBool("isRecording", false)
    └─ VoiceEmotionAnalyzer.StopRecordingAndProcess()
            │
            ▼
        ApplyAutoGain() → ConvertToWav()
            │
            ▼
        AIService.ProcessVoice(wavData)
            │
            ├─ CallClovaSTT()  ──→ Naver STT API
            │       ▼
            ├─ CallOpenAI()    ──→ OpenAI GPT-4o-mini
            │       ▼
            └─ CallClovaTTS()  ──→ Naver TTS API
                    │
                    ▼
            VoiceProcessResponse 반환
                    │
            ┌───────┴───────┐
            │               │
        tts_error?      audio 있음
            │               │
    _OnProcessFailed   _OnProcessComplete
    tRefuse 애니메이션      감정 애니메이션 트리거
            │               │
            │         PlayAudioFromBase64()
            │               │
            │         _OnAudioPlayComplete
            │               │
            └───────┬───────┘
                    │
              _OnReset (UI 초기화)
```

### 데이터 로딩

```
StreamingAssets/DataConfig.json
    │
    ▼
DataRepository.LoadAll()
    │
    ├─ FindType("HomeView") → typeof(HomeView)
    └─ CSVParser.Read("HomeData.csv")
            │
            ▼
        PageData { key → value }
            │
            ▼
    _dataMap[typeof(HomeView)] = pageData
```

---

## 🧭 7. 화면/기능 전환 흐름

### 정상 흐름

```
[대기 상태]
    │  TxtMsgTop: 랜덤 예시 질문
    │  TxtMsgBottom: "버튼을 꾹 누른채로..."
    │  MicCanvasGroup.alpha = 1
    │
    ▼ 버튼 press
[녹음 중]
    │  TxtMsgTop: ""
    │  TxtMsgBottom: "AI 꿈별이가 듣고 있어요!..."
    │  MicEffectAnimator: isRecording = true
    │
    ▼ 버튼 release
[API 처리 중]
    │  TxtMsgBottom: ""
    │  MicCanvasGroup.alpha = 0
    │  ImgLoading.alpha = 1
    │  LoadingAnimator: isAnalyzing = true
    │
    ▼ API 성공
[응답 재생]
    │  ImgLoading.alpha = 0
    │  LoadingAnimator: isAnalyzing = false
    │  CharacterAnimator.SetTrigger(감정 트리거)
    │  TTS MP3 재생
    │
    ▼ 재생 완료 (_OnAudioPlayComplete → _OnReset)
[대기 상태] (새 랜덤 질문 표시)
```

### 예외 흐름

```
[녹음 중] → 녹음 실패 (빈 데이터 / 마이크 타임아웃)
    │  _OnRecordingFailed → _OnReset
    └──→ [대기 상태]

[API 처리 중] → STT/감정분석/TTS 실패
    │  _OnProcessFailed
    │  ImgLoading.alpha = 0
    │  LoadingAnimator: isAnalyzing = false
    │  CharacterAnimator.SetTrigger("tRefuse")
    │  _OnReset
    └──→ [대기 상태]

[어느 상태든] → 60초 무입력
    │  IdleManager.OnIdleTimeout
    │  NavigationManager → GoTo<HomeState>
    └──→ [대기 상태]
```

---

## ♻️ 8. 라이프사이클

### 앱 시작

```
Unity Awake
    │
    ├─ MonoSingleton 초기화 (NavigationManager, IdleManager, SerialManager)
    │
    ├─ NavigationManager.OnSingletonAwake()
    │   ├─ ActivateAllViews()     ← View Awake 트리거
    │   ├─ StateMachine 생성
    │   └─ RegisterState()
    │       ├─ DataRepository 생성 → CSV 로드
    │       └─ HomeState(HomeView, PageData) 등록
    │
    ├─ SerialManager.OnSingletonAwake()
    │   └─ ConnectPort()          ← 시리얼 포트 연결
    │
    ├─ HomeView.Awake()
    │   ├─ base.Awake()           ← rootPanel, 버튼 바인딩
    │   ├─ MicEffectCanvasGroup.alpha = 0
    │   └─ ImgLoading.alpha = 0
    │
    └─ VoiceEmotionAnalyzer.Awake()
        ├─ AudioSource 초기화
        └─ InitMicrophone()

Unity Start
    │
    ├─ NavigationManager.Start()
    │   ├─ GoTo<HomeState>()
    │   │   ├─ HomeState.Init()   ← 이벤트 바인딩 (최초 1회)
    │   │   └─ HomeState.Enter()  ← View Show
    │   └─ IdleManager.OnIdleTimeout 구독
    │
    └─ SerialManager.Start()
        └─ StartCoroutine(ListeningSerialPort())
```

### 상태 전환

```
StateMachine.ChangeState<T>()
    │
    ├─ OldState.Exit()     ← View Hide
    │
    ├─ (첫 진입이면) NewState.Init()
    │
    ├─ NewState.Enter()    ← BindView() → View Show
    │
    └─ OnStateChanged 이벤트 → IdleManager.ResetTimer()
```

### 앱 종료

```
OnApplicationQuit
    │
    ├─ SerialManager.OnSingletonApplicationQuit()
    │   └─ ClosePort()
    │
    └─ StateMachine.OnDestroy()
        └─ 모든 State.Dispose()  ← 이벤트 해제
```

---

## 🧑‍💻 9. 확장 가이드

### 새 화면(State + View) 추가

**Step 1.** View 클래스 생성

```csharp
// Assets/Scripts/View/ContentView.cs
using UnityEngine;
using TMPro;

namespace PagingTemplate.View
{
    public class ContentView : BaseView
    {
        [Header("=== Text ===")]
        [SerializeField] private TMP_Text _txtTitle;

        public TMP_Text TxtTitle => _txtTitle;
    }
}
```

**Step 2.** State 클래스 생성

```csharp
// Assets/Scripts/FSM/States/ContentState.cs
using PagingTemplate.View;
using PagingTemplate.Model;

namespace PagingTemplate.FSM.States
{
    public class ContentState : BaseState<ContentState, ContentView>
    {
        public ContentState(ContentView view, PageData data) : base(view, data) { }

        public override void Init()
        {
            base.Init();
            // 이벤트 바인딩
        }

        public override void Enter()
        {
            base.Enter();
            // 진입 시 UI 리셋
        }

        protected override void OnNextClicked()
        {
            // GoTo<NextState>();
        }
    }
}
```

**Step 3.** NavigationManager에 등록

```csharp
// NavigationManager.RegisterState() 에 추가
var contentView = FindAnyObjectByType<ContentView>();
var contentState = new ContentState(contentView, repo.GetData<ContentView>());
_stateMachine.AddState(contentState);
```

**Step 4.** 전환 연결

```csharp
// HomeState에서 전환
protected override void OnNextClicked()
{
    GoTo<ContentState>();
}
```

### 핵심 규칙

| 규칙 | 이유 |
|------|------|
| View는 SerializeField + getter만 | Presenter(State)가 로직을 담당 |
| Init()은 1회성 바인딩용 | Enter()에 넣으면 중복 구독 발생 |
| 이벤트 발행은 소유 클래스에서만 | C# event 키워드 제약, 외부 Invoke 불가 |
| 싱글톤은 MonoSingleton\<T\> 상속 | DontDestroyOnLoad, 스레드 안전 보장 |
| API 호출에 30초 타임아웃 필수 | 키오스크 무한 대기 방지 |

---

## ⚖️ 10. 설계 결정 및 트레이드오프

### Python 서버 제거 → C# 단일 구조

| 항목 | 내용 |
|------|------|
| 결정 | Python 서버 없이 C#에서 외부 API를 직접 호출 |
| 이유 | 키오스크 환경에서 Python 프로세스 관리 부담 제거, 배포 단순화 |
| 장점 | 단일 실행 파일, 프로세스 관리 불필요, 디버깅 용이 |
| 단점 | C#에서 JSON 수동 구성 (JsonUtility 한계), API 키가 빌드에 포함 |
| 결론 | 키오스크(오프라인 설치) 환경에서 단순함이 안정성보다 중요 |

### 이벤트 기반 아키텍처

| 항목 | 내용 |
|------|------|
| 결정 | VoiceEmotionAnalyzer가 7개 이벤트로 상태를 외부에 전달 |
| 이유 | AI 파이프라인과 UI 로직의 분리 |
| 장점 | 느슨한 결합, HomeState 외 다른 구독자 추가 가능 |
| 단점 | 이벤트 흐름 추적이 코드만으로는 어려움 |
| 결론 | 키오스크 규모에서 충분히 관리 가능한 복잡도 |

### _OnReset 통합 이벤트

| 항목 | 내용 |
|------|------|
| 결정 | 실패/재생완료 모든 종료 경로에서 _OnReset 발행 |
| 이유 | 초기 상태 복귀 로직 중복 제거 |
| 장점 | UI 초기화 코드가 한 곳에 집중 |
| 단점 | 성공/실패 후 복귀를 다르게 처리하려면 별도 분기 필요 |
| 결론 | 현재 요구사항에서는 동일한 복귀 동작이므로 적합 |

### SerializeField + getter 패턴 (View)

| 항목 | 내용 |
|------|------|
| 결정 | View에 private SerializeField + public getter, 로직은 State에서 처리 |
| 이유 | MVP 패턴에서 View는 수동적 역할 |
| 장점 | Inspector 연결 + 외부 읽기 전용, Presenter가 로직 집중 |
| 단점 | getter 보일러플레이트 증가 |
| 결론 | 역할 분리의 명확성이 코드량보다 중요 |

### TTS 실패를 _OnProcessFailed로 통합

| 항목 | 내용 |
|------|------|
| 결정 | TTS 실패 시 별도 이벤트 없이 _OnProcessFailed로 처리 |
| 이유 | 텍스트 응답만 있고 음성이 없는 상태는 키오스크 체험에 부적합 |
| 장점 | 실패 처리 경로 단순화, tRefuse 애니메이션으로 시각적 피드백 |
| 단점 | 텍스트 응답이 있어도 실패로 간주 |
| 결론 | 키오스크 체험 품질 기준으로 음성 없는 응답은 실패가 맞음 |
