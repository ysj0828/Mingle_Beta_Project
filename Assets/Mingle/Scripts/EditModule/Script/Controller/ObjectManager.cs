using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Mingle;

[Serializable]
public struct IDWrapper
{
    public IdentityData[] identities;

    public IDWrapper(IdentityData[] identities)
    {
        this.identities = identities;
    }
}

[Serializable]
public class IdentityData
{
    public string name;
    public string shop_object_id;
    public int inventory_object_id = 0;
    public int preset_detail_id = 0;
    public int room_preset_detail_id = 0;

    public IdentityData(string name, string shop_object_id, int inventory_object_id = 0, int preset_detail_id = 0, int room_preset_detail_id = 0)
    {
        this.name = name;
        this.shop_object_id = shop_object_id;
        this.inventory_object_id = inventory_object_id;
        this.preset_detail_id = preset_detail_id;
        this.room_preset_detail_id = room_preset_detail_id;
    }
}

public class ObjectManager : MonoBehaviour
{
    [SerializeField] bool _movable = true;
    [SerializeField] bool _rotatable = true;

    bool _isIntactObject = false;
    string _originTag;

    GameObject _selectedObjectValue;
    Outline _outLine;
    BoxCollider _mCollider;

    IdentityData _identityData;

    int _thisObjectNum = _allObjectCount;
    static int _allObjectCount = 0;

    [HideInInspector] public bool _initBoxCheck = false;
    [HideInInspector] public EditController _editController;

    public GameObject _selectedObject
    {
        get
        {
            return _selectedObjectValue;
        }
        set
        {
            _selectedObjectValue = value;
        }
    }

    private void Awake()
    {
        UpdateObjectID();
        _originTag = tag;
    }

    private void Start()
    {
        _mCollider = transform.GetComponent<BoxCollider>();
        _outLine = GetComponent<Outline>();

        if (_outLine == null)
            _outLine = gameObject.AddComponent<Outline>();

        if (gameObject.layer == LayerMask.NameToLayer("Ground"))
            _movable = _rotatable = false;

        StartCoroutine(CollisionCheckRoutine());
        StartCoroutine(CollisionCheckRoutineAsChild());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (this.gameObject == _editController._selectedObject)
        {
            if (TriggerTagCheck(other) && !_editController._overlapColliderList.Contains(other) && other.transform.parent != transform)
            {
                List<Collider> colls = new List<Collider>();
                Collider[] children = transform.GetComponentsInChildren<BoxCollider>();

                foreach (BoxCollider i in children)
                {
                    Collider[] tempColls = GetOverlapColliders(i);
                    foreach (Collider j in tempColls)
                        colls.Add(j);
                }

                if (!colls.Contains(other))
                    return;

                float tempSum = other.bounds.center.y + other.bounds.size.y / 2f;
                Transform parent = other.transform;

                while (true)
                {
                    colls = new List<Collider>(GetOverlapColliders(_mCollider, tempSum, 0.3f));
                    Collider[] childList = transform.GetComponentsInChildren<Collider>();
                    bool existCollision = false;

                    foreach (Collider i in childList)
                        if (colls.Contains(i))
                            colls.Remove(i);

                    foreach (Collider i in colls)
                    {
                        if (TriggerTagCheck(i) && i != _mCollider)
                        {
                            if (other.transform.position.y < i.transform.position.y)
                            {
                                if (i.transform.parent == transform)
                                    break;
                                existCollision = true;
                                other = i;
                            }
                        }
                    }

                    if (existCollision)
                    {
                        tempSum = other.bounds.center.y + other.bounds.size.y / 2f;
                        parent = other.transform;
                    }
                    else
                        break;
                }

                if (tempSum > gameObject.transform.position.y && !_editController._overlapColliderList.Contains(other) && other.transform != _editController.GetRoom())
                {
                    _editController._overlapColliderList.Add(other);
                    gameObject.transform.position = new Vector3(gameObject.transform.position.x, tempSum - 0.02f, gameObject.transform.position.z);
                    SetParent(parent);
                }
            }
        }
        else if (TriggerTagCheck(other))
        {
            Transform parent = transform.parent;
            Transform[] children = transform.GetComponentsInChildren<Transform>();
            bool isSeletedChild = false;

            foreach (Transform i in children)
                if (i == other.transform)
                    return;

            while (parent != null)
            {
                if (parent == other.transform)
                {
                    return;
                }
                // if (parent.tag == "ObjectSelected")
                if (parent.CompareTag(Constants.ObjectSelectedTag))
                {
                    isSeletedChild = true;
                    break;
                }
                parent = parent.parent;
            }

            if (!isSeletedChild)
                return;

            StartCoroutine(SetOutLine(true, new Color(1, 0, 0)));
            if (!_editController._overlapChildColliderList.Contains(_mCollider))
                _editController._overlapChildColliderList.Add(_mCollider);
        }

        _initBoxCheck = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (_editController._selectedObject == gameObject)
        {
            //List<Collider> colls = new List<Collider>(GetOverlapColliders(_mCollider));

            //if (!colls.Contains(other) && _editController._overlapColliderList.Contains(other))
            //{
            //    transform.parent = _editController.GetRoom();
            //    _editController._overlapColliderList.Remove(other);
            //}
        }
        else if (TriggerTagCheck(other))
        {
            CollisionCheckAsChild();
        }
        // else if (other.tag == "ObjectSelected")
        else if (other.CompareTag(Constants.ObjectSelectedTag))
        {
            if (other.transform == transform.parent)
                if (_editController._overlapColliderList.Contains(_mCollider))
                {
                    _editController._overlapColliderList.Remove(_mCollider);
                    transform.parent = _editController.GetRoom();
                }
        }
    }

