using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string SceneName;
    public void SceneChange()
    {
        SceneManager.LoadScene(SceneName);
    }
}
