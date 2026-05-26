using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wavecircle : MonoBehaviour
{
    [Range(0, 100)]
    public float no1;

    public Transform wave;
    public Transform s, e;

    public Text theText;

    public void SetPercent(float f)
    {
        no1 = f;
        UpdatePercent(f);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdatePercent(float f)
    {
        wave.position = s.position + (e.position - s.position) * f / 100;

        theText.text = Mathf.RoundToInt(f) + "%";
    }
}
