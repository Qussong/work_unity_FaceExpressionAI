# 인천 학생과학관 - 감정 AI 대화 키오스크 (Unity 클라이언트)

## 프로젝트 개요
인천 학생과학관에 설치되는 감정 인식 AI 대화 키오스크.
체험자가 음성으로 말하면 AI가 감정을 분석하고 공감 응답을 캐릭터 애니메이션+음성으로 들려주는 체험형 프로그램.

## 시스템 구성

### Unity 클라이언트 (이 프로젝트)
- **역할**: UI, 마이크 녹음, 오디오 처리, 캐릭터 애니메이션, TTS 재생
- **통신**: HTTP REST (JSON) → `http://localhost:5000`

### Python 서버 (별도 프로젝트)
- **경로**: `D:\Giworks Project\1_인천 학생과학관\emotion-ai\emotion_server_full.py`
- **실행**: `sub.bat` 배치파일로 venv 활성화 후 실행
- **역할**: Naver Clova STT → OpenAI GPT-4o-mini 감정분석 → Naver Clova TTS
- **포트**: 5000

## 통신 프로토콜

### 메인 엔드포인트: POST /process-voice
**요청**:
```json
{"audio": "base64 인코딩된 WAV"}
```

**응답** (`VoiceProcessResponse`):
```json
{
  "recognized_text": "STT 인식 결과",
  "emotion": 1,
  "response": "AI 공감 응답",
  "audio": "base64 MP3",
  "error": null,
  "tts_error": null
}
```

### 감정 코드
| 코드 | 감정 | 애니메이션 트리거 |
|------|------|------------------|
| 1 | 기쁨 | tHappy |
| 2 | 슬픔 | tSad |
| 3 | 화남 | tAngry |
| 4 | 놀람 | tSurprised |

## 오디오 처리 스펙
- 마이크: 48kHz, 모노, 16-bit PCM
- 최대 녹음: 10초
- 자동 게인 보정: 목표 0.35, 최대 150배 증폭
- TTS 재생: base64 MP3 → 임시 파일 → UnityWebRequest로 로드

## 하드웨어 연동
- SerialManager: 외부 물리 버튼을 시리얼 포트(COM)로 수신
- 패킷 헤더: 0xFA, data[1] == 1이면 녹음 시작/중지 토글

## 리팩토링 계획 (2026-03-23~)
### 제거 대상
- FSM 구조 (StateMachine, StartState, ContentState, ResultState)
- NavigationManager
- View 시스템 (BaseView, StartView, ContentView, ResultView)
- IdleManager (페이징 없으므로 불필요)
- 기존 Scene 파일들 (새로 생성)

### 유지 대상
- VoiceEmotionAnalyzer (핵심 음성 파이프라인) — 타임아웃 추가 필요
- VoiceEmotionTestUI (단일 화면 UI 컨트롤러) — 단순화 필요
- SerialManager (하드웨어 버튼)
- MonoSingleton (유틸리티)
- CanvasGroupExtensions (유틸리티)

### 수정 사항
- UnityWebRequest 타임아웃 추가 (현재 무한 대기 → 30초 제한)
- 타임아웃/에러 시 UI 자동 복원 (대기 상태로 되돌림)
- 단일 화면 구조로 전환 (페이지 전환 없음)

## 코딩 컨벤션
- C# 스크립트 경로: `Assets/Scripts/`
- AI 관련 스크립트: `Assets/Scripts/AI/ver3_tts_stt/` (현재 버전)
- 한국어 주석/로그 사용
- 이벤트 기반 아키텍처 (Action 이벤트)
