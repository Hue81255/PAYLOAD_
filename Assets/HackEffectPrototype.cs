using UnityEngine;

public class HackEffectPrototype : MonoBehaviour
{
    public ParticleSystem effect;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            effect.Play();
        }
    }
}