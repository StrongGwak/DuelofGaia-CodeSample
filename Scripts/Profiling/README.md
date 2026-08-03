# PerfProbe — 구간 프레임 시간 A/B 계측

셰이더 최초 컴파일로 인한 프레임 드랍을 개선하면서, 개선 전후를 같은 기준으로 비교하기 위해 만든 임시 계측 도구다.
`PerfProbe.cs`(도구)와 `perf.csv`(실측 결과)로 구성된다.

## 왜 만들었나

프로파일러 캡처는 사람이 버튼을 누르는 타이밍에 따라 측정 구간이 매번 달라진다.
개선 전후를 비교하려면 **구간의 시작과 끝이 코드 이벤트로 고정**돼야 한다.
그래서 씬 로드 콜백과 UI 트윈 완료 콜백을 앵커로 삼아 구간을 자동으로 자르도록 했다.

## 측정 구간과 앵커

| segment | 시작 앵커 | 종료 앵커 | 구간 성격 |
|---|---|---|---|
| `stage_enter` | `Stage_Select` 씬 `sceneLoaded` 콜백 | **30프레임 고정** (`StageEnterFrames`) | 프레임 수 고정 |
| `deck_panel_open` | 덱 패널 열기 시 `PerfProbe.Begin` | 패널 트윈 `OnComplete`에서 `PerfProbe.End` | **구간 길이 고정** (트윈 duration 1.0초) |

`stage_enter`는 종료 앵커로 쓸 코드 이벤트가 없다. 씬 페이드가 탭 입력을 기다리므로 그 전에 잘라야 해서
짧은 고정 프레임 수로 끊었다. 따라서 이 구간은 프레임 수가 같고 **구간 길이는 런마다 다르다.**

`deck_panel_open`은 반대다. 종료가 고정 길이 트윈이라 **구간 길이가 항상 같고 프레임 수만 달라진다.**
이 성질이 아래 "관측 창 고정" 검증의 근거가 된다.

## 실행 방법

```
DuelOfGaia.exe -perfLabel before
DuelOfGaia.exe -perfLabel after
```

`-perfLabel` 인수가 CSV의 `run` 열에 그대로 들어간다. 생략하면 `unlabeled`로 기록된다.

출력 경로:

```
%USERPROFILE%\AppData\LocalLow\DoG Studio\Duel Of Gaia\perf_samples.csv
```

파일이 없으면 헤더를 쓰고, 있으면 이어붙인다. 런을 반복해도 누적된다.

## 빌드 영향

릴리즈 빌드에는 들어가지 않는다.

- 설치부(`Install`)가 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싸여 있어 **인스턴스 자체가 생성되지 않는다**
- 공개 API 3종(`Begin` / `BeginFrames` / `End`)은 `[Conditional("UNITY_EDITOR")]` + `[Conditional("DEVELOPMENT_BUILD")]`이므로
  **호출부째 컴파일에서 제거된다.** 게임 코드에 남은 계측 호출이 릴리즈 성능에 영향을 주지 않는다
- 프로브 오브젝트는 `HideFlags.HideAndDontSave` + `DontDestroyOnLoad`

## perf.csv 스키마

`perf.csv`는 프로브 원본 출력(12열)에서 **3열을 제외한 9열**이다. 타임스탬프는 분 단위로 절삭했다.

| 열 | 의미 |
|---|---|
| `run` | `-perfLabel` 값 (`before` / `after`) |
| `segment` | 측정 구간 이름 |
| `timestamp` | 측정 시각 |
| `frames` | 구간 내 샘플(프레임) 수 |
| `maxMs` | 최악 프레임 시간 — **대표 지표** |
| `medianMs` | 중앙값 프레임 시간 |
| `meanMs` | 평균 프레임 시간 |
| `over16` | 16.7ms 초과 프레임 수 |
| `over33` | 33.3ms 초과 프레임 수 — **대표 지표** |

**제외한 열과 이유**

