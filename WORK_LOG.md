# PAYLOAD_ 작업 로그

Unity 2022.3.62f3 / C#

---

## 프로젝트 개요

플레이어가 바이러스/악성코드를 조작하여 9개 도시 구역을 감염시키고, AI 화이트해커를 피해 살아남는 전략 게임.

- **씬 구조**: StartScreen → select2 → select → New main → Process → New main
- **저장 슬롯**: 4슬롯 (slot 0~3), `Application.persistentDataPath/payload_save_{slot}.json`

---

## 작업 내역

### 1. 싱글톤 OnDestroy 정리
**대상 파일**: 모든 싱글톤 매니저

아래 파일 전부에 `OnDestroy()` 추가하여 씬 전환 시 인스턴스 누수 방지:

```csharp
void OnDestroy()
{
    if (Instance == this) Instance = null;
}
```

적용 파일:
- `CureManager.cs`
- `SpreadManager.cs`
- `GameManager.cs`
- `WhiteHackerManager.cs`
- `UIManager.cs`
- `InfectionEngine.cs`
- `RegionAdjacencyManager.cs`
- `PlayerStats.cs`
- `SaveManager.cs`
- `RegionDataLoader.cs`
- `EvolutionManager.cs`
- `MalwareSelectionManager.cs`
- `TraitTree/TraitTreeManager.cs`

---

### 2. SaveData 구조 변경 — 지역별 TraitTree 저장

**파일**: `Assets/Script/SaveData.cs`

기존 전역 언락 노드 리스트에서 **지역별 언락 노드** 구조로 변경:

```csharp
// 기존
public List<string> unlockedTraitNodes;

// 변경 후
public List<RegionTraitSaveData> regionTraitNodes = new List<RegionTraitSaveData>();

[Serializable]
public class RegionTraitSaveData
{
    public string       regionId;
    public List<string> unlockedNodes = new List<string>();
}
```

---

### 3. TraitTreeManager 전면 재설계 — 지역별 노드 관리

**파일**: `Assets/Script/TraitTree/TraitTreeManager.cs`

#### 핵심 변경사항

| 항목 | 이전 | 변경 후 |
|------|------|---------|
| 저장 구조 | `HashSet<TraitNode>` (ScriptableObject 참조) | `HashSet<string>` (노드 이름) |
| IsUnlocked 비교 | 인스턴스 참조 비교 (`Contains(n)`) | 이름 문자열 비교 (`set.Contains(n.name)`) |
| 보너스 계산 | 매번 전체 노드 순회 | 사전 계산 캐시 `_bonusByRegion` 사용 |
| 전역 스탯 수정 | TryUnlock 시 PlayerStats 직접 수정 | 수정 없음 — 지역 보너스로만 반영 |

#### 주요 API

```csharp
// 현재 편집 지역 설정 (ProcessSceneManager에서 호출)
public void SetCurrentRegion(string regionId)

// 공격 시 지역별 보너스 조회 (InfectionEngine, SpreadManager에서 사용)
public int GetRegionStatBonus(string regionId, TraitCategory cat)

// 저장
public List<RegionTraitSaveData> GetAllSaveData()

// 복원 (이름 기반 — 씬 재로드 후 인스턴스 불일치 문제 없음)
public void RestoreFromSaveData(List<RegionTraitSaveData> saveData)
```

#### 버그 수정: 노드가 복원 안 되던 문제

**원인**: `RestoreFromSaveData`에서 `FindObjectsOfType<TraitNodeUI>()`가
비활성화된 패널의 오브젝트를 찾지 못해 nodeMap이 비어있었음.
또한 `HashSet<TraitNode>`의 `Contains()`가 씬 재로드 후 인스턴스가 달라져 항상 false 반환.

**해결**: 저장/비교를 ScriptableObject 참조 대신 **이름(string)** 기반으로 전환.
이름은 씬 재로드와 무관하게 항상 동일하므로 복원이 정확하게 동작.

---

### 4. TraitTreeView 수정

**파일**: `Assets/Script/TraitTree/TraitTreeView.cs`

`RefreshSliderActuals()`에서 기본 스탯을 가져올 때 `PlayerStats` 직접 접근 대신
`TraitTreeManager.GetCurrentStat()` 사용 → 현재 지역 보너스 반영:

```csharp
int inf = TraitTreeManager.Instance != null
    ? TraitTreeManager.Instance.GetCurrentStat(TraitCategory.Inf)
    : PlayerStats.Instance.inf;
```

---

### 5. InfectionEngine 수정 — 지역별 트레이트 보너스 적용

**파일**: `Assets/Script/InfectionEngine.cs`

해킹 성공 판정 시 현재 지역에 해제된 노드 보너스 반영:

```csharp
int bInf     = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Inf)     ?? 0;
int bComp    = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Comp)    ?? 0;
int bStealth = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Stealth) ?? 0;

bool success = (playerInf + bInf) >= ... && (playerComp + bComp) >= ... && (playerStealth + bStealth) >= ...
```

---

### 6. SpreadManager 수정

**파일**: `Assets/Script/SpreadManager.cs`

- 자동 전파 확률 계산에 지역별 트레이트 보너스 반영
- `GetDisplayChance(RegionData)` public 메서드 추가 (UIManager 침투율 표시용)

```csharp
int regionBonus = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Inf) ?? 0;
float chance = baseSpreadChance + (playerInf + regionBonus - effectiveDef) * 0.015f;
```

**전파 속도 설정값** (최종):
```
spreadInterval    = 10f    // 전파 시도 주기 (초)
baseSpreadChance  = 0.35f  // 기본 전파 확률
```

---

### 7. SaveManager 수정

**파일**: `Assets/Script/SaveManager.cs`

- `Save()`: TraitTreeManager가 없는 씬(메인)에서는 이전 세이브 파일의 `regionTraitNodes` 그대로 유지
- `Load()`: `RestoreFromSaveData()` 호출 (ProcessSceneManager를 통해 간접 실행됨)

```csharp
// 메인씬에서 저장 시 — TraitTreeManager 없으면 기존 데이터 보존
if (TraitTree.TraitTreeManager.Instance != null)
    data.regionTraitNodes = TraitTree.TraitTreeManager.Instance.GetAllSaveData();
else if (HasSave(slot))
{
    var prev = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
    data.regionTraitNodes = prev?.regionTraitNodes ?? new List<RegionTraitSaveData>();
}
```

---

### 8. ProcessSceneManager 수정

**파일**: `Assets/Script/ProcessSceneManager.cs`

- `Start()`: 세이브 로드 후 현재 편집 지역을 TraitTreeManager에 알림
- `BackToGame()`: 노드 데이터 저장 후 메인씬 복귀
- `BackToSelect()`: 세이브 삭제 후 select씬으로 (게임 중에는 호출되지 않음)

```csharp
void Start()
{
    if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        SaveManager.Instance.Load();
    TraitTree.TraitTreeManager.Instance?.SetCurrentRegion(GameFlowData.SelectedRegionId);
}
```

---

### 9. GameManager 수정

**파일**: `Assets/Script/GameManager.cs`

- `GoToProcess()`: 선택된 지역 ID를 `GameFlowData.SelectedRegionId`에 저장 후 Process씬 이동
- `InitGame()` else 분기: 세이브 로드 실패 시 select씬이 아닌 메인씬에서 새 게임으로 재시작

```csharp
public void GoToProcess()
{
    GameFlowData.SelectedRegionId = UIManager.Instance?.GetSelectedRegion()?.id ?? "";
    SaveManager.Instance?.Save();
    Time.timeScale = 0f;
    SceneManager.LoadScene("Process");
}
```

---

### 10. GameFlowData 수정

**파일**: `Assets/Script/GameFlowData.cs`

Process씬으로 넘길 지역 ID 필드 추가:

```csharp
public static string SelectedRegionId = "";
```

---

### 11. UIManager 전면 수정

**파일**: `Assets/Script/UIManager.cs`

#### Text → TMP_Text 전환

모든 `UnityEngine.UI.Text` 필드를 `TMPro.TMP_Text`로 교체:

| 필드 | 용도 |
|------|------|
| `coinText` | 코인 표시 |
| `cureText` | 발각도 % |
| `infText` / `compText` / `stealthText` | 스탯 표시 |
| `whiteHackerTargetText` | 화이트해커 상태 |
| `infectedCountText` / `spreadTimerText` | 감염 현황 |
| `regionNameText` / `regionTypeText` / `regionInfStatusText` / `regionPenRateText` / `regionTimeText` | 지역 정보 패널 |
| `notifyText` | 알림 텍스트 |

#### 지역 정보 패널 추가

```csharp
public void ShowRegionInfo(RegionData region)  // 지역 클릭 시 패널 표시
public void HideRegionInfo()                   // 패널 숨김
public RegionData GetSelectedRegion()          // 현재 선택 지역 반환
```

#### 환경설정 패널 추가

```csharp
public void ToggleSettings()  // 홈버튼 OnClick 연결
public void CloseSettings()
```

#### 싱글톤 버그 수정 — UIManager가 사라지는 문제

**원인**: `else Destroy(gameObject)` — 나중에 초기화된 UIManager(텍스트 연결된 것)가 파괴됨

**해결**: 항상 현재 씬의 UIManager가 살아남도록 변경

