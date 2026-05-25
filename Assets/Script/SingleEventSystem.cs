using UnityEngine;
using UnityEngine.EventSystems;

public class SingleEventSystem : MonoBehaviour
{
    void Awake()
    {
        EventSystem[] all = FindObjectsOfType<EventSystem>(true);
        if (all.Length <= 1) return;

        // 자신(this)을 살리고 나머지는 제거
        foreach (EventSystem es in all)
        {
            if (es.gameObject != gameObject)
                Destroy(es.gameObject);
        }
    }
}
