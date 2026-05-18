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
        if (InfectionEngine.Instance == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<RegionController>(out var region))
                InfectionEngine.Instance.AttemptHack(region.data);
        }
    }
}