    public void ObjectSelectionTagChange()
    {
        gameObject.tag = "ObjectSelected";
    }

    public void ObjectDeselectionTagChange()
    {
        gameObject.tag = _originTag;
    }

    IEnumerator CollisionCheckRoutine()
    {
        yield return new WaitForSeconds(0.05f);

        CollisionCheck();

        StartCoroutine(CollisionCheckRoutine());
    }

    IEnumerator CollisionCheckRoutineAsChild()
    {
        yield return new WaitForSeconds(0.5f);

        if (_editController._overlapChildColliderList.Count > 0)
        {
            if (_editController._overlapChildColliderList.Contains(_mCollider))
            {
                CollisionCheckAsChild();
            }
        }

        StartCoroutine(CollisionCheckRoutineAsChild());
    }

    public List<Collider> _overLapColliderList = new List<Collider>();
    public List<Collider> _overLapChildColliderList = new List<Collider>();

    public void CollisionCheck()
    {
        if (_editController._selectedObject == gameObject)
        {
            List<Collider> colls = new List<Collider>(GetOverlapColliders(_mCollider, 0.3f));
            Collider[] children = GetComponentsInChildren<Collider>();

            foreach (Collider i in children)
                if (colls.Contains(i))
                    colls.Remove(i);

            for (int i = 0; i < _editController._overlapColliderList.Count; i++)
            {
                bool isExist = false;

                foreach (Collider j in colls)
                {
                    // if (j.tag == "ObjectNonSelectable")
                    if (j.CompareTag(Constants.ObjectNonSelectableTag))
                        if (GetSelectableParent(j.transform) == _editController._overlapColliderList[i].transform)
                            isExist = true;
                    if (j == _editController._overlapColliderList[i])
                        isExist = true;
                }

                if (!isExist)
                {
                    Collider targetCollider = _editController._overlapColliderList[i];
                    _editController._overlapColliderList.Remove(targetCollider);

                    if ((targetCollider.CompareTag(Constants.ObjectSelectableTag) || targetCollider.CompareTag(Constants.ObjectInteractableTag)))
                        transform.parent = _editController.GetRoom();
                    else
                        _editController._overlapColliderList.Add(SetParent(targetCollider.transform).GetComponent<BoxCollider>());
                }
            }
        }
    }

    public void CollisionCheckAsChild()
    {
        bool existObject = false;
        Collider[] tempColls = Physics.OverlapBox(_mCollider.bounds.center, _mCollider.size * 0.5f, transform.rotation);
        List<Transform> parents = new List<Transform>(transform.GetComponentsInParent<Transform>());
        List<Transform> children = new List<Transform>(transform.GetComponentsInChildren<Transform>());

        foreach (Collider i in tempColls)
        {
            if (TriggerTagCheck(i))
            {
                if (!parents.Contains(i.transform) && !children.Contains(i.transform) && i.gameObject != gameObject)
                {
                    Transform overlapParent = i.transform;
                    Transform thisParent = transform;

                    while (overlapParent.parent != _editController.GetRoom() && overlapParent.parent != null)
                        overlapParent = overlapParent.parent;

                    while (thisParent.parent != _editController.GetRoom() && thisParent.parent != null)
                        thisParent = thisParent.parent;

                    if (overlapParent != thisParent)
                        existObject = true;
                }
            }
        }

        if (!existObject)
        {
            StartCoroutine(SetOutLine(false));
            if (_editController._overlapChildColliderList.Contains(_mCollider))
                _editController._overlapChildColliderList.Remove(_mCollider);
        }
    }

