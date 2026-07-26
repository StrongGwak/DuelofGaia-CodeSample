# Duel of Gaia — 코드 샘플 (발췌)

Steam 출시작 **[Duel of Gaia](https://store.steampowered.com/app/4779190/Duel_of_Gaia/)** 에서
제가 설계·구현한 시스템의 소스만 발췌한 저장소입니다.

> **이 저장소는 빌드되지 않습니다.**
> 상용 서비스 중인 팀 프로젝트라 전체 소스는 비공개이며, 여기에는 제가 소유한 코드만
> 원본 폴더 구조 그대로 담았습니다. 에셋·씬·프리팹·밸런스 데이터·서버 설정은 제외했고,
> 팀원이 구축한 프레임워크 베이스 코드도 포함하지 않았습니다.
> 코드 스타일과 설계 의도를 보시는 용도입니다.

- **개발 기간**: 2025.02 ~ (17개월+)
- **엔진 / 언어**: Unity 6 (6000.0.25f1) / C#
- **팀 구성**: 프로그래머 2명 + UI 디자이너 1명

---

## 구성

| 폴더 | 내용 | 소유 |
|---|---|---|
| `Scripts/Data/Card`, `Data/Deck`, `Manager/CardManager·DeckManager`, `Character/Component/CardComponent`, `UI/Card`, `UI/Deck` | **카드 / 덱 시스템** — 4계층(Data → Component → Controller → UI) 구조, 융합 판정, 손패 관리 | 단독 설계·구현 |
| `Scripts/Skill/Action`, `Scripts/Effect/EffectAction`, `Scripts/TargetSearcher/IndicatorViewAction`, `UI/Display` | **스킬 프레임워크 확장** — 팀이 구축한 프레임워크의 확장 규약(SkillAction / EffectAction)에 맞춰 작성한 액션들과 타겟 인디케이터 | 확장 구현 (베이스는 팀원 구축, 미포함) |
| `Scripts/Manager/InputManager`, `UI/Menu/Option/Control`, `UI/Button/KeyBindButton` | **멀티플랫폼 입력** — 5개 입력 소스를 누적 버퍼로 통합한 카메라 파이프라인, 키 리바인딩 | 주 구현 |
| `Scripts/Data/Tutorial`, `Manager/TutorialManager·StageTutorialManager`, `UI/Ingame/Tutorial`, `UI/Stage/TutorialPromptUI` | **데이터 주도 튜토리얼** — ScriptableObject 스텝 리스트 + 이벤트 구독 완료 판정, 2-러너 구조 | 단독 설계·구현 |
| `Scripts/Manager/SaveManager·AudioManager`, `UI/Menu/Option`, `UI/Text/LocalizedText`, `UI/LocalizableUI` | **옵션 / 저장 / 로컬라이제이션** — 런타임 언어 전환, 오디오 믹서, 설정·진행 저장 | 단독 설계·구현 |
| `Scripts/UI/Stage`, `Manager/StageManager`, `Stage/StageSelector` | **스테이지 선택** — 덱 선택 패널, 해금 판정, 툴팁 | 단독 설계·구현 |
| `Scripts/Manager/SteamManager` | **Steam 연동** — Steamworks 초기화 | 단독 구현 |

## 눈여겨볼 만한 파일

- `Manager/CardManager.cs` — 융합 레시피를 "정렬된 카드명" 키의 딕셔너리로 구축해,
  드로우 시점 융합 판정을 조합 순회(nCk) 없이 조회 한 번으로 처리
- `Character/Component/CardComponent.cs` — 융합 재료 패시브 억제(참조 카운팅),
  카드 소모를 스킬 발동 확정 시점까지 지연하는 결합과 3경로 대칭 정리(`CleanupPending`)
- `Skill/Action/ChainSkillAction.cs` — 스킬 데이터에 정의된 필터/소터를 런타임에 재활용해
  연쇄 대상 탐색, `OverlapSphereNonAlloc`으로 GC 할당 회피
- `Effect/EffectAction/DrawAction.cs` — 스킬 프레임워크와 카드 시스템의 통합 지점
- `Manager/InputManager.cs` — 입력 수집과 적용을 분리한 누적 버퍼 파이프라인
  (LateUpdate 한 곳에서만 클램프·적용)
- `UI/Menu/Option/Control/KeyRebindRow.cs` — New Input System 리바인딩의 함정 처리와
  키 충돌 해제

## 제외한 것

- 모든 에셋, 씬, 프리팹, ScriptableObject 데이터 인스턴스 (카드 208종 / 스킬 341종)
- 팀원이 구축한 스킬·이펙트·타겟팅 프레임워크 베이스, 캐릭터 베이스, BehaviorTree
- 백엔드 연동 코드 전체 (Firebase 인증 / Firestore) 및 서버 자격 증명

---

© 2025–2026 곽강한. 상용 서비스 중인 **Duel of Gaia**의 소스 일부입니다.
채용 검토 목적의 열람용으로만 공개하며, 복제·재배포·2차적 저작물 작성을 허용하지 않습니다.
(All rights reserved. Provided for portfolio review only.)