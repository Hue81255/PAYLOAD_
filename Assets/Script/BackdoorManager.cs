using UnityEngine;

public class BackdoorManager : MonoBehaviour
{
    void OnEnable()  => GlobalEventManager.OnBackdoorActive += ActivateBackdoor;
    void OnDisable() => GlobalEventManager.OnBackdoorActive -= ActivateBackdoor;

    void ActivateBackdoor()
    {
        if (CureManager.Instance == null) return;

        // APT 패시브: 백도어 효과 2배 (40% 감소)
        float reduction = (MalwareSelectionManager.Instance != null &&
                           MalwareSelectionManager.Instance.HasAPTPassive) ? 40f : 20f;

        CureManager.Instance.cureProgress = Mathf.Max(0f, CureManager.Instance.cureProgress - reduction);
        UIManager.Instance?.ShowWarning($"[백도어] 발각도 -{reduction}% 감소!");
    }
}
