# 초능력 오셀로 — 참고용 샘플 코드 (약 3일 차 진행 상태)

설계서의 4.2 일정 기준으로 **2주차 초반**쯤 와 있는 상태를 가정해서 만든 코드입니다.
방향 잡기용이라, 그대로 제출하지 말고 구조와 흐름을 참고해서 직접 고쳐 나가세요.

> ⚠ 작성한 환경에 .NET이 없어서 **한 번도 빌드해 보지 않았습니다.**
> Visual Studio에서 처음 빌드할 때 자잘한 오류가 날 수 있어요.
> 그럴 땐 Claude Code에 "빌드 오류 고쳐 줘"라고 하면 됩니다.

## 여는 법

1. 이 폴더 전체를 원하는 위치에 풉니다.
2. `Othello.sln`을 Visual Studio 2022로 엽니다. (.NET 8 SDK 필요)
3. 시작 프로젝트를 `Othello.WinForms`로 지정하고 F5를 누릅니다.
4. 테스트는 **테스트 → 테스트 탐색기 → 모두 실행**으로 돌립니다. MSTest 패키지는 처음 빌드할 때 자동으로 받습니다.

이미 만들어 둔 WinForms 프로젝트를 쓰고 싶다면 이렇게 하세요.
- `Othello.Core`와 `Othello.Tests`만 **기존 프로젝트 추가**로 솔루션에 넣습니다.
- 기존 프로젝트에서 Core를 참조로 추가합니다.
- `BoardControl.cs`와 `GameForm.cs`를 복사해 넣고 네임스페이스를 맞춥니다.

## 구조 (설계서 3.1의 계층과 대응)

```
Othello.Core/            ← UI에 의존하지 않는 클래스 라이브러리
  Model/   Stone, Cell(+CellState [Flags]), Position, Board, GameState
  Rules/   StatusEffects(칸 상태 판정), ScoreRule(점수 계산)
  Cards/   ISkillCard, CardDeck, Hand, SampleCards(패 5종)
  Control/ GameController(턴 진행), GameAction, PlayerSlot
  Cpu/     WeightTables, CPUPlayer(⚠ 임시 버전)
Othello.WinForms/        ← 화면 (디자이너 없이 코드로 배치)
  BoardControl  GDI+ 보드 그리기, 더블 버퍼링
  GameForm      손패 → 대상 → 착수 입력 흐름, CPU 차례 비동기 처리
Othello.Tests/           ← MSTest, 설계서 TC 번호를 메서드 이름에 붙임
```

## 진행 상황 (설계서 기능 ID 기준)

| 영역 | 된 것 | 남은 것 |
|---|---|---|
| F1 기본 오셀로 | F1-1~F1-5 전부 | F1-6 대전 설정 화면(MatchSetupForm) |
| F2 패 시스템 | 덱·손패·드로우(3개 이상)·손패 초과 처리·덱 순환 | 최종 12종 중 나머지 7종 |
| 구현한 패 | 전향(A), 고정(A), 봉인(A), 점수 2배(B), 침묵(C) | 방패 카드 클래스 (점수 계산 쪽은 이미 대응) |
| F3 점수제 | 뒤집기 점수, 패 점수, 종료 보너스, 승패 | 결과 화면(ResultForm) 분리 표시 |
| F4 CPU | 백그라운드 계산 + 0.7초 지연, 가중치표 | ⚠ 지금은 "가중치 최고 칸"만 고름 → EvalResult, 후보 3개 × A형 패, 깊이 2 알파-베타 (3.4.4) |
| F5 전적 DB | — | RecordRepository, SQLite (4주차) |
| F6 편의 기능 | 착수 가능 점, 최근 착수 표시, 손패 툴팁 | 패 도감, 뒤집힐 돌 미리보기 |

## 코드에서 봐 둘 설계 포인트

- **Cell은 값 타입만 가진 구조체**예요. 그래서 `(Cell[,])cells.Clone()` 한 줄로 깊은 복사가 됩니다 (`Board.Clone`). CPU는 이 복사본 위에서 수를 둬 봅니다.
- **상태 판정은 StatusEffects 한곳에서만 합니다.** 봉인 칸에 둘 수 있는지, 고정 돌이 뒤집히는지를 Board와 패 클래스가 각자 검사하지 않아요.
- **`GameController.TryExecuteTurn`은 복사본에서 먼저 실행해 봅니다.** 성공할 때만 원본에 적용하므로, 잘못된 입력이 들어와도 상태가 절반만 바뀌는 일이 없어요.
- **패 하나 = 클래스 하나**예요 (`SkillCardBase` 상속). 새 패는 `GetTargets`와 `Apply`만 구현하면 됩니다.
- **사람과 CPU는 같은 `GameAction`으로 턴을 넘깁니다.** 그래서 CPU를 바꿔도 컨트롤러는 그대로 둘 수 있어요.

## 다음 단계로 Claude Code에 시킬 만한 것

1. "이 솔루션 빌드해 보고 오류 있으면 고쳐 줘. 테스트도 전부 통과시켜 줘"
2. "docs/계획서_최종안.md의 3.4.2~3.4.4대로 EvalResult와 CPUPlayer를 다시 구현해 줘. 먼저 계획 모드로 계획부터"
3. "최종 12종이 확정되면 나머지 패를 SampleCards와 같은 방식으로 추가하고, 패마다 TC-07 테스트를 붙여 줘"
4. "MainForm, MatchSetupForm 만들어 줘. ID 규칙은 설계서 2.4 입력 명세대로"
