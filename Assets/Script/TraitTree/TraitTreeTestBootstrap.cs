using UnityEngine;

namespace TraitTree
{
    /// <summary>
    /// Process 씬 단독 테스트용. 씬에 PlayerStats/EvolutionManager/InfectionEngine
    /// 싱글톤이 없으면 GameObject를 코드로 자동 생성한다.
    /// 정식 빌드에서는 이 컴포넌트를 제거하거나 GameObject를 비활성화하세요.
    ///
    /// [DefaultExecutionOrder(-1000)]: 다른 모든 MonoBehaviour의 Awake보다 먼저 실행돼
    /// TraitTreeView 등이 Instance를 참조하기 전에 매니저들을 만들어둔다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class TraitTreeTestBootstrap : MonoBehaviour
    {
        [Header("자동 생성된 PlayerStats 초기값")]
        [Tooltip("-1이면 PlayerStats.cs의 코드 기본값을 그대로 사용")]
        public int testCoins   = -1;
        public int testInf     = -1;
        public int testComp    = -1;
        public int testStealth = -1;

        [Header("부가 매니저")]
        [Tooltip("EvolutionManager가 없으면 자동 생성. 트리 언락 로직에 필수.")]
        public bool autoCreateEvolutionManager = true;
        [Tooltip("InfectionEngine이 없으면 자동 생성. 없어도 동작하지만 PlayerStats가 동기화하지 못함.")]
        public bool autoCreateInfectionEngine  = true;

        void Awake()
        {
            EnsureInstance<PlayerStats>("PlayerStats (Auto)", ps =>
            {
                if (testCoins   >= 0) ps.coins   = testCoins;
                if (testInf     >= 0) ps.inf     = testInf;
                if (testComp    >= 0) ps.comp    = testComp;
                if (testStealth >= 0) ps.stealth = testStealth;
            });

            if (autoCreateEvolutionManager)
                EnsureInstance<EvolutionManager>("EvolutionManager (Auto)", null);

            if (autoCreateInfectionEngine)
                EnsureInstance<InfectionEngine>("InfectionEngine (Auto)", null);
        }

        static void EnsureInstance<T>(string objectName, System.Action<T> onCreated) where T : Component
        {
            var existing = FindObjectOfType<T>();
            if (existing != null)
            {
                Debug.Log($"[TestBootstrap] {typeof(T).Name} 이미 존재 → 생성 건너뜀");
                return;
            }
            var go = new GameObject(objectName);
            var comp = go.AddComponent<T>(); // Awake가 즉시 실행되며 .Instance 세팅
            onCreated?.Invoke(comp);
            Debug.Log($"[TestBootstrap] 자동 생성: {objectName}");
        }
    }
}
