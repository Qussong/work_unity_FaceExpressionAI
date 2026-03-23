# 감정 AI 대화 키오스크

> 인천 학생과학관 체험 프로그램 — 음성으로 대화하면 AI가 감정을 분석하고 캐릭터가 공감 응답을 들려줍니다.

`[📷 이미지 배치 예정 - 키오스크 메인 화면 스크린샷]`

| 항목 | 내용 |
|------|------|
| 클라이언트 | 인천 학생과학관 |
| 플랫폼 | Windows PC (키오스크) |
| 엔진 | Unity (C#) |
| 백엔드 | Python Flask (`localhost:5000`) |
| 설계 패턴 | PagingTemplate (FSM + MVP + 싱글톤) |
| 외부 API | Naver Clova STT/TTS, OpenAI GPT-4o-mini |

---

## 화면 구성

| 홈 화면 |
|---------|
| `[📷 이미지 배치 예정 - 홈 화면]` |

```
[HomeState] ←─ 단일 화면에서 모든 흐름 처리
    │  녹음 버튼 클릭 (화면 or 시리얼 포트 물리 버튼)
    │  → 음성 녹음 → 서버 전송 → 결과 표시 + 캐릭터 애니메이션
    │  → 대기 상태로 자동 복귀
    └── (60초 무입력 시 IdleManager가 홈으로 리셋)
```

---

## 핵심 기능

| 기능 | 설명 |
|------|------|
| 음성 녹음 | 마이크 48kHz 모노 캡처, 자동 게인 보정 (목표 0.35, 최대 150배) |
| STT | Naver Clova Speech — 한국어 음성→텍스트 변환 |
| 감정 분석 | OpenAI GPT-4o-mini — 4종 감정 분류 + 20자 내외 공감 응답 생성 |
| TTS | Naver Clova Voice Premium — 감정별 음성 합성 (화자: nara) |
| 캐릭터 반응 | 감정 코드별 애니메이션 트리거 (tHappy/tSad/tAngry/tSurprised) |
| 하드웨어 버튼 | 시리얼 포트(COM) 통신으로 물리 버튼 입력 지원 |
| 유휴 감지 | 60초 무입력 시 자동 홈 복귀 |

---

## 아키텍처

PagingTemplate 네임스페이스 기반 FSM + MVP + 싱글톤 패턴

| 레이어 | 역할 | 주요 클래스 |
|--------|------|------------|
| **FSM** | 상태 전환 관리 | `StateMachine`, `IState`, `BaseState<TState, TView>`, `HomeState` |
| **View** | UI 패널 표시/숨김, 네비게이션 버튼 | `BaseView`, `HomeView` |
| **Model** | CSV 기반 데이터 로딩 및 View별 매핑 | `DataRepository`, `PageData` |
| **Manager** | 싱글톤 시스템 관리 | `NavigationManager`, `IdleManager` |
| **AI** | Python 서버 통신 (STT→감정분석→TTS) | `VoiceEmotionAnalyzer`, `VoiceProcessResponse` |
| **Utility** | 싱글톤 베이스, CSV 파서 | `MonoSingleton<T>`, `CSVParser` |

---

## 디렉토리 구조

```
Assets/
├── Scripts/
│   ├── AI/
│   │   └── ver3_tts_stt/
│   │       └── VoiceEmotionAnalyzer.cs   # 음성 파이프라인 (녹음→서버→재생)
│   ├── FSM/
│   │   ├── IState.cs                     # 상태 인터페이스
│   │   ├── BaseState.cs                  # 제네릭 상태 베이스 (MVP Presenter)
│   │   ├── StateMachine.cs               # 상태 등록/전환 엔진
│   │   └── States/
│   │       └── HomeState.cs              # 홈 화면 상태
│   ├── View/
│   │   ├── BaseView.cs                   # View 추상 베이스 (Show/Hide/네비 버튼)
│   │   └── HomeView.cs                   # 홈 화면 View
│   ├── Model/
│   │   ├── PageData.cs                   # View별 key-value 데이터 컨테이너
│   │   └── DataRepository.cs             # DataConfig.json → CSV → PageData 변환
│   ├── Manager/
│   │   ├── NavigationManager.cs          # FSM 총괄 싱글톤
│   │   └── IdleManager.cs                # 무입력 타임아웃 감지
│   └── Util/
│       ├── MonoSingleton.cs              # 제네릭 싱글톤 베이스
│       └── CSVParser.cs                  # StreamingAssets CSV 로더
├── Scenes/
│   ├── PagingTemplate.unity              # 메인 씬
│   └── GuideKioskTemplate.unity          # 가이드 키오스크 템플릿
└── StreamingAssets/
    └── DataConfig.json                   # View↔CSV 매핑 설정
```

---

## 데이터 흐름

```
1. 사용자 버튼 클릭 (화면 UI or 시리얼 포트 물리 버튼)
      ↓
2. VoiceEmotionAnalyzer.StartRecording()
   마이크 캡처 (48kHz 모노) → 자동 게인 보정 → WAV 16-bit PCM → Base64
      ↓
3. POST /process-voice → Python Flask 서버 (localhost:5000)
      ↓
4. [Python] Clova STT → GPT-4o-mini 감정분석 → Clova TTS
      ↓
5. VoiceProcessResponse 수신 → UI 업데이트 + 캐릭터 애니메이션 + MP3 재생
```

**서버 응답 형식:**

```json
{
  "recognized_text": "인식된 텍스트",
  "emotion": 1,
  "response": "공감 응답",
  "audio": "base64 MP3"
}
```

**감정 코드 매핑:**

| 코드 | 감정 | 애니메이션 트리거 | Clova TTS 감정 |
|------|------|------------------|---------------|
| 1 | 기쁨 | `tHappy` | 2 (기쁨) |
| 2 | 슬픔 | `tSad` | 1 (슬픔) |
| 3 | 화남 | `tAngry` | 3 (분노) |
| 4 | 놀람 | `tSurprised` | 0 (중립) |

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-03-23 | 프로젝트 전면 리팩토링 계획 수립 — FSM 페이징 구조 제거 및 단일 화면 전환, 통신 타임아웃 추가 예정 |
| 2026-03-16 | 전체 C# 소스 코드 정리 — 불필요 코드 제거, XML 주석 추가, 인코딩 오류 수정 |
| 2026-03-16 | README.md 초기 작성 |
