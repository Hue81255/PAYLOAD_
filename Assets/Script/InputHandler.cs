using UnityEngine;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI 요소 위 클릭은 3D 레이캐스트 무시 — GoToProcess 버튼 클릭 시 선택 지워지는 문제 방지
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleInput(Input.mousePosition);
        }
    }

    void HandleInput(Vector3 screenPos)
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            UIManager.Instance?.HideRegionInfo();
            return;
        }
        if (!hit.collider.TryGetComponent<RegionController>(out var region))
        {
            UIManager.Instance?.HideRegionInfo();
            return;
        }

        if (region.data != null)
            UIManager.Instance?.ShowRegionInfo(region.data);
    }
}