    public void InitGetParent()
    {
        Collider[] colls = GetOverlapColliders(_mCollider);
        if (colls.Length <= 0)
        {
            transform.parent = _editController.GetRoom();
            return;
        }

        Collider parent = null;
        float parentOffset = float.MaxValue;

        for (int i = 0; i < colls.Length; i++)
        {
            // if (colls[i].tag == "ObjectSelectable" || colls[i].tag == "ObjectInteractable")
            if (colls[i].CompareTag(Constants.ObjectSelectableTag) || colls[i].CompareTag(Constants.ObjectInteractableTag))
            {
                if (colls[i].bounds.center.y < _mCollider.bounds.center.y)
                {
                    float yOffset = MathF.Abs((colls[i].bounds.center.y + colls[i].bounds.size.y / 2) - (_mCollider.bounds.center.y - _mCollider.bounds.size.y / 2));
                    if (yOffset < parentOffset)
                    {
                        parent = colls[i];
                        parentOffset = yOffset;
                    }
                }
            }
        }

        if (parent == null)
            transform.parent = _editController.GetRoom();
        else
            SetParent(parent.transform);
    }

    public Collider[] GetOverlapColliders(BoxCollider target, float yOffset = 0.3f)
    {
        Vector3 boxPos = target.bounds.center - new Vector3(0f, yOffset / 1.9f, 0f);
        Vector3 boxSize = target.size - new Vector3(0.5f * target.size.x, yOffset, 0.5f * target.size.z);

        if ((target.transform.rotation * boxSize).y <= 0)
        {
            boxPos = target.bounds.center - new Vector3(0f, target.size.y / 4f, 0f);
            boxSize = target.size - target.transform.rotation * new Vector3(0f, target.size.y / 2f, 0f);
        }

        return Physics.OverlapBox(boxPos, boxSize, target.transform.rotation);
    }

    public Collider[] GetOverlapColliders(BoxCollider target, float yPos, float yOffset = 0.3f)
    {
        Vector3 boxPos = new Vector3(target.bounds.center.x, yPos, target.bounds.center.z) - new Vector3(0f, yOffset / 1.9f, 0f);
        Vector3 boxSize = target.size - new Vector3(0.5f * target.size.x, yOffset, 0.5f * target.size.z);

        if ((target.transform.rotation * boxSize).y <= 0)
        {
            boxPos = new Vector3(target.bounds.center.x, yPos, target.bounds.center.z) - new Vector3(0f, target.size.y / 4f, 0f);
            boxSize = target.size - target.transform.rotation * new Vector3(0f, target.size.y / 2f, 0f);
        }

        return Physics.OverlapBox(boxPos, boxSize, target.transform.rotation);
    }

    public bool TriggerTagCheck(Collider target)
    {
        // return target.tag == "ObjectSelectable" || target.tag == "ObjectInteractable" || target.tag == "ObjectNonSelectable";
        return target.CompareTag(Constants.ObjectSelectableTag) || target.CompareTag(Constants.ObjectInteractableTag) || target.CompareTag(Constants.ObjectNonSelectableTag);
    }

    public Transform SetParent(Transform parent)
    {
        Transform tempParent = parent;

        // while (tempParent.tag != "ObjectSelectable" && tempParent.tag != "ObjectInteractable" && tempParent != _editController.GetRoom() && tempParent != null)
        while (!tempParent.CompareTag(Constants.ObjectSelectableTag) && !tempParent.CompareTag(Constants.ObjectInteractableTag) && tempParent != _editController.GetRoom() && tempParent != null)
            tempParent = tempParent.parent;

        transform.parent = tempParent;
        return tempParent;
    }

    public Transform GetSelectableParent(Transform target)
    {
        Transform tempParent = target;

        // while (tempParent.tag != "ObjectSelectable" && tempParent.tag != "ObjectInteractable" && tempParent != _editController.GetRoom() && tempParent != null)
        while (!tempParent.CompareTag(Constants.ObjectSelectableTag) && !tempParent.CompareTag(Constants.ObjectInteractableTag) && tempParent != _editController.GetRoom() && tempParent != null)
            tempParent = tempParent.parent;

        return tempParent;
    }

    public IdentityData GetID()
    {
        return _identityData;
    }

    public void SetID(IdentityData value)
    {
        _identityData = value;
    }

    public bool GetMovable()
    {
        return _movable;
    }

    public bool GetRotatable()
    {
        return _rotatable;
    }

    public void SetIntactBool(bool value)
    {
        _isIntactObject = value;
    }

    public bool GetIntactBool()
    {
        return _isIntactObject;
    }

    public BoxCollider GetCollider()
    {
        return _mCollider;
    }

    public IEnumerator SetOutLine(bool value, Color color)
    {
        yield return new WaitUntil(() => _outLine != null);

        _outLine.OutlineColor = color;
        _outLine.enabled = value;
    }

    public IEnumerator SetOutLine(bool value)
    {
        yield return new WaitUntil(() => _outLine != null);

        _outLine.OutlineColor = new Color(0, 190f / 255f, 1f);
        _outLine.enabled = value;
    }

    public void UpdateObjectID()
    {
        _thisObjectNum = _allObjectCount++;
    }

    public int GetObjectID()
    {
        return _thisObjectNum;
    }
}
