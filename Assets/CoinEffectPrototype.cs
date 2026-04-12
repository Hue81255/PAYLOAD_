using UnityEngine;
using TMPro;
using DG.Tweening;

public class CoinEffectPrototype : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    private int coin = 0;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = coinText.transform.localScale;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            coin++;
            coinText.text = "Coin : " + coin;

            coinText.transform.DOKill();
            coinText.transform.localScale = originalScale;

            coinText.transform.DOPunchScale(
                new Vector3(0.3f, 0.3f, 0.3f),
                0.25f,
                8,
                0.8f
            );
        }
    }
}