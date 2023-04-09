using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using Mingle;
using System.IO;
using System.Text;
using Photon.Pun;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;

[DisallowMultipleComponent]
public class EditController : MonoBehaviour
{
    bool _canObjectMoving = false;
    bool _canObjectRotating = false;
    bool _isObjectRotating = false;
    bool _isObjectMoving = false;
    bool _click_movementUI = false;
    bool _clickRotateUI = false;

    public string preset_id = null;
    public int inventory_space_id = 0;
    public string shop_space_id = null;

    // public bool isViewer = false;
    float _touchTime = 0;
    float _combinedTouchX, _combinedTouchY;

    Touch _singleTouch;
    RaycastHit _raycastHit;
    Vector2 _initTouchPos;
    Vector3 _objectTouchOffset = Vector2.zero;

    Collider _groundCollider;
    BoxCollider _selectedObjectBox;
    GameObject _outOfFloor;
    ObjectManager _selectedObjectManager;
    Objects _initRoomData = null;

    DefaultPool _defaultPool;

    private Camera _mainCam;

    List<ObjectManager> _initObjects = new List<ObjectManager>();
    List<GameObject> _allActiveObjects = new List<GameObject>();
    List<LineRenderer> _gridLine = new List<LineRenderer>();
    Queue<LineRenderer> _gridStorage = new Queue<LineRenderer>();

    Dictionary<object, GameObject> _addressableLoaded = new Dictionary<object, GameObject>();
    Dictionary<object, Queue<GameObject>> _pooledObjects = new Dictionary<object, Queue<GameObject>>();

    [SerializeField] float _overlapBoxOffset = -3f;
    [SerializeField] float _zoomSensitivity = 0.01f;
    [SerializeField] float _zoomMax = 13;
    [SerializeField] float _zoomMin = 2.7f;
    [SerializeField] float _clampSpeed = 0.4f;
    [SerializeField] float _exitLimitTime = 0.8f;

    [SerializeField] Text _historyCountText;
    [SerializeField] Slider _rotationUI;
    [SerializeField] Transform _zTestBoundPlane;
    [SerializeField] RectTransform _movementUI;
    [SerializeField] RectTransform _deleteButton;
    [SerializeField] CameraManager _camManager;
    [SerializeField] Material _lineMaterial;
    [SerializeField] EditMode _editMode;
    [SerializeField] SelectMode _selectMode;

    [HideInInspector] public GameObject _selectedObject;
    [HideInInspector] public List<Collider> _overlapColliderList = new List<Collider>();
    [HideInInspector] public List<Collider> _overlapChildColliderList = new List<Collider>();

    Stack<History> _previousHistory = new Stack<History>();
    Stack<History> _afterHistory = new Stack<History>();
    History _currentHistory = null;

    public bool _isRnMode = false;

    [Header("EmptyObjects")]
    public Transform _spaceParent;
    public Transform _poolingBox;
    public Transform _lineBox;
    public GameObject _cameraCentricObject;

    enum EditMode { Drag, Pointer }
    enum SelectMode { Selectable, NonSelectable }

    public enum HistoryCategory { Move, Rotate, Remove, Create, Select }

    [Serializable]
    public class History
    {
        public HistoryCategory _historyCategory;
        public ObjectManager _targetObject;
        public Vector3 _position;
        public Quaternion _rotation;
        public Vector3 _scale;
        public string _objectName;
        public int _targetObjectIdentify;

        public ObjectManager _parentObject;
        public int _parentObjectID;

        public ObjectManager[] _childObjects;
        public int[] _childObjectsID;

        public IdentityData _identityData;

        public History(HistoryCategory cate_selectedObjectry, ObjectManager target)
        {
            this._historyCategory = cate_selectedObjectry;
            this._targetObject = target;
            this._objectName = target.name.Replace("(Clone)", "");
            this._scale = target.transform.lossyScale;
            this._targetObjectIdentify = target.GetObjectID();
            this._identityData = target.GetID();
            UpdateTransform();
        }

        public void RenewDestoryedObject(ObjectManager target, int id)
        {
            if (this._targetObjectIdentify == id)
            {
                this._targetObject = target;
                this._targetObjectIdentify = target.GetObjectID();
            }
            else if (this._parentObjectID == id)
            {
                this._parentObject = target;
                this._parentObjectID = target.GetObjectID();
            }
            for (int i = 0; i < _childObjectsID.Length; i++)
            {
                if (_childObjectsID[i] == id)
                {
                    this._childObjects[i] = target;
                    this._childObjectsID[i] = target.GetObjectID();
                }
            }
        }

        public void UpdateTransform()
        {
            this._position = _targetObject.transform.position;
            this._rotation = _targetObject.transform.rotation;

            this._parentObject = _targetObject.transform.parent.GetComponent<ObjectManager>();

            if (_parentObject != null)
                this._parentObjectID = _parentObject.GetObjectID();

            List<ObjectManager> tempObjectManager = new List<ObjectManager>();
            this._childObjects = new ObjectManager[_targetObject.transform.childCount];

            for (int i = 0; i < _targetObject.transform.childCount; i++)
            {
                Transform tempChild = _targetObject.transform.GetChild(i);
                //if (tempChild.tag == "ObjectSelectable" || tempChild.tag == "ObjectInteractable")
                if (tempChild.CompareTag(Constants.ObjectSelectableTag) || tempChild.CompareTag(Constants.ObjectInteractableTag))
                    tempObjectManager.Add(tempChild.GetComponent<ObjectManager>());
            }

            this._childObjects = tempObjectManager.ToArray();
            this._childObjectsID = new int[_childObjects.Length];

            for (int i = 0; i < _childObjects.Length; i++)
                this._childObjectsID[i] = _childObjects[i].GetObjectID();
        }

        public void RemoveTargetObject()
        {
            this._targetObject = null;
        }
    }

    private void Start()
    {
        // exitObject = new GameObject("ExitObject");
        // room = new GameObject("Room").transform;
        // poolingBox = new GameObject("PoolingBox").transform;
        // lineBox = new GameObject("LineBox").transform;
        _mainCam = Camera.main;

        _camManager.lockTwoTouch = true;

        _camManager.VcamFraming.m_DeadZoneHeight = 0f;
        _camManager.VcamFraming.m_DeadZoneWidth = 0f;

        _defaultPool = PhotonNetwork.PrefabPool as DefaultPool;

        // Load(JsonUtility.ToJson(LoadJson(testRoomName)));
        _camManager.targetFind(_cameraCentricObject);

        _camManager.VcamFraming.m_DeadZoneHeight = 0.6f;
        _camManager.VcamFraming.m_DeadZoneWidth = 0.4f;
        _camManager.recenterCameraAxis();
    }

