using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace Mingle
{
    // 씬전환, 관리 용 모듈
    public class MingleSceneManager : MonoBehaviour
    {

        public GameObject LoadingImage = null;
        private float _loadingImageWidth = 782f;
        private float _loadingImageHeight = 1690f;

        private void Awake()
        {
            // Debug.Log(Screen.width / _loadingImageWidth);
            // Debug.Log(new Vector3(Screen.width / _loadingImageWidth, Screen.height / _loadingImageHeight, 1f));
            LoadingImage.transform.localScale = new Vector3(Screen.width / _loadingImageWidth, Screen.height / _loadingImageHeight, 1f);
        }

        private void Update()
        {
            Vector3 updateScale = new Vector3(Screen.width / _loadingImageWidth, Screen.height / _loadingImageHeight, 1f);
            if (LoadingImage.transform.localScale != updateScale) LoadingImage.transform.localScale = updateScale;
        }

        public void ChangeScene(JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();
            string sceneName = param["sceneName"].ToString();
            Debug.Log(sceneName);

            if (!string.IsNullOrEmpty(sceneName))
            {
                bool clearScene = param["clearScene"] == null ? false : param["clearScene"].ToObject<bool>();
                GameManager.Instance.SceneInitalization = param["params"]?.ToObject<JObject>();
                StartCoroutine(LoadScene(sceneName, clearScene, json));
            }
            else
            {
                RNMessenger.SendResult(json, false);
            }
        }
        private IEnumerator LoadScene(string sceneName, bool clearScene, JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();
            AsyncOperation operation = null;

            ShowLoadingImage();
            if (sceneName != Constants.EmptySceneName && HasScene(Constants.ChatRoomSceneName))
            {
                // operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                SetInActiveAllScene();
                if (!HasScene(sceneName))
                {
                    operation = SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }
                else
                {
                    SetActiveScene(sceneName);
                }
            }
            else
            {
                operation = SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }

            yield return new WaitForSeconds(0.3f);
            while (!operation.isDone)
            {
                yield return null;
            }
            HideLoadingImage();

            RNMessenger.SendResult(json, true);
            yield break;
        }

        public void ShowLoadingImage()
        {
            LoadingImage.SetActive(true);
        }

        public void HideLoadingImage()
        {
            LoadingImage.SetActive(false);
        }

        public void SetInActiveAllScene()
        {
            int countLoaded = SceneManager.sceneCount;

            for (int i = 0; i < countLoaded; i++)
            {
                // if(SceneManager.GetSceneAt(i).name == sceneName) return true;
                var rootObject = SceneManager.GetSceneAt(i).GetRootGameObjects();
                for (int j = 0; j < rootObject.Length; j++)
                {
                    var go = rootObject[j];
                    go.SetActive(false);
                    // objects.AddRange(go.GetComponentsInChildren<T>(true));
                }
            }
        }

        public void SetActiveScene(string sceneName)
        {
            int countLoaded = SceneManager.sceneCount;

            for (int i = 0; i < countLoaded; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    var rootObject = SceneManager.GetSceneAt(i).GetRootGameObjects();
                    SceneManager.SetActiveScene(SceneManager.GetSceneAt(i));
                    for (int j = 0; j < rootObject.Length; j++)
                    {
                        var go = rootObject[j];
                        go.SetActive(true);
                        // objects.AddRange(go.GetComponentsInChildren<T>(true));
                    }
                    return;
                }
            }
        }

        public bool HasScene(string sceneName)
        {
            int countLoaded = SceneManager.sceneCount;

            for (int i = 0; i < countLoaded; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName) return true;
            }
            return false;
        }

        private void ClearToEmpty()
        {
            while (true)
            {
                if (SceneManager.GetActiveScene().name == Constants.EmptySceneName) break;
                AsyncOperation operation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
                while (!operation.isDone) { }
            }
        }

        private void ClearToFeed()
        {
            while (true)
            {
                if (SceneManager.GetActiveScene().name == Constants.FeedSceneName || SceneManager.GetActiveScene().name == Constants.EmptySceneName) break;
                AsyncOperation operation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
                while (!operation.isDone) { }
            }
        }
    }
}