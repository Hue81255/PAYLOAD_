using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SelectSceneManager : MonoBehaviour
{
    [Header("카드 (9개, Inspector에서 순서대로 연결)")]
    public MalwareCardUI[] cards;

    [Header("선택 상세 표시")]
    public TMP_Text selectedNameText;
    public TMP_Text selectedDescText;
    public TMP_Text selectedPassiveText;
    public TMP_Text selectedAbilityText;
    public TMP_Text selectedStatsText;

    [Header("확인 버튼")]
    public Button confirmButton;

    MalwareDefinition[] definitions;
    int selectedIndex;

    void Start()
    {
        definitions = MalwareDatabase.GetAll();

        for (int i = 0; i < cards.Length && i < definitions.Length; i++)
        {
            cards[i].SetData(definitions[i]);

            // MalwareCardUI.SetData는 MalwareSelectionManager로 리스너를 설정하므로 이 씬용으로 재설정
            int idx = i;
            cards[i].button.onClick.RemoveAllListeners();
            cards[i].button.onClick.AddListener(() => SelectCard(idx));
        }

        if (confirmButton)
            confirmButton.onClick.AddListener(ConfirmSelection);

        SelectCard(0);
    }

    public void SelectCard(int index)
    {
        if (index < 0 || index >= definitions.Length) return;
        selectedIndex = index;

        for (int i = 0; i < cards.Length; i++)
            cards[i].SetSelected(i == index);

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

    void ConfirmSelection()
    {
        GameFlowData.SelectedMalwareType = selectedIndex;
        SceneManager.LoadScene("main");
    }

    static string Fmt(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
