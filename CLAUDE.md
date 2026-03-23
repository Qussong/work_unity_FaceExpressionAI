# 인천 학생과학관 - 감정 AI 대화 키오스크

## 프로젝트 개요
인천 학생과학관에 설치되는 감정 인식 AI 대화 키오스크.
체험자가 음성으로 말하면 AI가 감정을 분석하고 공감 응답을 캐릭터 애니메이션+음성으로 들려주는 체험형 프로그램.

## 시스템 구성

Unity 단일 구조 — Python 서버 없이 C#에서 외부 API를 직접 호출한다.

### 외부 API (EmotionAIService에서 직접 호출)
| API | 용도 |
|-----|------|
| Naver Clova STT | 한국어 음성 → 텍스트 변환 |
| OpenAI GPT-4o-mini | 감정 분류(1~4) + 공감 응답 생성 |
| Naver Clova Voice Premium | 감정 반영 TTS (화자: nara) |

### 감정 코드
| 코드 | 감정 | 애니메이션 트리거 | Clova 감정 파라미터 |
|------|------|------------------|-------------------|
| 1 | 기쁨 | tHappy | 2 |
| 2 | 슬픔 | tSad | 1 |
| 3 | 화남 | tAngry | 3 |
| 4 | 놀람 | tSurprised | 0 |

## AI 파이프라인

```
마이크 녹음 (48kHz 모노)
  → 자동 게인 보정 (목표 0.35, 최대 150배)
  → WAV 16-bit PCM 변환
  → Clova STT API (30초 타임아웃)
  → OpenAI GPT API (30초 타임아웃)
  → Clova TTS API (30초 타임아웃)
  → MP3 재생 + 캐릭터 애니메이션
```

### 핵심 클래스
| 클래스 | 역할 |
|--------|------|
| `EmotionAIService` | 3개 외부 API 직접 호출 (STT→감정분석→TTS) |
| `VoiceEmotionAnalyzer` | 마이크 녹음, 오디오 처리, 파이프라인 오케스트레이션 |

## 오디오 처리 스펙
- 마이크: 48kHz, 모노, 16-bit PCM
- 최대 녹음: 10초
- 자동 게인 보정: 목표 0.35, 최대 150배 증폭
- TTS 재생: base64 MP3 → 임시 파일 → UnityWebRequest로 로드

## 코딩 컨벤션
- C# 스크립트 경로: `Assets/Scripts/`
- AI 서비스: `Assets/Scripts/AI/EmotionAIService.cs`
- 음성 분석기: `Assets/Scripts/AI/ver3_tts_stt/VoiceEmotionAnalyzer.cs`
- 한국어 주석/로그 사용
- 이벤트 기반 아키텍처 (Action 이벤트)
- 모든 외부 API 호출에 타임아웃 30초 설정
