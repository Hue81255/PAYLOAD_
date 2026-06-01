using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// select 씬 전용. 바이러스 하나를 선택하고 확인 시 New main 씬으로 이동한다.
/// </summary>
public class VirusSelectScene : MonoBehaviour
{
    [System.Serializable]
    public class CardSlot
    {
        public Button        button;
        public MalwareCardUI cardUI;
    }

    [Header("카드 슬롯 (6개)")]
    public CardSlot[] slots = new CardSlot[6];

    [Header("선택 상세 패널")]
    public TMP_Text selectedNameText;
    public TMP_Text selectedDescText;
    public TMP_Text selectedPassiveText;
    public TMP_Text selectedAbilityText;
    public TMP_Text selectedStatsText;

    [Header("확인 버튼")]
    public Button confirmButton;

    MalwareDefinition[] _defs;
    int _selectedIndex = 0;

    void Start()
    {
        _defs = MalwareDatabase.GetAll();

        for (int i = 0; i < slots.Length && i < _defs.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            if (slot.cardUI != null)
            {
                slot.cardUI.SetData(_defs[i]);
                if (slot.cardUI.button != null)
                {
                    int ci = i;
                    slot.cardUI.button.onClick.RemoveAllListeners();
                    slot.cardUI.button.onClick.AddListener(() => SelectCard(ci));
                }
            }

            if (slot.button != null)
            {
                int bi = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => SelectCard(bi));
            }
        }

        if (confirmButton)
            confirmButton.onClick.AddListener(OnConfirm);

        SelectCard(0);
    }

    public void SelectCard(int index)
    {
        if (_defs == null || index < 0 || index >= _defs.Length) return;
        _selectedIndex = index;

        for (int i = 0; i < slots.Length; i++)
            if (slots[i]?.cardUI != null)
                slots[i].cardUI.SetSelected(i == index);

        var d = _defs[index];
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

        Debug.Log($"[VirusSelectScene] 선택됨: {d.displayName} (MalwareType={d.type}, index={index})");
    }

    void OnConfirm()
    {
        var d = _defs[_selectedIndex];
        Debug.Log($"[VirusSelectScene] 확인 버튼 — SelectedMalwareType={_selectedIndex} ({d.displayName}) → New main 씬으로 이동");

        GameFlowData.SelectedMalwareType = _selectedIndex;
        GameFlowData.IsNewGame           = true;

        // 이전 게임 저장 데이터 삭제 — 새 게임이 시작되므로 오염 방지
        SaveManager.Instance?.DeleteSave();

        SceneManager.LoadScene("New main");
    }

    static string Fmt(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
