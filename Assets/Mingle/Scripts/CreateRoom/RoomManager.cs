using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mingle;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Mingle
{
    public class RoomManager : MonoBehaviour
    {
        //Transform _spaceParent;
        Collider _groundCollider;
        DefaultPool _defaultPool = null;

        public JObject _currentInfoJSON = new JObject();
        public PlayerActionManager _currentPlayerActionManager;

        List<GameObject> _allActiveObjects = new List<GameObject>();
        Dictionary<object, GameObject> _addressableLoaded = new Dictionary<object, GameObject>();

        // Start is called before the first frame update
        void Start()
        {
            //_spaceParent = new GameObject("Room").transform;
            _defaultPool = PhotonNetwork.PrefabPool as DefaultPool;

            // Load(JsonUtility.ToJson(LoadJson(testRoomName)));
            // Load(Constants.DefaultJSONS[2]);
        }

        private void Update()
        {
            //CameraMovement();
        }

        public void RoomChange(int idx)
        {
            Load(Constants.DefaultJSONS[idx]);
        }

        public Objects LoadJson(string name)
        {
            FileStream fileStream = new FileStream(Application.persistentDataPath + "/" + name + ".json", FileMode.Open);
            byte[] data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();
            string json = Encoding.UTF8.GetString(data);

            Objects objects = JsonUtility.FromJson<Objects>(json);

            return objects;
        }

        public void CreateRoom(Objects roomData)
        {
            //if (_spaceParent == null)
            //    _spaceParent = new GameObject("Room").transform;

            foreach (ObjectData i in roomData.objects)
            {
                if (i.name == "post processing" || i.name.Contains("_Settings")) continue;
                GameObject tempObj = AddObject(i.name, i.position, i.rotation, i.scale);
                //tempObj.transform.parent = _spaceParent;

                //if (tempObj.tag == "Walkable")
                if (tempObj.CompareTag(Constants.WalkableTag))
                {
                    _groundCollider = tempObj.GetComponent<BoxCollider>();
                    _groundCollider.gameObject.GetComponent<NavMeshSurface>().BuildNavMesh();
                }
            }

            //BackgroundInfo bgInfo = _spaceParent.GetComponentInChildren<BackgroundInfo>();
            _groundCollider.gameObject.GetComponent<NavMeshSurface>().BuildNavMesh();

            //if (bgInfo != null)
            //    bgInfo.SetCameraColor(Camera.main);
        }

        public GameObject AddObject(string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject loadedObject = null;
            if (!_addressableLoaded.ContainsKey(name))
            {
                loadedObject = AddressableManager.AddressableLoad(name);
                if (loadedObject == null)
                {
                    Debug.LogError("AddressableLoad Failed: " + name);
                    return null;
                }
                _addressableLoaded.Add(name, loadedObject);
            }
            else
                loadedObject = _addressableLoaded[name];

            if (_defaultPool == null) _defaultPool = PhotonNetwork.PrefabPool as DefaultPool;
            if (!_defaultPool.ResourceCache.ContainsKey(loadedObject.name))
                _defaultPool.ResourceCache.Add(loadedObject.name, loadedObject);
            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable objHashtable = new ExitGames.Client.Photon.Hashtable();

                List<string> objects = new List<string>();
                foreach (JObject obj in _currentInfoJSON["objects"].ToObject<JArray>().Children<JObject>())
                {
                    if (!objects.Contains(obj["name"].ToString())) objects.Add(obj["name"].ToString());
                }
                objHashtable["RoomInfo"] = string.Join(",", objects);

                PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);

                if (_currentPlayerActionManager != null)
                    _currentPlayerActionManager.UpdateHashClient();

                GameObject createdObject = PhotonNetwork.InstantiateRoomObject(loadedObject.name, position, rotation);
                createdObject.name = name;
                createdObject.transform.localScale = scale == Vector3.zero ? _addressableLoaded[name].transform.localScale : scale;
                //createdObject.transform.parent = _spaceParent.parent;
                _allActiveObjects.Add(createdObject);

                return createdObject;
            }

            return null;
        }

        public void DestroyObject(GameObject target)
        {
            if (target != null)
                PhotonNetwork.Destroy(target);
        }

        public void DestroyAllObjects()
        {
            PhotonView[] tempObject = FindObjectsOfType<PhotonView>();

            foreach (PhotonView i in tempObject)
            {
                Debug.Log("KAIKAI" + i.tag);
                //if (i.tag == "ObjectSelectable" || i.tag == "ObjectNonSelectable" || i.tag == "ObjectInteractable" || i.tag == "Walkable")
                if (i.CompareTag(Constants.ObjectSelectableTag) || i.CompareTag(Constants.ObjectNonSelectableTag) || i.CompareTag(Constants.ObjectInteractableTag) || i.CompareTag(Constants.WalkableTag))
                    DestroyObject(i.gameObject);
            }
        }

        public void Load(string json)
        {
            _currentInfoJSON = JObject.Parse(json);
            Objects objects = JsonUtility.FromJson<Objects>(json.ToString());

            DestroyAllObjects();
            CreateRoom(objects);
        }

        public void UpdateHash()
        {
            ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
            string objects = objHashtable["RoomInfo"].ToString();
            if (!string.IsNullOrEmpty(objects))
            {
                DefaultPool defaultPool = PhotonNetwork.PrefabPool as DefaultPool;
                foreach (string objectName in objects.Split(','))
                {
                    if (objectName == "post processing") continue;
                    // AddressableManager.AddressableLoad(objectName, out preLoadObject);   
                    // defaultPool = PhotonNetwork.PrefabPool as DefaultPool

                    GameObject preLoadObject = AddressableManager.AddressableLoad(objectName);
                    if (defaultPool == null) defaultPool = PhotonNetwork.PrefabPool as DefaultPool;
                    if (!defaultPool.ResourceCache.ContainsKey(objectName))
                        defaultPool.ResourceCache.Add(objectName, preLoadObject);
                }
            }
        }

        public void LoadByName(int i)
        {
            _currentPlayerActionManager.LoadButton(i);
        }
    }
}
