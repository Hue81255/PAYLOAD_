using UnityEngine;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleInput(Input.mousePosition);
    }

    void HandleInput(Vector3 screenPos)
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (!hit.collider.TryGetComponent<RegionController>(out var region)) return;

        // 지역 클릭 시 감염 상태 정보 표시 (자동 전파 방식이므로 클릭으로 감염 없음)
        if (region.data != null)
            UIManager.Instance?.ShowWarning(
                $"{region.data.name} — 감염: {(region.data.isInfected ? "O" : "X")}");
    }
}
