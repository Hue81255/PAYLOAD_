using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenManager : MonoBehaviour
{
    public void OnPlayButtonPressed()
    {
        SceneManager.LoadScene("select2");
    }
}
