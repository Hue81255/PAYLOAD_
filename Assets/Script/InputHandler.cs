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
