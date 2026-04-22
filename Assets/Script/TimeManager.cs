using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    public float currentTime = 0f;
    public float timeSpeed = 1f;
    public bool isNight { get; private set; } = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;
        if (currentTime >= 24f) currentTime = 0f;

        bool nightCheck = (currentTime > 20f || currentTime < 6f);

        if (isNight != nightCheck)
        {
            isNight = nightCheck;
            GlobalEventManager.CallTimeChanged(currentTime, isNight);
        }
    }
}