    private void Update()
    {
        if (_selectMode == SelectMode.NonSelectable)
            return;

        if (Input.touchCount == 1)
        {
            _singleTouch = Input.touches[0];
        }

        switch (Input.touchCount)
        {
            case 0:
                break;

            case 1:
                _touchTime += Time.deltaTime;

                if (_singleTouch.phase == TouchPhase.Began)
                {
                    Physics.Raycast(_mainCam.ScreenPointToRay(_singleTouch.position), out _raycastHit);
                    Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
                    RaycastHit tempHit;

                    _initTouchPos = Input.touches[0].position;

                    if (Physics.Raycast(ray, out tempHit, float.MaxValue, 1 << LayerMask.NameToLayer("ObjectBound")))
                        _click_movementUI = true;

                    if (_selectedObject != null)
                    {
                        if (Physics.Raycast(ray, out tempHit, float.MaxValue, 1 << LayerMask.NameToLayer("Ground")))
                            _objectTouchOffset = Input.mousePosition - _mainCam.WorldToScreenPoint(new Vector3(_selectedObject.transform.position.x, tempHit.collider.bounds.center.y + tempHit.collider.bounds.size.y / 2f, _selectedObject.transform.position.z));
                        else
                            _objectTouchOffset = Input.mousePosition - _mainCam.WorldToScreenPoint(new Vector3(_selectedObject.transform.position.x, _selectedObjectBox.bounds.center.y + _selectedObjectBox.bounds.size.y / 2f, _selectedObject.transform.position.z));
                        SelectedObjectBoundCheck();
                    }
                }

                if (_singleTouch.phase == TouchPhase.Ended)
                {
                    if (_click_movementUI && _selectedObject != null)
                        SaveHistory(HistoryCategory.Move, _selectedObjectManager);
                    else if (_raycastHit.transform != null)
                        if (_canObjectMoving && _raycastHit.transform.gameObject == _selectedObject)
                            SaveHistory(HistoryCategory.Move, _selectedObjectManager);

                    Physics.Raycast(_mainCam.ScreenPointToRay(_singleTouch.position), out _raycastHit);

                    if (!_isObjectRotating && !_isObjectMoving)
                    {
                        if (_raycastHit.transform != null && !IsPointerOverUI(Input.GetTouch(0)))
                        {
                            //if ((_raycastHit.transform.tag == "ObjectSelectable" || _raycastHit.transform.tag == "ObjectInteractable") && !GetChildrenOverlap() && _raycastHit.transform.gameObject.layer != LayerMask.NameToLayer("Ground"))
                            if ((_raycastHit.transform.CompareTag(Constants.ObjectSelectableTag) || _raycastHit.transform.CompareTag(Constants.ObjectInteractableTag)) && !GetChildrenOverlap() && _raycastHit.transform.gameObject.layer != LayerMask.NameToLayer("Ground"))
                            {
                                ChangeSelectedObject(_raycastHit.transform);
                                SaveHistory(HistoryCategory.Select, _raycastHit.transform.gameObject.GetComponent<ObjectManager>());
                            }
                            else if (!_raycastHit.transform.CompareTag(Constants.ObjectSelectedTag) && _touchTime < _exitLimitTime && !GetChildrenOverlap())
                            {
                                if (_currentHistory != null && _selectedObject != null)
                                    if (_currentHistory._historyCategory == HistoryCategory.Rotate || _currentHistory._historyCategory == HistoryCategory.Move)
                                        HistoryExit();
                                ExitEditMode();
                            }
                        }
                    }

                    _touchTime = 0;
                    _click_movementUI = false;

                    SetIsRotating(false);
                    SetIsMoving(false);
                    _raycastHit = new RaycastHit();
                }

                break;

            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        CameraMovement();
        CamCentering();

        if (_selectedObject != null && _movementUI != null)
            _movementUI.position = _mainCam.WorldToScreenPoint(_selectedObject.transform.position);

        if (Input.touchCount == 1)
            _singleTouch = Input.touches[0];

        switch (Input.touchCount)
        {
            case 0:
                break;

            case 1:
                _touchTime += Time.deltaTime;

                if ((_initTouchPos - _singleTouch.position).magnitude > 5 && _raycastHit.transform != null)
                    if (_canObjectMoving && _raycastHit.transform.gameObject == _selectedObject)
                        MoveAssetByPoint();
                if (_click_movementUI)
                    MoveAssetByPoint();

                if (_raycastHit.collider != null)
                {
                    //if (_raycastHit.transform.tag == "ObjectSelectable" || _raycastHit.transform.tag == "ObjectInteractable")
                    if (_raycastHit.transform.CompareTag(Constants.ObjectSelectableTag) || _raycastHit.transform.CompareTag(Constants.ObjectInteractableTag))
                    {
                        GameObject prevObject = _raycastHit.transform.gameObject;
                        Physics.Raycast(_mainCam.ScreenPointToRay(_singleTouch.position), out _raycastHit);
                        if (_raycastHit.transform != null)
                        {
                            if (!_raycastHit.transform.CompareTag(Constants.ObjectSelectableTag) || !_raycastHit.transform.CompareTag(Constants.ObjectInteractableTag))
                            {
                                _raycastHit = new RaycastHit();
                            }
                            else
                            {
                                GameObject currentObject = _raycastHit.transform.gameObject;
                                if (prevObject != currentObject)
                                {
                                    _raycastHit = new RaycastHit();
                                }
                            }
                        }
                    }
                }

                break;

            case 2:
                break;
        }
    }

    public void CamCentering()
    {
        if (_selectMode == SelectMode.NonSelectable && _editMode == EditMode.Pointer)
            if (Mathf.Abs(_singleTouch.deltaPosition.x) + Mathf.Abs(_singleTouch.deltaPosition.y) > 8f)
                _cameraCentricObject.transform.position = _spaceParent.transform.position;
    }

    public void SetOutOfFloor(Vector3 position)
    {
        if (_outOfFloor == null)
        {
            _outOfFloor = new GameObject("OutFloor");
            _outOfFloor.layer = LayerMask.NameToLayer("OutArea");
            BoxCollider col = _outOfFloor.AddComponent<BoxCollider>();
            col.size = new Vector3(200f, 0.1f, 200f);
        }

        _outOfFloor.transform.position = position - new Vector3(0, 0.5f, 0);
    }

    //Room Edit
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

    public void CreateRoom(Objects roomData)
    {
        if (_spaceParent == null)
            _spaceParent = new GameObject("Room").transform;

        if (_initRoomData == null)
            _initRoomData = roomData;

        foreach (ObjectData i in roomData.objects)
        {
            if (i.name == "post processing" || i.name.Contains("_Settings")) continue;
            GameObject tempObj = AddObject(i.name, i.position, i.rotation, i.scale, i.shop_object_id, i.inventory_object_id, i.preset_detail_id, i.room_preset_detail_id);
            if (tempObj == null) continue;
            tempObj.transform.parent = _spaceParent;

            //if (tempObj.tag == "Walkable")
            if (tempObj.CompareTag(Constants.WalkableTag))
            {
                _groundCollider = tempObj.GetComponent<Collider>();
                SetOutOfFloor(_groundCollider.transform.position);
                _camManager.VcamFramingVC.m_Lens.OrthographicSize = _groundCollider.bounds.size.x > _groundCollider.bounds.size.z ? _groundCollider.bounds.size.x * 1.5f : _groundCollider.bounds.size.z * 1.5f;
                _camManager.VcamFramingVC.m_Lens.FieldOfView = _groundCollider.bounds.size.x > _groundCollider.bounds.size.z ? _groundCollider.bounds.size.x * 1.5f : _groundCollider.bounds.size.z * 1.5f;
            }

            Collider groundCollider = tempObj.GetComponent<Collider>();
            if (tempObj.layer == LayerMask.NameToLayer("Ground") && _selectMode == SelectMode.Selectable && groundCollider != null)
                DrawGrid(groundCollider);

            if (_selectMode != SelectMode.Selectable && tempObj.layer == LayerMask.NameToLayer("Ground") && tempObj.CompareTag(Constants.ObjectSelectableTag))
                tempObj.tag = "Walkable";
        }

        BackgroundInfo bgInfo = _spaceParent.GetComponentInChildren<BackgroundInfo>();
        if (bgInfo != null)
            bgInfo.SetCameraColor(_mainCam);

        _cameraCentricObject.transform.position = _groundCollider != null ? _groundCollider.transform.position : _spaceParent.transform.position;
    }

    // public void AddObject(string name, string inventory_object_id, string preset_detail_id, string room_preset_detail_id)
    // {
    //     GameObject createdObject = AddObject(name, null, null, null);
    //     SaveHistory(HistoryCategory.Create, createdObject);
    // }

    public GameObject AddObject(string name, string shop_object_id, int inventory_object_id = 0, int preset_detail_id = 0, int room_preset_detail_id = 0)
    {
        if (GetChildrenOverlap())
            return null;

        RaycastHit hit;
        Vector3 setPosition = _groundCollider.transform.position;
        Ray ray = _mainCam.ScreenPointToRay(new Vector2(_mainCam.pixelWidth / 2, _mainCam.pixelHeight / 2));

        if (Physics.Raycast(ray, out hit, float.MaxValue, 1 << LayerMask.NameToLayer("Ground")))
        {
            setPosition = new Vector3(hit.point.x, hit.collider.bounds.center.y + hit.collider.bounds.size.y / 2f, hit.point.z);
        }
        else if (Physics.Raycast(ray, out hit, float.MaxValue, 1 << LayerMask.NameToLayer("_outOfFloor")))
        {
            setPosition = new Vector3(Mathf.Clamp(hit.point.x, _groundCollider.bounds.min.x, _groundCollider.bounds.max.x),
                _groundCollider.transform.position.y + (_groundCollider.bounds.size.y / 2f + _groundCollider.bounds.center.y) * _groundCollider.transform.localScale.y,
                Mathf.Clamp(hit.point.z, _groundCollider.bounds.min.z, _groundCollider.bounds.max.z));
        }

        GameObject createdObject = AddObject(name, setPosition, Quaternion.identity, Vector3.zero, shop_object_id, inventory_object_id, preset_detail_id, room_preset_detail_id);
        ObjectManager tempOM = createdObject.GetComponent<ObjectManager>();
        ChangeSelectedObject(createdObject.transform);
        StartCoroutine(AddObjectHistoryCheck(tempOM, hit));

        return createdObject;
    }

    IEnumerator AddObjectHistoryCheck(ObjectManager target, RaycastHit hit)
    {
        yield return new WaitUntil(() => target._initBoxCheck);

        //if (hit.transform.gameObject.tag == "ObjectSelectable" && target.transform.parent == GetRoom())
        if (hit.transform.gameObject.CompareTag(Constants.ObjectSelectableTag) && target.transform.parent == GetRoom())
        {
            _overlapColliderList.Add(hit.collider);
            target.transform.parent = hit.transform;
        }

        SaveHistory(HistoryCategory.Create, target);
    }

    public GameObject AddObject(string name, Vector3 position, Quaternion rotation, Vector3 scale, string shop_object_id, int inventory_object_id = 0, int preset_detail_id = 0, int room_preset_detail_id = 0)
    {
        GameObject createdObject;
        ObjectManager tempOM = null;
        if (_pooledObjects.ContainsKey(name) && _pooledObjects[name].Count > 0)
        {
            createdObject = _pooledObjects[name].Dequeue();
            createdObject.transform.position = position;
            createdObject.transform.rotation = rotation;
            createdObject.transform.localScale = scale == Vector3.zero ? _addressableLoaded[name].transform.localScale : scale;
            tempOM = createdObject.AddComponent<ObjectManager>();
            tempOM._editController = this;
        }
        else
        {
            if (!_addressableLoaded.ContainsKey(name))
            {
                GameObject loadedObject = AddressableManager.AddressableLoad(name);
                if (loadedObject == null)
                {
                    Debug.LogError("AddressableLoad Fail : " + name + " is null");
                    return null;
                }
                _addressableLoaded.Add(name, loadedObject);
                if (_defaultPool != null && !_defaultPool.ResourceCache.ContainsKey(loadedObject.name))
                    _defaultPool.ResourceCache.Add(loadedObject.name, loadedObject);
            }
            createdObject = Instantiate(_addressableLoaded[name], position, rotation);
            createdObject.name = name;
            createdObject.transform.localScale = scale == Vector3.zero ? _addressableLoaded[name].transform.localScale : scale;

            //if (createdObject.tag == "ObjectSelectable" || createdObject.tag == "ObjectInteractable")
            if (createdObject.CompareTag(Constants.ObjectSelectableTag) || createdObject.CompareTag(Constants.ObjectInteractableTag))
            {
                tempOM = createdObject.AddComponent<ObjectManager>();
                tempOM._editController = this;
            }
        }

        createdObject.transform.parent = _spaceParent;
        _allActiveObjects.Add(createdObject);

        if (tempOM != null)
        {
            tempOM.SetID(new IdentityData(name, shop_object_id, inventory_object_id, preset_detail_id, room_preset_detail_id));
        }

        if (createdObject != null)
            createdObject.SetActive(true);

        return createdObject;
    }

    public void PoolingObject(GameObject target)
    {
        string tempName = target.name.Replace("(Clone)", "");
        int count = target.transform.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform tempChild = target.transform.GetChild(0);
            //if (tempChild.tag == "ObjectSelectable" || tempChild.tag == "ObjectInteractable")
            if (tempChild.CompareTag(Constants.ObjectSelectableTag) || tempChild.CompareTag(Constants.ObjectInteractableTag))
                tempChild.parent = _spaceParent;
        }

        foreach (History i in _previousHistory)
            if (i._targetObject == target)
                i.RemoveTargetObject();

        foreach (History i in _afterHistory)
            if (i._targetObject == target)
                i.RemoveTargetObject();

        if (!_pooledObjects.ContainsKey(tempName))
            _pooledObjects.Add(tempName, new Queue<GameObject>());

        target.transform.parent = _poolingBox;
        _pooledObjects[tempName].Enqueue(target);
        Destroy(target.GetComponent<ObjectManager>());
        target.SetActive(false);

        _allActiveObjects.Remove(target.gameObject);
    }

