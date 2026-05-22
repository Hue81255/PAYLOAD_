using UnityEngine;

public class HackEffect : MonoBehaviour
{
    private void MoveToCameraCenter()
    {
        Vector3 pos = Camera.main.transform.position;
        pos.z = 0;
        transform.position = pos;
    }

    public ParticleSystem particle;

    public void PlaySuccess()
    {
        MoveToCameraCenter();
        var main = particle.main;
        main.startColor = Color.green;

        particle.Stop();
        particle.Play();
    }

    public void PlayFail()
    {
        MoveToCameraCenter();
        var main = particle.main;
        main.startColor = Color.red;

        particle.Stop();
        particle.Play();
    }
}