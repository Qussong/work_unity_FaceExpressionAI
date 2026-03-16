# work_unity_FaceExpressionAI

인천 학생과학관 - 감정 표현 AI 키오스크 애플리케이션

- **플랫폼**: PC (Windows)
- **엔진**: Unity (C#)
- **백엔드**: Python Flask 서버 (`http://localhost:5000`)
- **핵심 기능**: 음성 녹음 → STT → 감정 분석 → TTS → 캐릭터 애니메이션 반응

---

## 아키텍처

FSM(유한 상태 기계) + MVC 레이어 분리 구조

| 레이어 | 역할 |
|--------|------|
| **FSM (States)** | 화면 전환 흐름 관리 (Start → Content → Result) |
| **View** | UI 패널 표시/숨김 및 애니메이션 제어 |
| **AI Analyzer** | Python 서버와 통신하여 STT·감정 분석·TTS 수행 |
| **Managers** | 싱글톤 시스템 관리 (상태, 유휴 감지, 시리얼 통신) |
| **Utilities** | 공통 유틸리티 (싱글톤 베이스, UI 페이드, CSV 파싱) |

---

## 디렉토리 구조

```
Assets/
├── Scripts/
│   ├── AI/
│   │   ├── ver1/              # 텍스트 입력 → 감정 분석
│   │   ├── ver2_tts/          # 텍스트 입력 + TTS 음성 출력
│   │   └── ver3_tts_stt/      # 음성 입력 + STT + 감정 분석 + TTS (완성판)
│   ├── FSM/
│   │   ├── IState.cs
│   │   ├── BaseState.cs
│   │   ├── StateMachine.cs
│   │   └── States/
│   │       ├── StartState.cs
│   │       ├── ContentState.cs
│   │       └── ResultState.cs
│   ├── View/
│   │   ├── BaseView.cs
│   │   ├── StartView.cs
│   │   ├── ContentView.cs
│   │   └── ResultView.cs
│   ├── NavigationManager.cs   # FSM 총괄 싱글톤
│   ├── IdleManager.cs         # 유휴 타임아웃 처리
│   ├── SerialManager.cs       # 외부 버튼 시리얼 통신
│   ├── CanvasGroupExtensions.cs
│   ├── CSVParser.cs
│   └── Util/
│       └── MonoSingleton.cs
├── Scenes/
│   ├── FaceExpressionAI.unity # 메인 프로덕션 씬
│   ├── emotionAI.unity        # 감정 AI 테스트 씬
│   └── GuideKioskTemplate.unity
├── Animation/
├── Model/
├── Sprites/
└── Resources/
```

---

## 주요 클래스

### FSM 코어

| 클래스 | 역할 |
|--------|------|
| `IState` | 상태 인터페이스 (Enter / Update / Exit) |
| `BaseState<TState, TView>` | 제네릭 상태 베이스 - View 바인딩 포함 |
| `StateMachine` | 상태 등록 및 전환 관리 |
| `StartState` | 시작 화면 상태 (Space 키로 디버그 메뉴) |
| `ContentState` | 메인 인터랙션 상태 |
| `ResultState` | 결과 표시 상태 |

### View 레이어

| 클래스 | 역할 |
|--------|------|
| `BaseView` | Show / Hide / Toggle 추상 베이스 |
| `StartView` | 시작 화면 UI (디버그 컨테이너, 마이크 이미지) |
| `ContentView` | 콘텐츠 표시 영역 |
| `ResultView` | 결과 표시 영역 |

### AI 분석기 (3단계 버전)

| 클래스 | 버전 | 기능 |
|--------|------|------|
| `EmotionAnalyzer` | v1 | 텍스트 → 감정 코드 + AI 응답 |
| `EmotionAnalyzerTTS` | v2 | 텍스트 → 감정 + AI 응답 + TTS 음성 |
| `VoiceEmotionAnalyzer` | v3 | 마이크 녹음 → STT → 감정 + TTS 음성 |

### UI 컨트롤러

| 클래스 | 역할 |
|--------|------|
| `EmotionTestUI` | v1 텍스트 입력 테스트 UI |
| `EmotionTTSTestUI` | v2 텍스트+TTS 테스트 UI |
| `VoiceEmotionTestUI` | v3 전체 음성 파이프라인 UI (캐릭터 애니메이션 포함) |

### 매니저 및 유틸

| 클래스 | 역할 |
|--------|------|
| `NavigationManager` | FSM + 상태 등록 총괄 싱글톤 |
| `IdleManager` | 60초 유휴 감지 → StartState로 복귀 |
| `SerialManager` | 외부 버튼 컨트롤러 시리얼 통신 (스레드 안전 큐) |
| `MonoSingleton<T>` | 제네릭 싱글톤 베이스 클래스 |
| `CanvasGroupExtensions` | UI FadeIn / FadeOut / Activate 유틸 |
| `CSVParser` | StreamingAssets CSV 로드 및 파싱 |

---

## 화면 흐름

```
[StartState]
    │  (사용자 인터랙션 또는 버튼 입력)
    ▼
[ContentState]  ← 음성 녹음 / 감정 분석 진행
    │  (분석 완료)
    ▼
[ResultState]   ← 결과 표시 + 캐릭터 감정 애니메이션
    │  (60초 유휴 or 재시작)
    ▼
[StartState]    ← IdleManager 타임아웃 시 자동 복귀
```

**디버그**: StartState에서 Space 키 → 디버그 컨테이너 토글

---

## 데이터 흐름 (v3 전체 음성 파이프라인)

```
1. 사용자가 버튼 클릭 (화면 or 시리얼 포트 하드웨어 버튼)
        ↓
2. VoiceEmotionAnalyzer.StartRecording()
   - Microphone.Start() → 48kHz 모노 캡처
        ↓
3. 녹음 종료 (버튼 재클릭 or 최대 10초)
   - 오디오 샘플 추출
   - 자동 게인 보정 (목표 레벨 0.35, 최대 150x 부스트)
   - WAV 변환 (16-bit PCM 모노)
   - Base64 인코딩
        ↓
4. POST /process-voice → Python Flask 서버
        ↓
5. [Python 서버]
   - STT: 음성 → 텍스트 (Clova/Naver STT)
   - 감정 분석: 텍스트 → 감정 코드 (1=기쁨, 2=슬픔, 3=화남, 4=놀람)
   - TTS: AI 응답 텍스트 → MP3 (Base64)
        ↓
6. VoiceProcessResponse 수신
   - UI 업데이트 (인식 텍스트, 감정, AI 응답)
   - 캐릭터 애니메이터 트리거 (tHappy / tSad / tAngry / tSurprised)
   - Base64 디코딩 → 임시 파일 → AudioSource 재생
```

**감정 코드 매핑**

| 코드 | 감정 | 애니메이터 트리거 |
|------|------|-----------------|
| 1 | 기쁨 (Joy) | `tHappy` |
| 2 | 슬픔 (Sadness) | `tSad` |
| 3 | 화남 (Anger) | `tAngry` |
| 4 | 놀람 (Surprise) | `tSurprised` |

---

## Python 백엔드 서버 (emotion_server.py)

Flask 기반 로컬 API 서버. Unity 클라이언트가 HTTP로 호출하며 STT·감정 분석·TTS 세 가지 외부 API를 조합하여 처리합니다.

### 사용 외부 API

| 서비스 | 용도 | 비고 |
|--------|------|------|
| **OpenAI GPT-4o-mini** | 텍스트 감정 분석 + 공감 응답 생성 | JSON 응답 강제 (`response_format: json_object`) |
| **Naver Clova Speech** | 한국어 STT (음성 → 텍스트) | `recog/v1/stt?lang=Kor` |
| **Naver Clova Voice Premium** | TTS (텍스트 → MP3 음성) | speaker: `nara`, emotion 파라미터 지원 |

### API 엔드포인트

| 메서드 | 경로 | 입력 | 출력 | 설명 |
|--------|------|------|------|------|
| POST | `/process-voice` | `{"audio": "base64 WAV"}` | recognized_text, emotion, response, audio | 전체 파이프라인 (STT→분석→TTS) |
| POST | `/stt` | `{"audio": "base64 WAV"}` | `{"text": "인식 텍스트"}` | STT만 (테스트용) |
| POST | `/analyze` | `{"text": "입력 텍스트"}` | emotion, response | 텍스트 감정 분석만 |
| POST | `/analyze-with-tts` | `{"text": "입력 텍스트"}` | emotion, response, audio | 텍스트 감정 분석 + TTS |
| GET | `/health` | - | `{"status": "ok"}` | 서버 상태 확인 |

### 감정별 TTS 감정 파라미터 매핑

Clova Voice는 자체 감정 코드 체계를 사용하므로 Unity 감정 코드에서 변환합니다.

| Unity 감정 코드 | 감정 | Clova emotion 파라미터 |
|----------------|------|----------------------|
| 1 | 기쁨 | 2 (기쁨) |
| 2 | 슬픔 | 1 (슬픔) |
| 3 | 화남 | 3 (분노) |
| 4 | 놀람 | 0 (중립) |

### 시스템 프롬프트 구조

OpenAI에 JSON 응답만 반환하도록 강제합니다:

```
{
  "emotion": 감정코드(1~4),
  "response": "20자 내외 공감 응답"
}
```

### 오디오 처리 흐름

```
Unity (WAV Base64)
    ↓ POST /process-voice
Flask 서버
    ├─ Base64 디코딩 → audio_bytes
    ├─ 최소 크기 검증 (< 1000 bytes → 에러)
    ├─ debug_audio.wav 로컬 저장 (디버깅용)
    ├─ Clova STT → recognized_text
    ├─ 빈 텍스트 검증
    ├─ OpenAI → emotion + response
    └─ Clova TTS → MP3 Base64
        ↓ JSON 응답
Unity (MP3 Base64 → AudioSource 재생)
```

### 서버 실행

```bash
python emotion_server.py
# 서버: http://localhost:5000
```

필요 패키지: `flask`, `httpx`

---

## 변경 이력

| 날짜 | 변경 내용 |
|------|----------|
| 2026-03-16 | README.md 초기 작성 - 전체 아키텍처 및 Python 백엔드 문서화 |
| 2026-03-16 | 전체 C# 소스 코드 정리 - 불필요 코드 제거(UsageExample 클래스, TestCode 등), XML 주석 추가, 인코딩 오류 7개 파일 수정, NavigationManager ContentState/ResultState 활성화 |