    public void PoolingAllObjects()
    {
        GameObject[] tempList = _allActiveObjects.ToArray();
        foreach (GameObject i in tempList)
        {
            PoolingObject(i);
        }
    }

    public void ResetRoom()
    {
        _previousHistory.Clear();
        _afterHistory.Clear();
        if (_historyCountText != null) _historyCountText.text = "0 / 0";

        PoolingAllObjects();
        if (_selectMode == SelectMode.Selectable)
            PoolingGrid();
        CreateRoom(_initRoomData);

        ExitEditMode();
    }

    public JArray ReturnAddedObjects()
    {
        // List<IdentityData> objects = new List<IdentityData>();
        JArray objects = new JArray();

        foreach (GameObject i in _allActiveObjects)
        {
            ObjectManager tempOM = i.GetComponent<ObjectManager>();
            if (tempOM != null)
            {
                if (!tempOM.GetIntactBool())
                {
                    if (!string.IsNullOrEmpty(tempOM.GetID().shop_object_id) && tempOM.GetID().preset_detail_id == 0 && tempOM.GetID().room_preset_detail_id == 0)
                    {
                        JObject Object = new JObject();
                        Object["name"] = tempOM.GetID().name;
                        Object["shop_object_id"] = tempOM.GetID().shop_object_id;
                        Object["inventory_object_id"] = tempOM.GetID().inventory_object_id;
                        objects.Add(Object);
                    }
                }
            }
        }

        return objects;
    }

