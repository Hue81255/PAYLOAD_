using UnityEngine;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        // PC 마우스 클릭 및 모바일 터치 공용
        if (Input.GetMouseButtonDown(0))
        {
            HandleInput(Input.mousePosition);
        }
    }

    void HandleInput(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 클릭한 물체에 'Region' 컴포넌트가 있다면 해킹 시도
            if (hit.collider.TryGetComponent<RegionController>(out var region))
            {
                // Engine에 해킹 시도 명령
                FindObjectOfType<InfectionEngine>().AttemptHack(region.data);
            }
        }
    }
}