using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
namespace Modules.SceneManagement.Internal
{
    public class SceneManagement : MonoBehaviour
    {
        

        private void Start()
        {
            StartCoroutine(LoadMainScene());
        }

        IEnumerator LoadMainScene()
        {
            yield return new WaitForSeconds(3);
            SceneManager.LoadScene(1);
        }
    }
}