    public JArray ReturnDeletedObjects()
    {
        JArray objects = new JArray();
        List<History> tempHistory = _previousHistory.ToList();

        if (_currentHistory != null)
            tempHistory.Add(_currentHistory);

        foreach (History i in tempHistory)
        {
            if (i._historyCategory != HistoryCategory.Remove) continue;
            if (!string.IsNullOrEmpty(i._identityData.shop_object_id) && (i._identityData.preset_detail_id != 0 || i._identityData.room_preset_detail_id != 0))
            {
                JObject Object = new JObject();
                Object["name"] = i._identityData.name;
                Object["shop_object_id"] = i._identityData.shop_object_id;
                Object["inventory_object_id"] = i._identityData.inventory_object_id;
                if (i._identityData.preset_detail_id != null) Object["preset_detail_id"] = i._identityData.preset_detail_id;
                if (i._identityData.room_preset_detail_id != null) Object["room_preset_detail_id"] = i._identityData.room_preset_detail_id;
                objects.Add(Object);
            }
        }

        return objects;
    }

    public void DebugDeletedObjects()
    {
        Debug.Log(ReturnDeletedObjects());
    }

    // 히스토리 리두 언두 
    // 오브젝트 삭제 / 추가
    // 

    public void UpdatePresetDetailId(JArray objects)
    {

        foreach (JObject updateInfo in objects)
        {
            string shop_object_id = updateInfo["shop_object_id"].ToString();
            int inventory_object_id = updateInfo["inventory_object_id"].ToObject<int>();
            int preset_detail_id = updateInfo["preset_detail_id"].ToObject<int>();

            foreach (GameObject i in _allActiveObjects)
            {
                ObjectManager tempOM = i.GetComponent<ObjectManager>();
                if (tempOM == null) continue;
                if (tempOM.GetIntactBool()) continue;

                if (tempOM.GetID().shop_object_id == shop_object_id && tempOM.GetID().preset_detail_id == 0)
                {
                    tempOM.GetID().inventory_object_id = inventory_object_id;
                    tempOM.GetID().preset_detail_id = preset_detail_id;
                    break;
                }

                // if (tempOM.GetID().inventory_object_id == inventory_object_id && string.IsNullOrEmpty(tempOM.GetID().preset_detail_id))
                // {
                //     tempOM.GetID().preset_detail_id = preset_detail_id;
                //     break;
                // }
            }
        }
    }

    public void UpdateRoomPresetDetailId(JArray objects)
    {
        foreach (JObject updateInfo in objects)
        {
            string shop_object_id = updateInfo["shop_object_id"].ToString();
            int inventory_object_id = updateInfo["inventory_object_id"].ToObject<int>();
            int room_preset_detail_id = updateInfo["room_preset_detail_id"].ToObject<int>();

            foreach (GameObject i in _allActiveObjects)
            {
                ObjectManager tempOM = i.GetComponent<ObjectManager>();
                if (tempOM == null) continue;
                if (tempOM.GetIntactBool()) continue;

                if (tempOM.GetID().shop_object_id == shop_object_id && tempOM.GetID().room_preset_detail_id == 0)
                {
                    tempOM.GetID().inventory_object_id = inventory_object_id;
                    tempOM.GetID().room_preset_detail_id = room_preset_detail_id;
                    break;
                }

                // if (tempOM.GetID().inventory_object_id == inventory_object_id && string.IsNullOrEmpty(tempOM.GetID().room_preset_detail_id))
                // {
                //     tempOM.GetID().room_preset_detail_id = room_preset_detail_id;
                //     break;
                // }
            }
        }
    }

