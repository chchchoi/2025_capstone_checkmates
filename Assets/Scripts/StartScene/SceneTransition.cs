using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    void Start()
    {
        // 3초 후 씬 전환
        StartCoroutine(SceneTransitionCoroutine());
    }

    // IEnumerator로 수정
    private IEnumerator SceneTransitionCoroutine()
    {
        // 3초 대기
        yield return new WaitForSeconds(3f);

        // 씬 전환
        SceneManager.LoadScene("LoginScene");
    }
}
