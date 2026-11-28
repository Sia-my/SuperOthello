# 초능력 오셀로 (SuperOthello)

오셀로 규칙에 **초능력 패(카드)** 와 **점수제 승리 조건**을 더한 Windows 데스크톱 게임입니다.
사람 vs 사람, 사람 vs CPU(쉬움 / 보통 / 어려움)를 지원하고, 전적은 SQLite에 저장됩니다.

소프트웨어 프로젝트 3번째 과제 · 팀 리턴즈 (홍정호, 이재윤)

## 실행 방법

### 필요 환경
- Windows 10/11 64비트, 해상도 1280×720 이상
- [.NET 10 SDK](https://dotnet.microsoft.com/) (실행만 할 때는 .NET 10 Desktop Runtime)

### 빌드 · 실행 · 테스트
```powershell
dotnet build SuperOthello.slnx
dotnet run --project SuperOthello
dotnet test Othello.Tests/Othello.Tests.csproj
```
Visual Studio에서는 `SuperOthello.slnx`를 열고 `SuperOthello`를 시작 프로젝트로 실행합니다.

### 전적 DB 위치
`%LocalAppData%\SuperOthello\records.db` (처음 실행할 때 자동 생성).
DB를 열 수 없으면 안내만 띄우고 게임은 그대로 실행됩니다 (전적 기능만 비활성).

## 조작법

1. **메인 메뉴**에서 `대전`을 누릅니다.
2. **대전 설정**: 1P(흑)·2P(백) 슬롯마다 `사람`(ID 입력) 또는 `CPU`(난이도)를 고르고 `시작`.
   - ID는 1~12자 한글·영문·숫자만 가능 (`#` 입력 불가, 두 사람의 ID 중복 불가, CPU끼리 대전 불가)
3. **게임 화면**: 한 턴에 아래 순서로 진행합니다.
   1. (선택) 손패에서 **카드 클릭** → 다시 누르면 선택 취소
   2. 대상이 필요한 카드면 강조된 칸 중 **대상 칸 클릭**
   3. 점으로 표시된 **착수 칸 클릭** (카드를 쓴 결과 둘 곳이 없으면 카드 사용 후 자동 패스)
   - 둘 곳이 없으면 자동 패스, 칸에 마우스를 올리면 놓일 돌과 뒤집힐 돌(폭탄은 3×3 범위)을 미리 보여 줌
   - 상단 바에 지금 해야 할 일이 안내되고, 손패에 마우스를 올리면 효과 설명 툴팁
   - 손패가 3장인데 새 카드를 받으면 4장 중 버릴 1장을 고릅니다.
   - `기권` 또는 창 닫기 → 확인 후 종료 (전적에 기록되지 않음)
4. **결과 화면**: 뒤집기·카드 점수, 남은 돌 보너스, 최종 점수, 승자 표시 → `다시 하기`(같은 설정) / `메인 메뉴`
5. 메인 메뉴의 `전적 보기`(최근 10게임, ID 검색 시 승·패·무·승률), `카드 도감`(12종 설명)

## 게임 규칙 요약

- **점수**: 상대 돌 n개를 뒤집으면 착수자 +n, 상대 −n. 종료 시 판에 남은 자기 돌 수만큼 보너스. 점수가 높은 쪽 승리 (음수 허용, 같으면 무승부)
- **카드**: 시작 시 1장씩, 한 번에 3개 이상 뒤집으면 1장 획득. 손패 최대 3장, 한 턴에 1장 (착수 전).
  사용·버린 카드는 덱으로 돌아가 다시 섞입니다.

### 카드 12종

| 유형 | 카드 | 효과 | 점수 영향 |
|---|---|---|---|
| A 판 변경 | 전향 | 상대 돌 1개를 내 돌로 (모서리 불가) | 사용자 −2 |
| A 판 변경 | 폭탄 | 지정 칸 중심 3×3의 돌 제거 (고정 돌 제외) | 제거된 내 돌 1개당 −1 |
| A 판 변경 | 고정 | 내 돌 1개를 게임 끝까지 뒤집기·제거 불가 | 없음 |
| A 판 변경 | 봉인 | 빈칸 1개를 3턴 동안 착수 불가 | 없음 |
| B 점수 | 점수 2배 | 이번 착수 획득 점수 ×2 | 획득 ×2 |
| B 점수 | 방패 | 상대의 다음 턴 동안 받는 감점 절반(내림) | 감점 ÷ 2 |
| B 점수 | 럭키 세븐 | 이번 착수로 정확히 7개 뒤집으면 +7 | 사용자 +7 |
| B 점수 | 마이너스의 손 | 이번 착수 상대 감점 2배 | 사용자 +n, 상대 −2n |
| C 운영 | 약탈 | 상대 손패 1장을 무작위로 가져옴 | 없음 |
| C 운영 | 침묵 | 상대는 다음 턴에 카드 사용 불가 | 없음 |
| C 운영 | 보급 | 덱에서 2장 (한도 초과분은 버림) | 없음 |
| C 운영 | 역전의 발판 | 지고 있을 때만, 덱에서 3장 (한도 초과분은 버림) | 없음 |

## 기능 목록

| 구분 | 기능 |
|---|---|
| F1 기본 오셀로 | 판 초기화, 착수 가능 칸 계산, 뒤집기, 자동 패스, 종료 판정, 대전 설정 |
| F2 초능력 패 | 12종 공용 덱, 드로우·획득, 손패 3장 한도(사람: 버리기 / CPU: 획득 생략), 패 사용·효과, 손패·덱 공개, 덱 재구성 |
| F3 점수제 | 뒤집기 점수, 카드 점수, 종료 보너스, 승패 판정, 행동 로그 |
| F4 CPU | 난이도 3단계, 깊이 2 알파-베타 탐색, A형 카드 평가, B·C형 조건 규칙, 백그라운드 계산 + 연출 지연 |
| F5 기록 | ID 자동 등록, 결과 저장(SQLite, 매개변수화 쿼리), 최근 10게임, ID별 통계 |
| F6 부가 | 착수 가능 칸 점 표시, 최근 착수 하이라이트, 카드 도감·손패 툴팁, 뒤집힐 돌 미리보기 |

F1~F6의 필수·권장·선택 항목을 모두 구현했습니다.

## 폴더 구조

```
SuperOthello.slnx
Othello.Core/          UI에 의존하지 않는 게임 로직 (클래스 라이브러리)
  Model/               Data, Board, Cell, PlayerSlot, PlayerIdRule — 데이터와 ID 규칙
  Logic/               BoardLogic, GameLogic — 오셀로 기본 규칙
  Logic/Rules/         CellStateLogic, GameStateLogic, ScoreLogic, CardHandLogic (+ Card*.cs 초능력 확장)
  Cards/               ISkillCard, SkillCardBase, 카드 12종, CardDeck, Hand, CardPool
  Control/             GameController(턴 진행), GameAction
  Cpu/                 CPUPlayer, Evaluator, EvalResult, StabilityLogic, WeightTables, CpuCardLogic, CpuSimLogic
  Records/             RecordRepository(SQLite), GameRecord, PlayerStats
Othello.Tests/         MSTest 단위 테스트 (Core, Card, Cpu, Record, PlayerId)
SuperOthello/          WinForms 화면 (MainForm, MatchSetupForm, GameForm, ResultForm, RecordForm, CardBookForm)
                       + 직접 그리는 컨트롤 (BoardControl, PlayerPanel, CardTile, TurnBar, LogView) · 공용 테마 Theme
```
