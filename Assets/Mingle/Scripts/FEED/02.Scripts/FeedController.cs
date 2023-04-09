using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Mingle
{
    public enum SpaceCategory { None, Friends, Recommend }

    public class FeedController : MonoBehaviour
    {
        [Header("Referenece")]
        [SerializeField] Camera _mainCamera;
        [SerializeField] Camera _subCamera;

        [SerializeField] RawImage _mainImage;
        [SerializeField] RawImage _subImage;

        [SerializeField] CameraManager _cameraManager;
        [SerializeField] GameObject _roomBase;

        [SerializeField] int __roomTransformListRecycledCount = 3;

        private int _friendRoomNum = 0;
        private int _recommendRoomNum = 0;
        private int _pointedRoomNum = 0;

        DefaultPool _defaultPool;

        private Vector3 _imageInitPos;
        private Objects[] _assignedObjectsList;
        private List<Transform> _roomTransformList = new List<Transform>();
        private Collider[] _floorColliderList;

        private Transform _poolingBox;
        private SpaceCategory _spaceCategory = SpaceCategory.None;
        private Dictionary<object, GameObject> _loadedAddressableObject = new Dictionary<object, GameObject>();
        private Dictionary<object, Queue<GameObject>> _pooledObjects = new Dictionary<object, Queue<GameObject>>();

        private List<Objects> _friendJsons = new List<Objects>();
        private List<Objects> _recommendJsons = new List<Objects>();

        // Start is called before the first frame update
        void Start()
        {
            Application.targetFrameRate = 60;

            _assignedObjectsList = new Objects[__roomTransformListRecycledCount];
            _floorColliderList = new Collider[__roomTransformListRecycledCount];
            _defaultPool = PhotonNetwork.PrefabPool as DefaultPool;

            _mainCamera.targetTexture.width = Screen.width;
            _mainCamera.targetTexture.height = Screen.height;
            _subCamera.targetTexture.width = Screen.width;
            _subCamera.targetTexture.height = Screen.height;

            _imageInitPos = _mainImage.rectTransform.position;

            for (int i = 0; i < __roomTransformListRecycledCount; i++)
            {
                _roomTransformList.Add(Instantiate(_roomBase).transform);
                _roomTransformList[i].transform.position = new Vector3(200f * i, 0f, 0f);
            }

            _poolingBox = new GameObject("Pooling Box").transform;
            _cameraManager.targetFind(_roomTransformList[0].gameObject);
            RecenterCamValues();

            LoadJsonsToLocal();
            FriendsButton();
        }

        // Update is called once per frame
        void Update()
        {
            if (_mainImage.rectTransform.position != _imageInitPos)
                _mainImage.rectTransform.position = Vector3.Lerp(_mainImage.rectTransform.position, _imageInitPos, Time.deltaTime * 5f);
        }

        void RecenterCamValues()
        {
            _cameraManager.recenterCameraAxis();
            _cameraManager.recenterCameraDistance();
            _cameraManager.recenterCameraOnMove();
        }

        public void SaveJson(string name, Objects objects)
        {
            File.WriteAllText(Application.persistentDataPath + "/" + name + ".json", String.Empty);
            StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/" + name + ".json", true);
            writer.WriteLine(JsonUtility.ToJson(objects));
            writer.Close();
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

        public void LoadJsonsToLocal()
        {
            for (int i = 0; i < 3; i++)
            {
                _friendJsons.Add(LoadJson(((i + 1) * 10).ToString()));
            }
            for (int i = 0; i < 3; i++)
            {
                _recommendJsons.Add(LoadJson(((i + 1) * 10).ToString()));
            }
        }

        // 가구일 경우 Destroy 대신 사용
        public void PoolingObject(GameObject target)
        {
            string tempName = target.name.Replace("(Clone)", "");

            if (!_pooledObjects.ContainsKey(tempName))
                _pooledObjects.Add(tempName, new Queue<GameObject>());

            target.transform.parent = _poolingBox;
            _pooledObjects[tempName].Enqueue(target);
            target.SetActive(false);
        }

        // 가구일 경우 Instantiate 대신 사용
        public GameObject CreateObject(string prefabName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject tempObj;
            string loadedName;

            if (_pooledObjects.ContainsKey(prefabName))
            {
                if (_pooledObjects[prefabName].Count > 0)
                {
                    tempObj = _pooledObjects[prefabName].Dequeue();
                    tempObj.SetActive(true);
                    tempObj.transform.position = position;
                    tempObj.transform.rotation = rotation;
                    tempObj.transform.localScale = scale;
                    return tempObj;
                }
            }

            if (!_loadedAddressableObject.ContainsKey(prefabName))
            {
                GameObject tempOrigin = AddressableManager.AddressableLoad(prefabName);
                _loadedAddressableObject.Add(prefabName, tempOrigin);
                if (!_defaultPool.ResourceCache.ContainsKey(tempOrigin.name))
                    _defaultPool.ResourceCache.Add(tempOrigin.name, tempOrigin);
                loadedName = tempOrigin.name;
            }
            else
                loadedName = _loadedAddressableObject[name].name;

            tempObj = PhotonNetwork.Instantiate(loadedName, position, rotation);
            tempObj.transform.localScale = scale;
            return tempObj;
        }

        public void ChangeRoom(Objects objects)
        {
            _subCamera.transform.position = _mainCamera.transform.position;
            _subCamera.transform.rotation = _mainCamera.transform.rotation;
            _cameraManager.targetFind(_roomTransformList[_pointedRoomNum].gameObject);

            if (!(_assignedObjectsList[_pointedRoomNum] == objects))
            {
                _assignedObjectsList[_pointedRoomNum] = objects;

                Transform[] children = _roomTransformList[_pointedRoomNum].GetComponentsInChildren<Transform>();
                foreach (Transform i in children)
                    if (i != _roomTransformList[_pointedRoomNum])
                        PoolingObject(i.gameObject);

                foreach (ObjectData i in objects.objects)
                {
                    Transform tempObj = CreateObject(i.name, i.position, i.rotation, i.scale).transform;
                    tempObj.transform.position += _roomTransformList[_pointedRoomNum].transform.position;
                    tempObj.parent = _roomTransformList[_pointedRoomNum];

                    // if (tempObj.tag == "Walkable")
                    if (tempObj.CompareTag(Constants.WalkableTag))
                    {
                        BoxCollider coll = tempObj.GetComponent<BoxCollider>();
                        float distance = MathF.Max(coll.bounds.size.x, coll.bounds.size.z) * 2.5f;
                        _cameraManager.VcamFraming.m_CameraDistance = distance;
                        _floorColliderList[_pointedRoomNum] = coll;
                    }
                }
            }
            else if (_floorColliderList[_pointedRoomNum] != null)
            {
                float distance = MathF.Max(_floorColliderList[_pointedRoomNum].bounds.size.x, _floorColliderList[_pointedRoomNum].bounds.size.z) * 2.5f;
                _cameraManager.VcamFraming.m_CameraDistance = distance;
            }

            _subCamera.backgroundColor = _mainCamera.backgroundColor;
            BackgroundInfo bgInfoMain = _roomTransformList[_pointedRoomNum].GetComponentInChildren<BackgroundInfo>();
            if (bgInfoMain != null)
                bgInfoMain.SetCameraColor(_mainCamera);
        }

        public void NextFeed()
        {
            float offset = _imageInitPos.x - _mainImage.rectTransform.position.x;

            if (Mathf.Abs(offset) > 10)
                return;

            int prevPrevNum = _pointedRoomNum - 1;
            int prevNum = _pointedRoomNum;
            _pointedRoomNum += 1;

            if (_pointedRoomNum > __roomTransformListRecycledCount - 1)
                _pointedRoomNum = 0;

            if (prevPrevNum < 0)
                prevPrevNum = __roomTransformListRecycledCount - 1;

            _roomTransformList[prevPrevNum].gameObject.SetActive(false);
            _roomTransformList[_pointedRoomNum].gameObject.SetActive(true);
            _roomTransformList[prevNum].gameObject.SetActive(true);

            switch (_spaceCategory)
            {
                case SpaceCategory.Recommend:
                    if (_recommendRoomNum + 1 >= _friendJsons.Count)
                        _recommendRoomNum = -1;

                    ChangeRoom(_recommendJsons[++_recommendRoomNum]);
                    SlideToTarget(Screen.width);
                    break;

                case SpaceCategory.Friends:
                    if (_friendRoomNum + 1 >= _friendJsons.Count)
                        _friendRoomNum = -1;

                    ChangeRoom(_friendJsons[++_friendRoomNum]);
                    SlideToTarget(Screen.width);
                    break;

                default:
                    break;
            }
        }

        public void PrevFeed()
        {
            float offset = _imageInitPos.x - _mainImage.rectTransform.position.x;

            if (Mathf.Abs(offset) > 10)
                return;

            int prevPrevNum = _pointedRoomNum + 1;
            int prevNum = _pointedRoomNum;
            _pointedRoomNum -= 1;

            if (_pointedRoomNum < 0)
                _pointedRoomNum = __roomTransformListRecycledCount - 1;

            if (prevPrevNum > __roomTransformListRecycledCount - 1)
                prevPrevNum = 0;

            _roomTransformList[prevPrevNum].gameObject.SetActive(false);
            _roomTransformList[_pointedRoomNum].gameObject.SetActive(true);
            _roomTransformList[prevNum].gameObject.SetActive(true);

            switch (_spaceCategory)
            {
                case SpaceCategory.Recommend:
                    if (_recommendRoomNum - 1 < 0)
                        _recommendRoomNum = _recommendJsons.Count;

                    ChangeRoom(_recommendJsons[--_recommendRoomNum]);
                    SlideToTarget(-Screen.width);
                    break;

                case SpaceCategory.Friends:
                    if (_friendRoomNum - 1 < 0)
                        _friendRoomNum = _friendJsons.Count;

                    ChangeRoom(_friendJsons[--_friendRoomNum]);
                    SlideToTarget(-Screen.width);
                    break;

                default:
                    break;
            }
        }

        public void ChangeType(string type)
        {
            switch (type)
            {
                case "Friend":
                    FriendsButton();
                    break;
                case "Recommend":
                    RecommendButton();
                    break;
                default:
                    break;
            }
        }

        public void RecommendButton()
        {
            if (_spaceCategory == SpaceCategory.Recommend)
                return;

            _spaceCategory = SpaceCategory.Recommend;
            ChangeRoom(_recommendJsons[_recommendRoomNum]);
            SlideToTarget(0);
        }

        public void FriendsButton()
        {
            if (_spaceCategory == SpaceCategory.Friends)
                return;

            _spaceCategory = SpaceCategory.Friends;
            ChangeRoom(_friendJsons[_friendRoomNum]);
            SlideToTarget(0);
        }

        public void SlideToTarget(float imagePos)
        {
            _mainImage.rectTransform.position = _imageInitPos + new Vector3(imagePos, 0, 0);
            RecenterCamValues();
        }

        private void OnDestroy()
        {
            List<GameObject> tempLoaded = new List<GameObject>(_loadedAddressableObject.Values);
            AddressableManager.ReleaseArray(tempLoaded);
        }
    }
}