| 열 | 제외 이유 |
|---|---|
| `p99Ms` | `stage_enter`는 샘플이 30개뿐이라 99번째 백분위가 `maxMs`와 같은 값이 된다. 별도 정보가 없는 아티팩트라 인용하면 오해를 부른다 |
| `vSync` | 전 행 상수(0). 아래 측정 환경 표로 이관 |
| `targetFps` | 전 행 상수. 위와 같음 |

샘플 값은 `Time.unscaledDeltaTime * 1000`이다. 타임스케일 영향을 받지 않지만
**CPU/GPU를 분리하지 않은 벽시계 프레임 시간**이라는 점은 해석 시 감안해야 한다.

## 측정 환경

| 항목 | 값 |
|---|---|
| 빌드 | Development Build |
| 해상도 | 1920×1080 전체화면 |
| 품질 설정 | 기본 |
| vSync | off |
| CPU | Ryzen 5 3500X |
| GPU | GTX 1660 SUPER |
| RAM | 16GB |
| OS | Windows 10 |

각 조건 5런. 아래 수치는 모두 **5런의 중앙값**이다.

## 결과

| 구간 | 지표 | before | after | 변화 |
|---|---|---|---|---|
| `deck_panel_open` | `maxMs` | 370.25 | 13.96 | **−96%** |
| `deck_panel_open` | `over33` | 1 | 0 | 해소 |
| `stage_enter` | `maxMs` | 772.69 | 171.77 | **−78%** |
| `stage_enter` | `over33` | 2 | 2 | 변화 없음 |

원인은 `DrawScreenSpaceUI > Material.SetPassFast > Shader.CreateGPUProgram`에서 144ms —
TMP_SDF 변형 2종(`UNDERLAY_ON`, `UNDERLAY_ON UNITY_UI_CLIP_RECT`)의 최초 컴파일이었다.
Shader Variant Collection 에셋은 프로젝트에 있었으나 `GraphicsSettings`에도 코드에도 연결돼 있지 않았다.
로딩 화면에서 프레임을 나눠 워밍업하도록 바꿔 해결했다.

### 관측 창이 고정이라는 증거

"측정 구간이 정말 같았나"에 대한 답은 `deck_panel_open` 행의 `frames × meanMs`다.

| run | frames | meanMs | 곱 |
|---|---|---|---|
| before | 104 | 9.65 | 1003.6 |
| before | 79 | 12.71 | 1004.1 |
| before | 87 | 11.53 | 1003.1 |
| after | 136 | 7.39 | 1005.0 |
| after | 135 | 7.41 | 1000.4 |

프레임 수는 79~136으로 크게 다르지만 **구간 길이는 전 행 1000~1006ms**다.
종료 앵커가 고정 길이 트윈이라 유저 조작과 무관하게 같은 창을 관측한 것이 데이터로 확인된다.

`stage_enter`는 프레임 수 고정 앵커라 이 검증이 적용되지 않는다. 해당 구간은 `maxMs`만 비교 대상으로 본다.

## 해석 한계 — 반드시 함께 읽을 것

- **누적 개선이다.** before/after 두 커밋 사이에 9개 커밋이 섞여 있다.
  셰이더 워밍업 단독 효과로 단정할 수 없다
- **`stage_enter`의 172ms는 남아 있다.** 씬 활성화 비용으로 추정하며 원인을 특정하지 않았다.
  페이드 뒤라 체감되지 않을 뿐이므로 "완전 제거"라고 말하면 과장이다
- **표본이 작다.** 조건당 5런의 중앙값이며 신뢰구간을 계산하지 않았다
- **단일 머신 단일 환경**이다. 다른 GPU에서 셰이더 컴파일 비용은 달라진다
- `p99Ms`는 위 사유로 제외했다. **인용하지 말 것**

## 재현 절차

1. Development Build로 빌드
2. `-perfLabel before`로 실행 → 타이틀에서 스테이지 선택 진입 → 덱 패널 열기 (5회 반복)
3. 개선 커밋 적용 후 동일하게 `-perfLabel after`로 실행
4. `perf_samples.csv`에서 `run`별로 분리해 `maxMs`·`over33` 중앙값 비교

동일 구간 비교가 성립하려면 `deck_panel_open`의 `frames × meanMs`가 양쪽 모두 같은 범위인지 먼저 확인할 것.