    //Camera
    public void CameraMovement()
    {
        if (_selectedObject != null)
            _cameraCentricObject.transform.position = _selectedObject.transform.position;

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touchOnePrevPos = touch2.position - touch2.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touch1.position - touch2.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            _camManager.VcamFramingVC.m_Lens.OrthographicSize += deltaMagnitudeDiff * _zoomSensitivity;
            _camManager.VcamFramingVC.m_Lens.OrthographicSize = Mathf.Max(_camManager.VcamFramingVC.m_Lens.OrthographicSize, 0.1f);

            _combinedTouchX = touch1.deltaPosition.x + touch2.deltaPosition.x;
            _combinedTouchY = touch1.deltaPosition.y + touch2.deltaPosition.y;

            _cameraCentricObject.transform.position -= (_mainCam.transform.right * _combinedTouchX + _mainCam.transform.up * _combinedTouchY) * _zoomSensitivity * _camManager.VcamFramingVC.m_Lens.OrthographicSize / 20f;

            Vector3 minBound = _groundCollider.bounds.min - Vector3.one * 5;
            Vector3 maxBound = _groundCollider.bounds.max + Vector3.one * 5 + new Vector3(0, 10, 0);

            float LimitX = Mathf.Clamp(_cameraCentricObject.transform.position.x, minBound.x, maxBound.x);
            float LimitY = Mathf.Clamp(_cameraCentricObject.transform.position.y, minBound.y, maxBound.y);
            float LimitZ = Mathf.Clamp(_cameraCentricObject.transform.position.z, minBound.z, maxBound.z);

            _cameraCentricObject.transform.position = new Vector3(LimitX, LimitY, LimitZ);
        }
    }

    bool IsPointerOverUI(Touch touch)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);

        eventData.position = new Vector2(touch.position.x, touch.position.y);

        List<RaycastResult> results = new List<RaycastResult>();
        if (!EventSystem.current) return false;
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    public void ExitEditMode()
    {
        if (_selectedObject != null)
        {
            //_selectedObject.GetComponent<Outline>().enabled = false;
            _selectedObjectManager._selectedObject = null;
            _selectedObjectManager.ObjectDeselectionTagChange();
            StartCoroutine(_selectedObjectManager.SetOutLine(false));
            _cameraCentricObject.transform.position = _selectedObject.transform.position;
            _selectedObjectManager = null;
            _selectedObject = null;
        }

        EditToolsActive(false, false);
        _camManager.VcamFramingVC.Follow = _cameraCentricObject.transform;
        _camManager.VcamFramingVC.LookAt = _cameraCentricObject.transform;
    }

    public void EditToolsActive(bool active, bool deleteActive = true)
    {
        if (_zTestBoundPlane != null)
        {
            _zTestBoundPlane.gameObject.SetActive(active);
            if (_selectedObject != null)
            {
                Vector3 sizeWithRotate = _addressableLoaded[_selectedObject.name].transform.rotation * _selectedObjectBox.size;
                _zTestBoundPlane.localScale = new Vector3(sizeWithRotate.x, 0.01f, sizeWithRotate.z);
                _zTestBoundPlane.eulerAngles = new Vector3(0f, _selectedObject.transform.eulerAngles.y, 0f);
                _zTestBoundPlane.position = new Vector3(_selectedObject.transform.position.x, _groundCollider.bounds.center.y + _groundCollider.bounds.size.y / 2f, _selectedObject.transform.position.z);
            }
        }
        if (_movementUI != null) _movementUI.gameObject.SetActive(active);

        if (!_isRnMode)
        {
            if (_rotationUI != null) _rotationUI.gameObject.SetActive(active);
            if (_deleteButton != null) _deleteButton.gameObject.SetActive(deleteActive);
        }

        // UpdateEditorInfo();
        // Debug.Log(_selectedObject.transform.eulerAngles.y > 180 ? _selectedObject.transform.eulerAngles.y - 360 : _selectedObject.transform.eulerAngles.y);
    }

    void UpdateEditorInfo()
    {
        JObject json = new JObject();
        json["cmd"] = "UpdateEditorInfo";

        JObject param = new JObject();
        param["name"] = "";
        bool IsEdit = false;
        // Debug.Log("Event Length : " + _previousHistory.Count);

        List<History> histories = _previousHistory.ToList();
        histories.Add(_currentHistory);

        foreach (History item in histories.ToArray())
        {
            if (item._historyCategory != HistoryCategory.Select && item._historyCategory != HistoryCategory.Create)
            {
                IsEdit = true;
                break;
            }
        }

        param["IsEdit"] = IsEdit;
        if (_selectedObject != null)
        {
            if (_movementUI != null) _movementUI.gameObject.SetActive(true);
            float rotationAngle = _selectedObject.transform.eulerAngles.y > 180 ? _selectedObject.transform.eulerAngles.y - 360 : _selectedObject.transform.eulerAngles.y; //0~360
            float rotationFloat = rotationAngle / 360;
            param["Rotation"] = rotationFloat;
            param["IsVisibleDelete"] = string.IsNullOrEmpty(_selectedObjectManager.GetID().shop_object_id) ? false : true;
            param["name"] = _selectedObject.name;
        }
        json["params"] = param;

        RNMessenger.SendJson(json);
    }

    //Edit Object
    public void ChangeSelectedObject(Transform target)
    {
        if (_selectMode == SelectMode.NonSelectable)
            return;

        if (_selectedObject != null)
            if (_selectedObject.transform == target)
                return;

        if (_selectedObject != null)
        {
            _selectedObjectManager = _selectedObject.GetComponent<ObjectManager>();
            _selectedObjectManager._overLapColliderList = _overlapColliderList;
            _selectedObjectManager._selectedObject = null;
            _selectedObjectManager.ObjectDeselectionTagChange();
            StartCoroutine(_selectedObjectManager.SetOutLine(false));
        }

        _selectedObject = target.gameObject;
        BoxCollider tempCollider = _selectedObject.gameObject.GetComponent<BoxCollider>();

        SetMeshBoundsParameters();

        //if (_selectedObject.transform.parent.tag == "ObjectSelectable")
        if (_selectedObject.transform.parent.CompareTag(Constants.ObjectSelectableTag))
            _overlapColliderList.Add(_selectedObject.transform.parent.GetComponent<BoxCollider>());

        if (_overlapColliderList.Contains(tempCollider))
            _overlapColliderList.Remove(tempCollider);

        _selectedObjectManager = target.gameObject.GetComponent<ObjectManager>();
        _overlapColliderList = _selectedObjectManager._overLapColliderList;
        _selectedObjectManager.ObjectSelectionTagChange();

        _selectedObjectManager._selectedObject = _selectedObject;
        StartCoroutine(_selectedObjectManager.SetOutLine(true));

        _canObjectMoving = _selectedObjectManager.GetMovable();
        _canObjectRotating = _selectedObjectManager.GetRotatable();

        _rotationUI.value = _selectedObject.transform.eulerAngles.y > 180 ? _selectedObject.transform.eulerAngles.y - 360 : _selectedObject.transform.eulerAngles.y;
        _movementUI.position = _mainCam.WorldToScreenPoint(_selectedObject.transform.position);

        if (!_isRnMode)
        {
            _movementUI.gameObject.SetActive(_canObjectMoving);
            _rotationUI.gameObject.SetActive(_canObjectRotating);
        }

        if (_zTestBoundPlane != null && _selectedObjectBox != null)
        {
            Vector3 sizeWithRotate = _addressableLoaded[_selectedObject.name].transform.rotation * _selectedObjectBox.size;
            _zTestBoundPlane.localScale = new Vector3(sizeWithRotate.x, 0.01f, sizeWithRotate.z);
            _zTestBoundPlane.rotation = _selectedObject.transform.rotation;
        }

        if (string.IsNullOrEmpty(_selectedObjectManager.GetID().shop_object_id)) EditToolsActive(true, true);
        else EditToolsActive(true);
    }

    public void MoveAssetByPoint()
    {
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition - _objectTouchOffset);
        RaycastHit tempHit;

        if (Physics.Raycast(ray, out tempHit, float.MaxValue, 1 << LayerMask.NameToLayer("Ground")))
        {
            if (_camManager.VcamFraming.FollowTarget != null && _selectedObject != null)
            {
                Vector3 originVec = _selectedObject.transform.position;
                Vector3 direction = new Vector3(tempHit.point.x, 0, tempHit.point.z) - new Vector3(_selectedObject.transform.position.x, 0f, _selectedObject.transform.position.z);
                Vector3 norm = direction.normalized / 2;
                Vector3 beApplied = direction.magnitude < norm.magnitude ? direction : norm;

                _selectedObject.transform.position = new Vector3(originVec.x + beApplied.x, AssetYPosition(), originVec.z + beApplied.z);
                SetIsMoving(true);
            }
            if (_selectedObject != null && _zTestBoundPlane && _zTestBoundPlane.gameObject.activeSelf && _selectedObjectBox != null)
                _zTestBoundPlane.position = new Vector3(_selectedObject.transform.position.x, tempHit.collider.bounds.center.y + tempHit.collider.bounds.size.y / 2f, _selectedObject.transform.position.z);
        }
    }

    public float AssetYPosition()
    {
        float groundPos = _groundCollider.transform.position.y + (_groundCollider.bounds.size.y / 2f + _groundCollider.bounds.center.y);

        if (_overlapColliderList != null || _overlapColliderList.Count > 0)
        {
            float tempMagY = _groundCollider != null ? groundPos : 0;

            foreach (Collider col in _overlapColliderList)
            {
                float tempSum = col.bounds.size.y / 2f + col.bounds.center.y;

                if (tempMagY < tempSum)
                {
                    tempMagY = tempSum;
                }
            }
            return tempMagY - 0.02f;
        }
        else
        {
            return _groundCollider != null ? groundPos : 0;
        }
    }

    public void SetMeshBoundsParameters()
    {
        _selectedObjectBox = _selectedObject.GetComponent<BoxCollider>();
    }

    public string DeleteObject()
    {
        if (_selectedObject == null || GetChildrenOverlap()) return null;
        string deleteObjectName = _selectedObject.name;
        SaveHistory(HistoryCategory.Remove, _selectedObjectManager);

        // Destroy(_selectedObject);
        // ExitEditMode();

        PoolingObject(_selectedObject);
        ExitEditMode();
        _overlapChildColliderList.Clear();
        _overlapColliderList.Clear();

        return deleteObjectName;
    }

    public int GetObjectCount(string objectName)
    {
        int objectCount = 0;
        foreach (GameObject activeObject in _allActiveObjects)
        {
            if (activeObject.name == objectName) objectCount++;
        }
        return objectCount;
    }

    public void SlideRotate(Slider slider)
    {
        if (!_clickRotateUI)
            return;
        _selectedObject.transform.position = new Vector3(_selectedObject.transform.position.x, AssetYPosition(), _selectedObject.transform.position.z);
        _selectedObject.transform.eulerAngles = new Vector3(0f, slider.value, 0f) + _addressableLoaded[_selectedObject.name].transform.eulerAngles;
        // Debug.Log("Slider: " + slider.value);
        // Debug.Log("Slider: " + _selectedObject.transform.eulerAngles.y);
        _zTestBoundPlane.eulerAngles = new Vector3(0f, slider.value, 0f);
        SetIsRotating(true);
    }

    public void ControlChange(Text text)
    {
        _editMode = _editMode == EditMode.Drag ? EditMode.Pointer : EditMode.Drag;
        text.text = _editMode == EditMode.Drag ? "Offset" : "Raycast";
    }

    public void SelectableChange(Text text)
    {
        _selectMode = _selectMode == SelectMode.Selectable ? SelectMode.NonSelectable : SelectMode.Selectable;
        text.text = _selectMode == SelectMode.Selectable ? "Selectable" : "NonSelectable";

        if (_selectMode == SelectMode.NonSelectable)
            ExitEditMode();
    }

    public void SetIsRotating(bool value)
    {
        _isObjectRotating = value;
        _camManager.EnableRotation = !value;
    }

    public void SetRotatingHistory()
    {
        if (_currentHistory._targetObject != null)
            if (_currentHistory._historyCategory == HistoryCategory.Create && _currentHistory._targetObject.gameObject == _selectedObject)
                SaveHistory(HistoryCategory.Move, _selectedObjectManager);

        SelectedObjectBoundCheck();
        _clickRotateUI = true;
    }

    public void SetRotatingUp()
    {
        SaveHistory(HistoryCategory.Rotate, _selectedObjectManager);
        _clickRotateUI = false;
    }

    public string Save()
    {
        List<ObjectData> objcets = new List<ObjectData>();

        foreach (GameObject i in _allActiveObjects)
        {
            ObjectManager tempOM = i.GetComponent<ObjectManager>();
            if (tempOM != null)
                objcets.Add(new ObjectData(i.name.Replace("(Clone)", ""), i.transform.position, i.transform.rotation, i.transform.lossyScale, tempOM.GetID().shop_object_id, tempOM.GetID().inventory_object_id, tempOM.GetID().preset_detail_id, tempOM.GetID().room_preset_detail_id));
            else
                objcets.Add(new ObjectData(i.name.Replace("(Clone)", ""), i.transform.position, i.transform.rotation, i.transform.lossyScale, null, 0, 0, 0));
        }

        Objects tempObjects = new Objects(objcets.ToArray());
        return JsonUtility.ToJson(tempObjects);
    }

    public void Load(string json)
    {
        Objects objects = JsonUtility.FromJson<Objects>(json.ToString());
        _initRoomData = objects;
        ResetRoom();

        _initObjects.Clear();

        foreach (GameObject i in _allActiveObjects)
        {
            // Debug.Log(i);
            ObjectManager tempOM = i.GetComponent<ObjectManager>();
            if (tempOM != null)
            {
                tempOM.SetIntactBool(true);
                _initObjects.Add(tempOM);
                tempOM.InitGetParent();
            }
        }
    }

    public void SaveHistory(HistoryCategory cate_selectedObjectry, ObjectManager target, bool resultMove = false)
    {
        _afterHistory.Clear();
        if (_currentHistory != null)
            _previousHistory.Push(_currentHistory);
        _currentHistory = new History(cate_selectedObjectry, target);

        UpdateHistoryCount();
        UpdateEditorInfo();
    }

    public void RevertHistory(bool revert)
    {
        if (revert)
        {
            if (_previousHistory.Count <= 0 && _currentHistory == null)
                return;

            if (_currentHistory != null)
                _afterHistory.Push(_currentHistory);

            SetHistory(revert);
            _currentHistory = _previousHistory.Count <= 0 ? null : _previousHistory.Pop();
        }
        else
        {
            if (_afterHistory.Count <= 0)
                return;

            if (_currentHistory != null)
                _previousHistory.Push(_currentHistory);

            _currentHistory = _afterHistory.Pop();
            SetHistory(revert);
        }

        UpdateHistoryCount();
        UpdateEditorInfo();
    }

    public void SetHistory(bool revert)
    {
        if (_currentHistory._historyCategory == HistoryCategory.Rotate || _currentHistory._historyCategory == HistoryCategory.Move)
        {
            if (_currentHistory._targetObject == null)
                return;

            History targetHistory = revert ? _previousHistory.Peek() : _currentHistory;

            _currentHistory._targetObject.transform.position = targetHistory._position;
            _currentHistory._targetObject.transform.rotation = targetHistory._rotation;

            _currentHistory._targetObject.transform.parent = targetHistory._parentObject == null ? _spaceParent : targetHistory._parentObject.transform;
            for (int i = 0; i < targetHistory._childObjects.Length; i++)
                targetHistory._childObjects[i].transform.parent = _currentHistory._targetObject.transform;

            _zTestBoundPlane.eulerAngles = _currentHistory._targetObject.transform.eulerAngles;
            _zTestBoundPlane.position = new Vector3(_currentHistory._targetObject.transform.position.x, _zTestBoundPlane.position.y, _currentHistory._targetObject.transform.position.z);
        }
        else if (_currentHistory._historyCategory == HistoryCategory.Remove || _currentHistory._historyCategory == HistoryCategory.Create)
        {
            if ((revert && _currentHistory._historyCategory == HistoryCategory.Create) || (!revert && _currentHistory._historyCategory == HistoryCategory.Remove))
            {
                // 오브젝트 생성 / 삭제 로그 출력
                Debug.Log("오브젝트 삭제");
                _currentHistory.UpdateTransform();
                PoolingObject(_currentHistory._targetObject.gameObject);

                if ((revert ? _previousHistory : _afterHistory).Count > 0)
                {
                    if ((revert ? _previousHistory : _afterHistory).Peek() != null)
                    {
                        if ((revert ? _previousHistory : _afterHistory).Peek()._targetObject != null)
                        {
                            if ((revert ? _previousHistory : _afterHistory).Peek()._historyCategory != HistoryCategory.Select)
                            {
                                ChangeSelectedObject((revert ? _previousHistory : _afterHistory).Peek()._targetObject.transform);
                                return;
                            }
                        }
                    }
                }

                ExitEditMode();
                _overlapColliderList.Clear();
                _overlapChildColliderList.Clear();
            }
            else if ((!revert && _currentHistory._historyCategory == HistoryCategory.Create) || (revert && _currentHistory._historyCategory == HistoryCategory.Remove))
            {
                // 오브젝트 생성 / 삭제 로그 출력
                Debug.Log("오브젝트 생성");
                ExitEditMode();
                GameObject target = AddObject(_currentHistory._objectName, _currentHistory._position, _currentHistory._rotation, _currentHistory._scale, null, 0, 0, 0);

                target.transform.position = _currentHistory._position;
                target.transform.rotation = _currentHistory._rotation;

                _zTestBoundPlane.eulerAngles = new Vector3(0f, target.transform.eulerAngles.y, 0f);
                _zTestBoundPlane.position = new Vector3(_currentHistory._position.x, _zTestBoundPlane.position.y, _currentHistory._position.z);

                int id = _currentHistory._targetObjectIdentify;
                ObjectManager tempOM = target.GetComponent<ObjectManager>();
                _currentHistory.RenewDestoryedObject(tempOM, id);
                target.transform.parent = _currentHistory._parentObject == null ? _spaceParent : _currentHistory._parentObject.transform;

                for (int i = 0; i < _currentHistory._childObjects.Length; i++)
                    _currentHistory._childObjects[i].transform.parent = target.transform;

                foreach (History i in _previousHistory)
                    i.RenewDestoryedObject(tempOM, id);

                foreach (History i in _afterHistory)
                    i.RenewDestoryedObject(tempOM, id);

                ChangeSelectedObject(target.transform);
            }
        }
        else if (_currentHistory._historyCategory == HistoryCategory.Select)
        {
            if (!revert)
                ChangeSelectedObject(_currentHistory._targetObject.transform);
            else if (_previousHistory.Count > 0 && _previousHistory.Peek()._targetObject != null)
                ChangeSelectedObject(_previousHistory.Peek()._targetObject.transform);
            else
                ExitEditMode();
        }
    }

    //Etc...
    public void SetIsMoving(bool value)
    {
        _isObjectMoving = value;
        _camManager.EnableRotation = !value;
    }

    public Objects SavePreset()
    {
        //ToDo
        return new Objects(null);
    }

    public void AddedObjectsLog()
    {
        Debug.Log(ReturnAddedObjects());
    }

    public void DeletedObjectsLog()
    {
        Debug.Log(ReturnDeletedObjects());
    }

    private void OnApplicationQuit()
    {
        foreach (GameObject i in _addressableLoaded.Values)
        {
            AddressableManager.ReleaseIns(i);
        }
    }

    public Transform GetRoom()
    {
        return _spaceParent;
    }

    public void Set_click_movementUI(bool value)
    {
        _click_movementUI = value;
    }

    public void HistoryExit()
    {
        if (_previousHistory.Count > 0)
            while (_previousHistory.Peek()._targetObject == _currentHistory._targetObject && (_previousHistory.Peek()._historyCategory == HistoryCategory.Move || _previousHistory.Peek()._historyCategory == HistoryCategory.Rotate))
                _previousHistory.Pop();

        if (_afterHistory.Count > 0)
            while (_afterHistory.Peek()._targetObject == _currentHistory._targetObject && (_afterHistory.Peek()._historyCategory == HistoryCategory.Move || _afterHistory.Peek()._historyCategory == HistoryCategory.Rotate))
                _afterHistory.Pop();

        _currentHistory = new History(HistoryCategory.Move, _currentHistory._targetObject);
        UpdateHistoryCount();
    }

    public void UpdateHistoryCount()
    {
        _historyCountText.text = _previousHistory.Count + " / " + (_previousHistory.Count + _afterHistory.Count - (_currentHistory == null ? 1 : 0));
    }

    public void DrawGrid(Collider targetCollider)
    {
        if (!_lineBox) return;

        BoxCollider targetBox = targetCollider.GetComponent<BoxCollider>();

        if (targetBox != null)
            targetBox.size = new Vector3((int)(Mathf.Round(targetBox.size.x) / 2 - 1) * 2, targetBox.size.y, (int)(Mathf.Round(targetBox.size.z) / 2 - 1) * 2);

        float zMin = targetCollider.bounds.center.z - targetCollider.bounds.size.z / 2f - 1;
        float zMax = targetCollider.bounds.center.z + targetCollider.bounds.size.z / 2f + 1;
        float xMin = targetCollider.bounds.center.x - targetCollider.bounds.size.x / 2f - 1;
        float xMax = targetCollider.bounds.center.x + targetCollider.bounds.size.x / 2f + 1;
        float yMax = targetCollider.bounds.center.y + targetCollider.bounds.size.y / 2f + 0.05f;

        for (float i = zMin + 1; i < zMax - 0.5f; i += 1)
        {
            LineRenderer tempLine;
            if (_gridStorage.Count > 0)
            {
                tempLine = _gridStorage.Dequeue();
                tempLine.gameObject.SetActive(true);
            }
            else
            {
                GameObject tempObject = new GameObject("Line");
                tempLine = tempObject.AddComponent<LineRenderer>();
                tempLine.material = _lineMaterial;
                tempLine.SetWidth(0.05f, 0.05f);
                tempObject.transform.parent = _lineBox;
            }

            tempLine.SetPosition(0, new Vector3(xMin + 1, yMax, i));
            tempLine.SetPosition(1, new Vector3(xMax - 1, yMax, i));

            _gridLine.Add(tempLine);
        }

        for (float i = xMin + 1; i < xMax - 0.5f; i += 1)
        {
            LineRenderer tempLine;
            if (_gridStorage.Count > 0)
            {
                tempLine = _gridStorage.Dequeue();
                tempLine.gameObject.SetActive(true);
            }
            else
            {
                GameObject tempObject = new GameObject("Line");
                tempLine = tempObject.AddComponent<LineRenderer>();
                tempLine.material = _lineMaterial;
                tempLine.SetWidth(0.05f, 0.05f);
                tempObject.transform.parent = _lineBox;
            }

            tempLine.SetPosition(0, new Vector3(i, yMax, zMin + 1));
            tempLine.SetPosition(1, new Vector3(i, yMax, zMax - 1));

            _gridLine.Add(tempLine);
        }
    }

    public void PoolingGrid()
    {
        if (!_lineBox) return;

        LineRenderer[] renderers = _lineBox.transform.GetComponentsInChildren<LineRenderer>();

        foreach (LineRenderer i in renderers)
        {
            i.gameObject.SetActive(false);
            _gridStorage.Enqueue(i);
            _gridLine.Remove(i);
        }

        _gridLine.Clear();
    }

    public bool GetChildrenOverlap()
    {
        if (_overlapChildColliderList.Count > 0)
            Debug.Log("Can not perform because child objects are colliding!");
        return _overlapChildColliderList.Count > 0;
    }

    public bool PreviousButtonEnable()
    {
        return _previousHistory.Count > 0;
    }

    public bool AfterButtonEnable()
    {
        return _afterHistory.Count > 0;
    }

    public void SelectedObjectBoundCheck()
    {
        Collider[] colls = _selectedObjectManager.GetOverlapColliders(_selectedObjectManager.GetCollider());
        List<Collider> childList = new List<Collider>(_selectedObject.GetComponentsInChildren<Collider>());

        foreach (Collider i in colls)
            if (!childList.Contains(i) && !_overlapColliderList.Contains(i))
                // if (i.tag == "ObjectSelectable" || i.tag == "ObjectInteractable")
                if (i.CompareTag(Constants.ObjectSelectableTag) || i.CompareTag(Constants.ObjectInteractableTag))
                    if (i.transform.position.y + i.bounds.size.y / 2f > _selectedObject.transform.position.y)
                        _overlapColliderList.Add(i);
    }

    public void UpdateRotation(float rotate)
    {
        float angle = rotate * 360;
        _selectedObject.transform.eulerAngles = new Vector3(0f, angle, 0f);
        SaveHistory(HistoryCategory.Rotate, _selectedObjectManager);
    }

    public bool IsSavable()
    {
        return _overlapChildColliderList.Count <= 0;
    }
}