```csharp
// 기존 (버그)
if (Instance == null) Instance = this;
else Destroy(gameObject);

// 수정 후
if (Instance != null && Instance != this)
    Destroy(Instance.gameObject); // 이전 것 제거
Instance = this;                  // 현재 것을 항상 사용
```

---

### 12. InputHandler 수정

**파일**: `Assets/Script/InputHandler.cs`

지역 클릭 시 UIManager 지역 정보 패널 연동:

```csharp
// 지역 클릭
UIManager.Instance?.ShowRegionInfo(region.data);

// 빈 공간 클릭
UIManager.Instance?.HideRegionInfo();
```

---

### 13. GameSpeedController 신규 추가

**파일**: `Assets/Script/GameSpeedController.cs`

일시정지 / 1배속 / 2배속 버튼 제어:

```csharp
public void Pause()   { Time.timeScale = 0f; }
public void Play()    { Time.timeScale = 1f; }
public void Speed2x() { Time.timeScale = 2f; }
```

유니티 Button OnClick 연결 방법:
- Pause 버튼 → `GameSpeedController.Pause()`
- Play 버튼 → `GameSpeedController.Play()`
- 2x 버튼 → `GameSpeedController.Speed2x()`

---

### 14. MalwareCardUI 수정

**파일**: `Assets/Script/MalwareCardUI.cs`

select씬에서 선택한 카드 색상 변경 (선택됨 = 회색, 미선택 = 흰색):

```csharp
// SetSelected() 내부
background.color = selected
    ? new Color(0.55f, 0.55f, 0.55f, 1f)  // 회색
    : Color.white;
```

---

### 15. VirusSelectScene / MalwareSelectionManager 수정

**파일**: `Assets/Script/VirusSelectScene.cs`, `MalwareSelectionManager.cs`

디버그 로그 추가 — 선택된 바이러스 종류 및 게임 적용 여부 확인용:

```csharp
Debug.Log($"[VirusSelectScene] 카드 선택: {card.malwareType}");
Debug.Log($"[MalwareSelectionManager] ApplyNewGame: type={type}");
```

---

### 16. Worm 패시브 조정

**파일**: `Assets/Script/MalwareSelectionManager.cs`

Worm 악성코드 자동 전염도 증가 패시브 속도 조정:

```csharp
// 조정 전: 10초마다 infectedRegions만큼 증가
// 조정 후: 30초마다 1 증가
```

---

### 17. Process.unity 인스펙터 값 수정

**파일**: `Assets/Scenes/Process.unity`

| 항목 | 수정 전 | 수정 후 |
|------|---------|---------|
| `debugLogs` | 1 | 0 |
| `coins` | 99999 | 100 |
| BackToGame 버튼 OnClick | 미연결 | `ProcessSceneManager.BackToGame()` 연결 |

---

### 18. News 시스템 신규 구축

**파일**: `Assets/Script/News.cs`, `Assets/Resources/News.json`, `Assets/Script/NewsTickerUI.cs`

#### News.cs — 뉴스 매니저 (싱글톤)

기존 프로젝트 루트에 있던 파일(`News.cs`, `News.json`)을 올바른 경로로 이동 후 전면 재작성.

주요 변경사항:
- `public static News Instance` 싱글톤 추가 (`OnDestroy` 포함)
- `Update()`가 자체적으로 모든 뉴스 로직 구동 (외부 호출 불필요)
- `BuildCurrentState()`: PlayerStats / GameManager / CureManager / MalwareSelectionManager에서 직접 상태 수집
- `GetMalwareName()`: `MalwareType` 열거형 → JSON의 한국어 이름 매핑

```csharp
static string GetMalwareName()
{
    switch (MalwareSelectionManager.Instance.selectedType)
    {
        case MalwareType.Ransomware:  return "랜섬웨어";
        case MalwareType.Spyware:     return "스파이웨어";
        case MalwareType.Worm:        return "웜";
        case MalwareType.Trojan:      return "스틸러";
        case MalwareType.Botnet:      return "크립토마이너";
    }
}
```

- `IsBossAccessible()`: InfectionEngine + RegionAdjacencyManager로 보스 지역 인접 감염 여부 판단
- `malwareIntroDone` 플래그로 인트로 뉴스 최초 1회만 발행

#### CureManager.cs — cureStarted 접근자 추가

`cureStarted`가 `private`이어서 News.cs에서 접근 불가 → 프로퍼티 추가:

```csharp
public bool IsCureStarted => cureStarted;
```

#### NewsTickerUI.cs — 뉴스 UI 표시 컴포넌트 (신규)

`News.OnNewsPublished` 이벤트를 구독해 TMP_Text 한 줄에 헤드라인 표시, CanvasGroup 페이드 인/아웃 처리:

```csharp
public TMP_Text newsText;        // 단일 텍스트 필드
public float displayDuration = 8f;
public float fadeDuration    = 0.5f;
```

