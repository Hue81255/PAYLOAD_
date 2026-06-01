using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotEntry
    {
        public Button       selectButton;
        public TMP_Text     infoText;
        public Button       deleteButton;
    }

    [SerializeField] SlotEntry[] slots = new SlotEntry[4];

    void Start() => RefreshAll();

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i);
    }

    void RefreshSlot(int i)
    {
        var entry = slots[i];
        bool has  = HasSave(i);

        entry.infoText.text = has ? BuildSaveInfo(i) : $"슬롯 {i + 1}\n── 새 게임 ──";
        entry.deleteButton.gameObject.SetActive(has);

        int captured = i;
        entry.selectButton.onClick.RemoveAllListeners();
        entry.selectButton.onClick.AddListener(() => OnSelect(captured));
        entry.deleteButton.onClick.RemoveAllListeners();
        entry.deleteButton.onClick.AddListener(() => OnDelete(captured));
    }

    string BuildSaveInfo(int slot)
    {
        var d = TryLoad(slot);
        if (d == null) return $"슬롯 {slot + 1}\n저장 데이터";
        return $"슬롯 {slot + 1}\n{d.savedAt}\n감염 {d.infectedRegions}/9구역";
    }

    void OnSelect(int slot)
    {
        GameFlowData.SelectedSlot = slot;
        bool isNew = !HasSave(slot);
        GameFlowData.IsNewGame = isNew;

        // 새 슬롯 선택 시 해당 슬롯의 저장 데이터가 없더라도 다른 슬롯 잔여물 차단
        SaveManager.CurrentSlot = slot;

        SceneManager.LoadScene("New main");
    }

    void OnDelete(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        RefreshSlot(slot);
    }

    bool HasSave(int slot) => File.Exists(SlotPath(slot));

    string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"payload_save_{slot}.json");

    SaveData TryLoad(int slot)
    {
        try   { return JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot))); }
        catch { return null; }
    }
}
