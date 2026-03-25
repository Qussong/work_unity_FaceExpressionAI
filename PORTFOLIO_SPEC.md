# 🎭 감정 AI 대화 키오스크

> **Unity (C#) · FSM + MVP · Naver Clova STT/TTS · OpenAI GPT-4o-mini · 시리얼 통신**
>
> 음성으로 대화하면 AI가 감정을 분석하고, 캐릭터가 공감 응답을 들려주는 과학관 체험 키오스크

---

# 🧾 프로젝트 요약

| 항목 | 내용 |
|------|------|
| 프로젝트명 | 감정 AI 대화 키오스크 |
| 클라이언트 | 인천 학생과학관 |
| 플랫폼 | Windows PC (키오스크) |
| 엔진 / 언어 | Unity / C# |
| 설계 패턴 | FSM + MVP + Singleton + Observer |
| 외부 API | Naver Clova STT, OpenAI GPT-4o-mini, Naver Clova TTS |
| 하드웨어 | 시리얼 통신 물리 버튼 (9600bps) |
| 배포 환경 | 과학관 현장 키오스크 (오프라인 상시 운영) |

---

# 🚀 프로젝트 소개

인천 학생과학관에 설치되는 감정 인식 AI 대화 키오스크. 체험자가 물리 버튼을 누르고 음성으로 말하면, AI가 감정을 분석해 캐릭터 애니메이션과 공감 음성으로 응답한다.

- 🎤 물리 버튼 press/release로 음성 녹음 제어
- 🧠 STT → 감정 분석(5종) → TTS 3단계 AI 파이프라인
- 🎬 감정별 캐릭터 애니메이션 + 음성 응답
- ⏱ 60초 무입력 시 자동 홈 복귀

**Python 서버 없이 Unity C# 단일 구조로 3개 외부 API를 직접 호출하는 경량 아키텍처**가 특징이다.

---

# 🎯 해결하려는 문제

| 문제 | 설명 |
|------|------|
| 키오스크 안정성 | 과학관에서 상시 운영되므로 프로세스 관리 실패 = 체험 중단 |
| 다중 프로세스 복잡도 | Python 서버 + Unity 클라이언트 구조는 배포·관리 부담 |
| 무한 대기 위험 | 외부 API 응답 지연 시 키오스크가 멈추는 문제 |
| 하드웨어 연동 | 물리 버튼의 시리얼 데이터를 Unity 메인 스레드와 안전하게 연결 |

**해결 전략:**

- C# 단일 구조로 전환 — Python 서버 제거, UnityWebRequest로 API 직접 호출
- 모든 API 호출에 30초 타임아웃 적용
- 시리얼 수신을 스레드 안전 큐로 메인 스레드에 전달
- 이벤트 기반 아키텍처로 실패 경로마다 자동 복귀 보장

---

# 🧩 핵심 기능

### 🎤 음성 녹음 + 자동 게인 보정

마이크 48kHz 모노 캡처, 소음 환경에서도 인식률을 높이기 위해 자동 게인 보정(목표 0.35, 최대 150배) 적용

### 🧠 3단계 AI 파이프라인

Clova STT(음성→텍스트) → GPT-4o-mini(감정 분류 + 공감 응답) → Clova TTS(감정 반영 음성 합성)

### 🎬 감정별 캐릭터 반응

5종 감정(기쁨/슬픔/화남/놀람/중립) 분류 후 Animator 트리거로 캐릭터 애니메이션 재생, 실패 시 거절 애니메이션

### 🔌 시리얼 통신 버튼

물리 버튼의 press/release를 시리얼 포트로 수신, config.csv로 포트명·사용 여부를 런타임 설정

### ⏱ 유휴 감지 자동 복귀

60초 무입력 감지 시 자동으로 홈 화면 복귀, 키오스크 무인 운영 보장

---

# 🧠 시스템 아키텍처

```
┌─────────────────────────────────┐
│         Hardware Layer          │
│     SerialManager (물리 버튼)    │
└───────────────┬─────────────────┘
                │ 이벤트
┌───────────────▼─────────────────┐
│          FSM Layer              │
│  NavigationManager → StateMachine│
│       HomeState (Presenter)     │
└────┬──────────────────────┬─────┘
     │                      │
┌────▼──────┐        ┌──────▼──────┐
│ View Layer│        │  AI Layer   │
│ HomeView  │◄ 이벤트 │ VoiceEmotion│
│ (UI 노출) │        │  Analyzer   │
└───────────┘        │  AIService  │
                     └──────┬──────┘
                            │ HTTP
                     ┌──────▼──────┐
                     │ External API│
                     │ Clova STT   │
                     │ OpenAI GPT  │
                     │ Clova TTS   │
                     └─────────────┘
```

**설계 목표:**

- **단일 실행 파일** — Python 서버 없이 Unity만으로 배포
- **느슨한 결합** — 이벤트 기반으로 AI 파이프라인과 UI 로직 분리
- **안정성 우선** — 모든 실패 경로에서 자동 복귀, 타임아웃 필수

---

# 🔄 화면 흐름

```
[대기] ──버튼 press──→ [녹음 중] ──버튼 release──→ [API 처리]
  ▲                                                    │
  │                                          ┌─────────┴─────────┐
  │                                        성공               실패
  │                                          │                  │
  │                                    [응답 재생]        [거절 애니메이션]
  │                                     TTS + 캐릭터       tRefuse
  │                                          │                  │
  └──────────── _OnReset (UI 초기화) ─────────┴──────────────────┘
```

**UX 특징:**

- 물리 버튼 누르는 동안만 녹음 → 직관적 상호작용
- 실패 시에도 캐릭터가 반응(거절 애니메이션) → 체험 흐름 유지
- 모든 종료 경로가 _OnReset으로 통합 → 대기 상태 자동 복귀
- 60초 무입력 시 IdleManager가 홈으로 리셋

---

# 🏗 핵심 설계

### FSM (Finite State Machine)

화면 단위로 상태를 관리한다. 상태 전환 시 `Exit → Init(최초 1회) → Enter` 라이프사이클을 보장한다.

```
StateMachine.ChangeState<T>()
  → OldState.Exit()        // View 숨김
  → NewState.Init()        // 이벤트 바인딩 (첫 진입만)
  → NewState.Enter()       // View 표시
  → OnStateChanged 이벤트   // IdleManager 리셋
```

- **Init/Enter 분리**: 이벤트 중복 구독 방지
- **Type 기반 Dictionary**: 제네릭으로 타입 안전한 상태 전환

### MVP (Model-View-Presenter)

BaseState가 Presenter, BaseView가 View, PageData가 Model 역할. View는 SerializeField + getter만 노출하고 로직은 State에서 처리.

- **역할 분리 명확**: View는 수동적, Presenter가 모든 로직 담당
- **CSV 기반 데이터**: DataConfig.json → CSV → PageData로 런타임 데이터 로딩

### Observer (이벤트 기반)

VoiceEmotionAnalyzer가 7개 `event Action`으로 파이프라인 상태를 외부에 전달한다.

```
_OnRecordingStarted → _OnRecordingStopped
  ├─ _OnRecordingFailed → _OnReset
  └─ _OnProcessComplete → _OnAudioPlayComplete → _OnReset
     또는 _OnProcessFailed → _OnReset
```

- **느슨한 결합**: AI 로직과 UI 로직 완전 분리
- **확장 가능**: HomeState 외 다른 구독자 추가 가능

---

# 🧰 기술 스택

| 분류 | 기술 |
|------|------|
| 엔진 | Unity |
| 언어 | C# |
| UI | Unity UI (Canvas, TMP_Text, CanvasGroup) |
| 애니메이션 | Unity Animator (트리거/Bool 파라미터) |
| HTTP 통신 | UnityWebRequest |
| 시리얼 통신 | System.IO.Ports.SerialPort |
| STT | Naver Clova Speech |
| 감정 분석 | OpenAI GPT-4o-mini |
| TTS | Naver Clova Voice Premium |
| 데이터 | JSON + CSV (StreamingAssets) |

---

# ⚙ 주요 시스템

### NavigationManager
FSM 총괄 싱글톤 — StateMachine 소유, 상태 등록/전환, 유휴 타임아웃 시 홈 복귀

### AIService
외부 API 직접 호출 — Clova STT → OpenAI GPT → Clova TTS 순차 코루틴 파이프라인

### VoiceEmotionAnalyzer
마이크 녹음 + 오디오 처리 + 파이프라인 오케스트레이션, 7개 이벤트로 상태 전달

### SerialManager
시리얼 포트 통신 싱글톤 — 코루틴 수신 → 스레드 안전 큐 → 메인 스레드 콜백, config.csv로 ON/OFF 전환

### IdleManager
무입력 타임아웃 감지 — 마우스/키보드/터치 입력 모니터링, 60초 무입력 시 이벤트 발행

---

# 📊 데이터 흐름

```
[물리 버튼 press]
    ↓
마이크 녹음 (48kHz 모노) → 자동 게인 보정 → WAV 16-bit PCM
    ↓
Clova STT API → 텍스트
    ↓
OpenAI GPT-4o-mini → 감정코드(1~4) + 공감응답
    ↓
Clova TTS API → MP3 음성
    ↓
캐릭터 애니메이션 + TTS 재생 → _OnReset (복귀)
```

---

# 🧑‍💻 기술적 도전

## 1️⃣ Python 서버 제거 → C# 단일 구조 전환

### 문제점
기존 Python 서버 + Unity 클라이언트 구조에서 Python 프로세스가 비정상 종료되면 키오스크 전체가 멈추는 문제. 과학관 현장에서 프로세스 관리 인력이 없음.

### 해결
UnityWebRequest로 3개 외부 API(Clova STT, OpenAI, Clova TTS)를 C#에서 직접 호출하는 단일 구조로 전환. JsonUtility의 중첩 구조 한계는 JSON 문자열 수동 구성으로 해결. 모든 API 호출에 30초 타임아웃 적용.

## 2️⃣ 시리얼 통신 스레드 안전 처리

### 문제점
시리얼 포트 수신은 별도 스레드에서 동작하지만, Unity API(Animator, UI)는 메인 스레드에서만 접근 가능.

### 해결
코루틴 기반 수신 루프에서 `lock`으로 보호된 `Queue<byte[]>`에 데이터를 넣고, `Update()`에서 큐를 소비하여 메인 스레드로 전달. 버퍼 복사로 데이터 무결성 보장.

## 3️⃣ 모든 실패 경로에서 자동 복귀

### 문제점
키오스크가 오류 상태에서 멈추면 다음 체험자가 이용 불가. 녹음 실패, STT 실패, 감정분석 실패, TTS 실패 등 실패 지점이 다양함.

### 해결
`_OnReset` 통합 이벤트를 도입하여 모든 종료 경로(성공/실패)에서 동일하게 UI 초기화. 녹음 실패 → `_OnRecordingFailed` → `_OnReset`, API 실패 → `_OnProcessFailed` → `_OnReset`, 재생 완료 → `_OnAudioPlayComplete` → `_OnReset`.

## 4️⃣ 소음 환경에서의 음성 인식률 개선

### 문제점
과학관 환경은 주변 소음이 크고, 체험자(학생)의 음성이 작을 수 있음.

### 해결
자동 게인 보정 알고리즘 적용. 녹음된 오디오의 최대 레벨을 측정한 뒤, 목표 레벨(0.35)에 맞춰 최대 150배까지 증폭. 클리핑 방지를 위해 -1~1 범위로 클램핑.

---

# ⚖ 설계 트레이드오프

| 선택 | 이유 |
|------|------|
| Python 서버 제거 → C# 단일 구조 | 키오스크 안정성 > API 키 보안 (오프라인 설치 환경) |
| Init/Enter 라이프사이클 분리 | 이벤트 중복 구독 방지 > 단순한 상태 진입 |
| TTS 실패 = 전체 실패 처리 | 체험 품질 기준으로 음성 없는 응답은 불완전 |
| _OnReset 통합 이벤트 | UI 초기화 코드 집중 > 성공/실패별 분기 유연성 |
| View에 로직 없음 (SerializeField + getter만) | MVP 역할 분리 명확성 > getter 보일러플레이트 |
| config.csv로 시리얼 ON/OFF | 개발 환경에서 하드웨어 없이 테스트 가능 |

---

# 🎯 프로젝트 결과

- Python 서버 제거로 **배포 단순화** — 단일 Unity 빌드 파일로 현장 설치
- 모든 실패 경로에서 **자동 복귀** 보장 — 키오스크 무인 운영 가능
- 3개 외부 API(STT + 감정분석 + TTS) **순차 파이프라인** 안정 동작
- 물리 버튼 + 음성 + 캐릭터 애니메이션으로 **몰입감 있는 체험** 제공

---

# 📌 배운 점

- **키오스크 환경의 안정성 설계** — 모든 실패 지점에 복귀 경로를 만드는 방어적 프로그래밍의 중요성
- **이벤트 기반 아키텍처** — Observer 패턴으로 AI 파이프라인과 UI를 분리하면 각각 독립적으로 수정 가능
- **FSM + MVP 조합** — 상태 기반 화면 관리와 Presenter 패턴을 결합하면 화면 추가가 정형화됨
- **스레드 안전 설계** — 시리얼 통신처럼 메인 스레드 외부 데이터는 큐 + lock 패턴으로 안전하게 전달

---

# 🔮 향후 개선

- 감정 분류 정확도 향상을 위한 음성 톤 분석(Pitch/Energy) 추가
- 다국어 지원 (영어/일본어 STT + TTS 전환)
- 체험 데이터 로깅 및 통계 대시보드
- 캐릭터 립싱크 + 표정 애니메이션 고도화
