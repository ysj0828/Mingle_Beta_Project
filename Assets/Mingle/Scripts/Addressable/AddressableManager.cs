using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Mingle
{
    public static class AddressableManager
    {
        static Dictionary<string, GameObject> _objectPool = new Dictionary<string, GameObject>();
        // 업데이트 카탈로그
        public static void UpdateCatalog()
        {
            Addressables.CheckForCatalogUpdates().Completed += (result) =>
            {
                var catalogToUpdate = result.Result;
                if (catalogToUpdate.Count > 0)
                {
                    Addressables.UpdateCatalogs(catalogToUpdate);
                }
                else
                {
                    Debug.Log("업데이트 할 내용이 없습니다.");
                }
            };
        }

        public static GameObject AddressableLoad(string assetName)
        {
            // float startTime = Time.realtimeSinceStartup;
            if (!_objectPool.ContainsKey(assetName))
            {
                var temp = Addressables.LoadAssetAsync<GameObject>(assetName);
                if (temp.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
                {
                    Debug.Log(assetName + " : " + temp.Status);
                    temp = Addressables.LoadAssetAsync<GameObject>("Cube");
                    _objectPool[assetName] = temp.WaitForCompletion();
                }
                else
                {
                    _objectPool[assetName] = temp.WaitForCompletion();
                }
            }
            // Debug.Log("KAIKAI2 " + assetName + " : " + (Time.realtimeSinceStartup - startTime));
            return _objectPool[assetName];
        }

        public static GameObject AddressableCustomLoad(string assetName)
        {
            // float startTime = Time.realtimeSinceStartup;
            if (!_objectPool.ContainsKey(assetName))
            {
                var temp = Addressables.LoadAssetAsync<GameObject>(assetName);
                _objectPool[assetName] = temp.WaitForCompletion();
            }
            // Debug.Log("KAIKAI2 " + assetName + " : " + (Time.realtimeSinceStartup - startTime));
            return _objectPool[assetName];
        }

        public static void AddressableTextLoad(string assetName, out TextAsset loadObj)
        {
            var temp = Addressables.LoadAssetAsync<TextAsset>(assetName);
            loadObj = temp.WaitForCompletion();
        }

        // 인스턴티에이트 로드
        public static void AddressableInsLoad(string assetName, Vector3 objPos, Quaternion objRot, out GameObject loadObj)
        {
            var temp = Addressables.InstantiateAsync(assetName, objPos, objRot);
            loadObj = temp.WaitForCompletion();
        }

        // 애니메이터 로드
        public static void AnimatorControllerLoad(string assetName, out RuntimeAnimatorController loadAniController)
        {
            var temp = Addressables.LoadAssetAsync<RuntimeAnimatorController>(assetName);
            loadAniController = temp.WaitForCompletion();
        }

        //---------------------------------------------------------------------------------------------------//
        // 릴리즈

        // 에셋 릴리즈
        public static void Release(GameObject loadObj)
        {
            Addressables.Release(loadObj);
        }

        // 에셋 리스트 릴리즈 
        public static void ReleaseArray(List<GameObject> loadObj)
        {
            for (int i = 0; i < loadObj.Count; i++)
            {
                Addressables.Release(loadObj[i]);
            }
        }

        // Instantiate 릴리즈
        public static void ReleaseIns(GameObject loadObj)
        {
            Addressables.ReleaseInstance(loadObj);
        }

        // Instantiate 리스트 릴리즈
        public static void ReleaseArrayIns(List<GameObject> loadObj)
        {
            for (int i = 0; i < loadObj.Count; i++)
            {
                Addressables.ReleaseInstance(loadObj[i]);
            }
        }

        // 애니메이터 릴리즈
        public static void ReleaseAnimator(RuntimeAnimatorController loadAniController)
        {
            Addressables.Release(loadAniController);
        }
    }
}