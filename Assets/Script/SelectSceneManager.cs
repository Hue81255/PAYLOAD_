using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// main 씬 안에 Canvas 팝업으로 배치되는 악성코드 선택 UI.
/// 지역 클릭 → GameManager.OnStartRegionClicked() → ShowPanel(region) 순서로 표시된다.
/// 확인 시 선택한 바이러스로 해당 지역을 감염시키고 자동 전파를 시작한다.
/// </summary>
public class SelectSceneManager : MonoBehaviour
{
    public static SelectSceneManager Instance;

    [Header("팝업 패널 루트 (SetActive로 표시/숨김)")]
    public GameObject panel;

    [Header("선택된 지역 정보 표시 (선택 사항)")]
    public TMP_Text regionNameText;   // 예: "선택 지역: 중구"
    public TMP_Text regionStatsText;  // 예: "방어: 전염도 20 / 복잡도 15 / 은신도 10"

    [System.Serializable]
    public class CardSlot
    {
        public Button        button;
        public MalwareCardUI cardUI;
    }

    [Header("카드 슬롯 (6개, Inspector에서 버튼 연결)")]
    public CardSlot[] slots = new CardSlot[6];

    [Header("선택 상세 표시")]
    public TMP_Text selectedNameText;
    public TMP_Text selectedDescText;
    public TMP_Text selectedPassiveText;
    public TMP_Text selectedAbilityText;
    public TMP_Text selectedStatsText;

    [Header("확인 버튼")]
    public Button confirmButton;

    MalwareDefinition[] definitions;
    int        selectedIndex;
    RegionData pendingRegion; // 감염시킬 대상 지역

    // ── 생명주기 ──────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        definitions = MalwareDatabase.GetAll();

        for (int i = 0; i < slots.Length && i < definitions.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            if (slot.cardUI != null)
            {
                slot.cardUI.SetData(definitions[i]);
                if (slot.cardUI.button != null)
                {
                    slot.cardUI.button.onClick.RemoveAllListeners();
                    int ci = i;
                    slot.cardUI.button.onClick.AddListener(() => SelectCard(ci));
                }
            }

            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
                int bi = i;
                slot.button.onClick.AddListener(() => SelectCard(bi));
            }
        }

        if (confirmButton)
            confirmButton.onClick.AddListener(ConfirmSelection);

        if (panel) panel.SetActive(false);
    }

    // ── 공개 API ──────────────────────────────────────────────────

    /// <summary>지역을 선택한 뒤 팝업을 표시한다.</summary>
    public void ShowPanel(RegionData region = null)
    {
        pendingRegion = region;

        // 지역 정보 텍스트 갱신
        if (region != null)
        {
            if (regionNameText)
                regionNameText.text = $"선택 지역: {region.name}";

            if (regionStatsText)
                regionStatsText.text =
                    $"방어  전염도 {region.minStats.inf} / " +
                    $"복잡도 {region.minStats.comp} / " +
                    $"은신도 {region.minStats.stealth}";
        }
        else
        {
            if (regionNameText)  regionNameText.text  = "";
            if (regionStatsText) regionStatsText.text = "";
        }

        if (panel) panel.SetActive(true);
        SelectCard(0);
    }

    /// <summary>버튼 OnClick 전용 — 지역 정보 없이 팝업 표시</summary>
    public void ShowPanelFromButton() => ShowPanel(null);

    public void HidePanel()
    {
        if (panel) panel.SetActive(false);
    }

    public void SelectCard(int index)
    {
        if (definitions == null || index < 0 || index >= definitions.Length) return;
        selectedIndex = index;

        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].cardUI != null)
                slots[i].cardUI.SetSelected(i == index);

        var d = definitions[index];
        if (selectedNameText)    selectedNameText.text    = d.displayName;
        if (selectedDescText)    selectedDescText.text    = d.description;
        if (selectedPassiveText) selectedPassiveText.text = $"[패시브] {d.passiveDescription}";
        if (selectedAbilityText) selectedAbilityText.text = $"[특수] {d.abilityDescription}";

        string stats = "";
        if (d.infBonus     != 0) stats += $"전염도 {Fmt(d.infBonus)}\n";
        if (d.compBonus    != 0) stats += $"복잡도 {Fmt(d.compBonus)}\n";
        if (d.stealthBonus != 0) stats += $"은신도 {Fmt(d.stealthBonus)}\n";
        if (d.coinBonus    != 0) stats += $"코인 {Fmt(d.coinBonus)}\n";
        if (selectedStatsText) selectedStatsText.text = stats.TrimEnd();
    }

    // ── 확인 버튼 ─────────────────────────────────────────────────

    void ConfirmSelection()
    {
        HidePanel();
        GameFlowData.SelectedMalwareType = selectedIndex;
        // 악성코드 패시브 적용
        MalwareSelectionManager.Instance?.ApplyNewGame(selectedIndex);
        // 선택 지역 감염 + 자동 전파 시작
        GameManager.Instance?.ConfirmStartInfection(pendingRegion);
    }

    static string Fmt(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
