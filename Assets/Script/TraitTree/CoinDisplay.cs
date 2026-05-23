using TMPro;
using UnityEngine;

namespace TraitTree
{
    /// <summary>
    /// PlayerStats.coins 값을 TMP_Text에 실시간 표시한다.
    /// 같은 GameObject에 TMP_Text가 있으면 Reset에서 자동 연결.
    /// </summary>
    public class CoinDisplay : MonoBehaviour
    {
        [Tooltip("코인 숫자를 표시할 TMP 텍스트. 비워두면 같은 GameObject의 TMP_Text를 자동 사용.")]
        public TMP_Text targetText;

        [Tooltip("숫자 앞 텍스트 (예: \"COIN : \")")]
        public string prefix = "";

        [Tooltip("숫자 뒤 텍스트 (예: \" G\")")]
        public string suffix = "";

        [Tooltip("천 단위 콤마 (예: 1,234,567)")]
        public bool useThousandSeparator = false;

        int lastCoins = int.MinValue;

        void Reset()
        {
            // Inspector에서 컴포넌트를 처음 추가할 때 같은 GO의 TMP_Text 자동 연결
            if (targetText == null) targetText = GetComponent<TMP_Text>();
        }

        void Awake()
        {
            // 런타임에 Reset이 호출되지 않으므로 안전망
            if (targetText == null) targetText = GetComponent<TMP_Text>();
        }

        void Update()
        {
            if (targetText == null || PlayerStats.Instance == null) return;

            int coins = PlayerStats.Instance.coins;
            if (coins == lastCoins) return;   // 값 변화 없으면 텍스트 재할당 skip
            lastCoins = coins;

            string numberStr = useThousandSeparator
                ? coins.ToString("N0")
                : coins.ToString();
            targetText.text = $"{prefix}{numberStr}{suffix}";
        }
    }
}