- `OnEnable` / `Start`에서 이벤트 구독 (초기화 순서 무관하게 안전하게 연결)
- `OnDisable` / `OnDestroy`에서 구독 해제

#### 유니티 연결 방법

1. 메인씬에 빈 GameObject `NewsManager` 생성 → `News` 컴포넌트 추가
2. 뉴스 텍스트 UI 오브젝트에 `NewsTickerUI` 컴포넌트 추가
3. `NewsTickerUI.newsText` 슬롯에 TMP_Text 드래그 연결

---

### 19. Wavecircle 중복 클래스 제거

**파일**: `Assets/Data Visualization Resources/Scripts/Wavecircle.cs`, `Wavecircle2.cs`

`Wavecircle2.cs`가 `Wavecircle`과 동일한 클래스명을 사용해 `CS0101` 컴파일 오류 발생.

- `Wavecircle2.cs`의 `SetPercent()` 메서드를 `Wavecircle.cs`에 병합
- `Wavecircle2.cs` 삭제

```csharp
public void SetPercent(float f)
{
    no1 = f;
    UpdatePercent(f);
}
```

---

### 20. PlayModePreviewFix 에디터 스크립트 추가

**파일**: `Assets/Editor/PlayModePreviewFix.cs`

Unity 2022.3 에디터 버그 — Play 모드 진입 시 Preview 창이 이미 파괴된 `SerializedObject`에 접근하며 발생하는 `NullReferenceException` 방지:

```
NullReferenceException: SerializedObject of SerializedProperty has been Disposed.
UnityEditor.GameObjectInspector.OnDisable()
UnityEditor.PreviewWindow:OnDisable()
```

```csharp
[InitializeOnLoad]
public static class PlayModePreviewFix
{
    static PlayModePreviewFix()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Selection.activeObject = null;
    }
}
```

Play 진입 직전에 인스펙터 선택을 해제해 PreviewWindow가 참조를 유지하지 못하도록 차단.

---

### 21. TraitTreeView 불필요 경고 제거

**파일**: `Assets/Script/TraitTree/TraitTreeView.cs`

`debugLogs = true` 상태에서 정상 동작 중에도 계속 출력되던 두 경고 제거:

| 제거된 경고 | 이유 |
|-------------|------|
| `PlayerStats.Instance == null → 슬라이더 갱신 불가` | 초기화 순서상 일시적으로 발생하는 정상 상황 |
| `CanUnlockNow 실패 → 언락 안 함` | 코인 부족·선행 조건 미충족은 게임 플레이 중 당연히 발생 |

UI 경고 메시지(`UIManager.ShowWarning`)는 그대로 유지.

---

## 유니티 에디터 연결 가이드

### New main씬 UIManager 연결 항목

UIManager GameObject 인스펙터에서 다음 TMP_Text 컴포넌트 연결:

| 필드 | 연결할 오브젝트 |
|------|----------------|
| Coin Text | 코인 표시 TMP |
| Cure Text | 발각도 텍스트 TMP |
| Cure Slider | 발각도 슬라이더 |
| Cure Bar Fill | 슬라이더 Fill Image |
| Inf / Comp / Stealth Text | 각 스탯 TMP |
| White Hacker Slider | 화이트해커 슬라이더 |
| White Hacker Target Text | 화이트해커 상태 TMP |
| Infected Count Text | 감염 구역 TMP |
| Spread Timer Text | 다음 전파 타이머 TMP |
| Region Info Panel | 지역 정보 패널 GameObject |
| Region Name/Type/InfStatus/PenRate/Time Text | 지역 패널 내 각 TMP |
| Settings Panel | 환경설정 패널 GameObject |
| Notify Text | 알림 TMP |

### GameSpeedController 버튼 연결

씬에 GameSpeedController 컴포넌트가 있는 GameObject를 만들고:
- 일시정지 버튼 → OnClick → `GameSpeedController.Pause()`
- 재생 버튼 → OnClick → `GameSpeedController.Play()`
- 2배속 버튼 → OnClick → `GameSpeedController.Speed2x()`

---

## 알려진 주의사항

- `ProcessSceneManager.BackToGame()`은 이전 세이브를 보존하며 메인씬으로 복귀
- `ProcessSceneManager.BackToSelect()`는 세이브 삭제 후 select씬으로 이동 (게임 중 호출 금지)
- Process씬에는 GameManager, UIManager, SpreadManager가 없으므로 관련 null 체크 필수
- TraitTree 노드 보너스는 **메인씬에서 해당 지역 공격 시**에만 적용 (Process씬 UI 표시 전용이 아님)
- UIManager는 씬마다 독립적 — DontDestroyOnLoad 사용 안 함
