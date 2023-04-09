using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System;
using Mingle;
using UnityEngine.AddressableAssets;
using System.Text.RegularExpressions;

public class AvatarRPC : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;
    [SerializeField] private PhotonView _photonView;
    [SerializeField] private PlayerActionManager _playerManager;

    [SerializeField] private GameObject obj_Prop_03_hand;
    [SerializeField] private GameObject obj_Prop_04_hand;

    [SerializeField] private GameObject prop_Actual;

    private bool firstLoad = false;

    private Coroutine _animatorLookAtCoroutine = null;
    private Coroutine _objInteractionCoroutine = null;

    public GameObject objToDestroy;
    public GameObject objToDestroyOBJINT;
    public AudioClip soundToDestroy;
    public string EmoticonEffectName;

    private string objcheck;
    public string OBJIntEnterCheck
    {
        get => objcheck;
        set
        {
            objcheck = value;
            _animator.SetBool("isIdle", false);

            if (value == "OBJ_Int_Prop_03")
            {
                obj_Prop_03_hand.SetActive(true);
                //! disable actual object
                prop_Actual.SetActive(false);
            }

            else if (value == "OBJ_Int_Prop_04")
            {
                obj_Prop_04_hand.SetActive(true);
                //! disable actual object
                prop_Actual.SetActive(false);
            }

            else
            {
                if (prop_Actual != null)
                {
                    prop_Actual.SetActive(true);
                    prop_Actual = null;
                }

                obj_Prop_03_hand.SetActive(false);
                obj_Prop_04_hand.SetActive(false);
            }

            _animator.Play(value, 0, 0);
        }
    }

    private void Start()
    {
        firstLoad = true;
    }

    #region Animation RPC
    //한 이모티콘에 애니메이션이 한개일 경우
    [PunRPC]
    private IEnumerator PlaySingleAnimation_RPC(int photonViewID, string animation_clip, int animation_index)
    {
        _animator.SetBool("isIdle", false);
        _agent.enabled = true;
        _agent.ResetPath();

        _playerManager.CurrentState = AvatarState.Animation;

        CancelAllCoroutine();

        // AddressableManager.AddressableInsLoad(animation_clip, Vector3.zero, Quaternion.identity, out obj);

        if (_playerManager.VFXObject == null)
        {
            var loadObject = Addressables.LoadAssetAsync<GameObject>(animation_clip);
            loadObject.WaitForCompletion();
            yield return loadObject;

            objToDestroy = loadObject.Result;
        }

        _animator.SetInteger("Emoticon", animation_index);
        _playerManager.VFXObject = Instantiate(objToDestroy, transform.position, transform.rotation);
        yield break;
    }

    //한 이모티콘에 애니메이션이 여러개일 경우
    [PunRPC]
    private IEnumerator PlayRandomAnimation_RPC(int photonViewID, string animation_clip, int random_index, string effectName, int animation_index)
    {
        _animator.SetBool("isIdle", false);
        _agent.enabled = true;
        _agent.ResetPath();
        // _animator.Play("Idle");

        string soundClipName = "";

        string animation_index_string = animation_index.ToString();

        if (animation_index < 10)
        {
            animation_index_string = "0" + animation_index_string;
        }

        string animation_clipNameOnly = Regex.Match(animation_clip, @"(?<=_)[^_]+$").ToString();

        if (animation_clipNameOnly != "")
        {
            soundClipName = "S" + animation_index_string + "_S_E_3D_" + animation_clipNameOnly;
        }

        else if (animation_clipNameOnly == "")
        {
            soundClipName = "S" + animation_index_string + "_S_E_3D_" + animation_clipNameOnly + "_0" + random_index;
        }

        CancelAllCoroutine();

        _playerManager.CurrentState = AvatarState.Animation;

        _animator.SetFloat("BlendThreshold", (float)random_index);

        if (effectName != string.Empty)
        {
            EmoticonEffectName = effectName;
            _animator.SetInteger("Emoticon", animation_index);
            if (_playerManager.VFXObject != null) Destroy(_playerManager.VFXObject);
            // if (_playerManager.VFXObject == null)
            // {
            var loadObject = Addressables.LoadAssetAsync<GameObject>(effectName);

            loadObject.WaitForCompletion();

            yield return loadObject;

            if (loadObject.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                objToDestroy = loadObject.Result;
                _playerManager.VFXObject = Instantiate(objToDestroy, transform.position, transform.rotation);
            }

            // var loadSound = Addressables.LoadAssetAsync<AudioClip>(soundClipName);
            // loadSound.WaitForCompletion();
            // yield return loadSound;
            // if (loadSound.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            // {
            //     soundToDestroy = loadSound.Result;
            // _playerManager.SfxSource.PlayOneShot(soundToDestroy);
            // }
            // }
        }

        // _playerManager.VFXObject = Instantiate(objToDestroy, transform.position, transform.rotation);
        yield break;
    }

    //아바타 뒤에서 탭 했을때 놀라는 애니메이션
    [PunRPC]
    private void SurpriseAvatar()
    {
        _playerManager.CurrentState = AvatarState.AvatarInteraction;
        CancelAllCoroutine();
        _animator.SetBool("isIdle", false);
        _animator.Play("C_MO_Touch05", 0, 0);
    }

    //이동 취소
    [PunRPC]
    private void CancelMove()
    {
        _agent.ResetPath();
        CancelAllCoroutine();
    }

    [PunRPC]
    private void CancelAvatarInteraction()
    {
        _playerManager.CurrentState = AvatarState.Idle;
    }

    //애니메이션 취소
    [PunRPC]
    private void CancelAnimation()
    {
        // if (objToDestroy != null) Destroy(objToDestroy);
        // if (objToDestroyOBJINT != null) Destroy(objToDestroyOBJINT);
        _animator.SetTrigger("IdleTrigger");
        _agent.enabled = true;
        _agent.ResetPath();
        CancelAllCoroutine();
        _animator.SetInteger("Emoticon", 0);
        // _animator.SetBool("isIdle", true);

        if (prop_Actual != null)
        {
            prop_Actual.SetActive(true);
            prop_Actual = null;
            obj_Prop_03_hand.SetActive(false);
            obj_Prop_04_hand.SetActive(false);
        }

        _playerManager.CurrentState = AvatarState.Idle;
        // _animator.Play("Idle");
    }

    //아바타 or 오브젝트 인터랙션 취소
    // [PunRPC]
    private void CancelAllCoroutine()
    {
        if (_animatorLookAtCoroutine != null)
        {
            StopCoroutine(_animatorLookAtCoroutine);
            _animatorLookAtCoroutine = null;
            _playerManager.AnimatorLookAtWeight = 0;
        }

        if (_objInteractionCoroutine != null)
        {
            StopCoroutine(_objInteractionCoroutine);
            _objInteractionCoroutine = null;
        }
    }

    //오브젝트 인터랙션 시작 (취소를 위해 코루틴을 변수에 저장)
    [PunRPC]
    private void StartObjInteractionCoroutine(Vector3 objPosition, Quaternion objRotation, Vector3 closestPoint, int viewID, string objectType, float objBlendThreshold, bool isAvatarOffRoot)
    {
        _objInteractionCoroutine = StartCoroutine(ObjectInteraction(objPosition, objRotation, closestPoint, viewID, objectType, objBlendThreshold, isAvatarOffRoot));
    }

    public void ObjInteractionOnConnect(Vector3 objPosition, Quaternion objRotation, Vector3 closestPoint, int viewID, string objectType, bool isAvatarOffRoot)
    {
        _objInteractionCoroutine = StartCoroutine(OnConnectObjectInteraction(objPosition, objRotation, closestPoint, viewID, objectType, isAvatarOffRoot));
    }

    private IEnumerator OnConnectObjectInteraction(Vector3 objPosition, Quaternion objRotation, Vector3 closestPoint, int viewID, string objectType, bool isAvatarOffRoot)
    {
        if (_animator == null) Start();

        _agent.enabled = false;

        if (isAvatarOffRoot)
        {
            Transform tappedObject = null;
            Transform tappedObjectChild = null;
            try
            {
                tappedObject = PhotonView.Find(viewID).gameObject.transform;
                if (tappedObject.childCount > 0) tappedObjectChild = tappedObject.GetChild(0);
            }
            catch (System.Exception)
            {
                Debug.Log("no child obj");
                throw;
            }

            if (objectType != "OBJ_Int_Furn_05" && objectType != "OBJ_Int_Hobby_04")
            {
                Vector3 parentObjectToAvatar = transform.position - tappedObject.position;
                Vector3 parentObjectToNicknamePosition = tappedObjectChild.position - tappedObject.position;
                float angleToRotate = Vector3.Angle(parentObjectToNicknamePosition, parentObjectToAvatar);

                var sign = Mathf.Sign(parentObjectToNicknamePosition.x * parentObjectToAvatar.z - parentObjectToNicknamePosition.z * parentObjectToAvatar.x);

                Vector3 axisToRotate = sign > 0 ? Vector3.down : Vector3.up;

                tappedObjectChild.RotateAround(tappedObject.position, axisToRotate, angleToRotate);

                transform.position = tappedObjectChild.position;
                Vector3 directionVector = (objPosition - transform.position).normalized;
                Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                transform.rotation = directionQuaternion;
            }
            else
            {
                transform.position = tappedObjectChild.position;
                Vector3 directionVector = (tappedObject.position - tappedObjectChild.position).normalized;
                Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                transform.rotation = directionQuaternion;
            }
        }

        else
        {
            if (objectType != "OBJ_Int_Prop_03" && objectType != "OBJ_Int_Prop_04")
            {
                transform.position = objPosition;
                transform.rotation = objRotation;
            }

            else
            {
                Vector3 directionVector = (objPosition - transform.position).normalized;
                Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                transform.rotation = directionQuaternion;
                transform.position = objPosition;
            }
        }

        if (objectType == "OBJ_Int_Hobby_01")
        {
            if (_playerManager.VFXInteractionObject != null)
            {
                AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                Destroy(_playerManager.VFXInteractionObject);
            }
            AddressableManager.AddressableInsLoad("OBJ_Int_01", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
        }

        else if (objectType == "OBJ_Int_Hobby_02")
        {
            if (_playerManager.VFXInteractionObject != null)
            {
                AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                Destroy(_playerManager.VFXInteractionObject);
            }
            AddressableManager.AddressableInsLoad("OBJ_Int_02", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
        }

        else if (objectType == "OBJ_Int_Hobby_03")
        {
            if (_playerManager.VFXInteractionObject != null)
            {
                AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                Destroy(_playerManager.VFXInteractionObject);
            }
            AddressableManager.AddressableInsLoad("OBJ_Int_03", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
        }

        //! -- future build when all the effects are added
        /*
        if (_playerManager.VFXInteractionObject == null)
        {
            Match match = Regex.Match(objectType, @"(\w+)_(\w+)_(\w+)_(\w+)");
            string objEffectName = match.Groups[1].Value + "_" + match.Groups[2].Value + "_" + match.Groups[4].Value;
            var loadObject = Addressables.LoadAssetAsync<GameObject>(objEffectName);
            loadObject.WaitForCompletion();
            yield return loadObject;

            objToDestroyOBJINT = loadObject.Result;
        }

        if (_playerManager.VFXInteractionObject != null) Destroy(_playerManager.VFXInteractionObject);
            _playerManager.VFXInteractionObject = Instantiate(objToDestroyOBJINT, transform.position, transform.rotation);
        */
        if (objectType == "OBJ_Int_Prop_03" || objectType == "OBJ_Int_Prop_04")
        {
            prop_Actual = PhotonView.Find(viewID).gameObject;
            prop_Actual.SetActive(false);
        }

        OBJIntEnterCheck = objectType;
        yield break;
    }

    private IEnumerator ObjectInteraction(Vector3 objPosition, Quaternion objRotation, Vector3 cloestPoint, int viewID, string objectType, float objBlendThreshold, bool isAvatarOffRoot)
    {
        Match match = Regex.Match(objectType, @"^([^_]+)_([^_]+)_([^_]+)_(\d+)$");
        int interaction = 0;
        if (match.Success)
        {
            switch (match.Groups[3].Value)
            {
                case "Hobby":
                    interaction = 10 + int.Parse(match.Groups[4].Value);
                    break;

                case "Furn":
                    interaction = 20 + int.Parse(match.Groups[4].Value);
                    break;

                case "Prop":
                    interaction = 30 + int.Parse(match.Groups[4].Value);
                    break;
            }
        }

        while (!firstLoad) yield return new WaitForEndOfFrame();

        if (_animator == null) Start();

        // if (_playerManager.CurrentState == PlayerActionManager.AvatarState.Moving) _agent.ResetPath();
        _animator.SetBool("isIdle", false);
        // _playerManager.CurrentState = AvatarState.ObjectInteraction;
        _agent.enabled = true;
        // _agent.ResetPath();

        // string moveMotion = "";

        // CancelAllCoroutine();
        if (_animatorLookAtCoroutine != null)
        {
            StopCoroutine(_animatorLookAtCoroutine);
            _animatorLookAtCoroutine = null;
            _playerManager.AnimatorLookAtWeight = 0;
        }

        _animator.SetFloat("InteractionBlend", objBlendThreshold);

        if ((this.transform.position - objPosition).magnitude > 4)
        {

            _animator.Play("Run", 0, 0);
            // moveMotion = "Run";
            _agent.speed = 3.5f;
        }

        else if ((this.transform.position - objPosition).magnitude <= 4 && (cloestPoint - this.transform.position).magnitude > 1.75f)
        {
            // _animator.Play("Walk", 0, 0);
            _animator.SetTrigger("WalkTrigger");
            // moveMotion = "Walk";
            _agent.speed = 2.5f;
        }

        _agent.SetDestination(cloestPoint);
        _playerManager.CurrentState = AvatarState.Moving;

        // _animator.Play(moveMotion);

        while (true)
        {
            if ((cloestPoint - this.transform.position).magnitude > 1.75f)
            {
                // if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(moveMotion)) _animator.Play(moveMotion, 0, 0);
                // if (_photonView.IsMine) yield return new WaitForEndOfFrame();
                // else yield return null;
                yield return null;
            }
            else
            {
                BoxCollider targetObject = PhotonView.Find(viewID).gameObject.GetComponent<BoxCollider>();
                if (Physics.OverlapBox(targetObject.bounds.center, targetObject.bounds.extents, objRotation, 1 << 3).Length >= 1)
                {
                    Debug.Log("more than 1 avatar");
                    yield break;
                }

                // if (isAvatarOffRoot)
                // {
                //     //! change nickname position
                //     nickNameOffRootPosition = _playerManager.NickName.transform.position;
                // }

                // foreach (var player in PhotonNetwork.PlayerListOthers)
                // {
                //     if (player.CustomProperties.ContainsKey(timeCheck) && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(timeCheck))
                //     {
                //         if ((float)player.CustomProperties[timeCheck] < (float)PhotonNetwork.LocalPlayer.CustomProperties[timeCheck])
                //         {
                //             break;
                //         }

                //         else continue;
                //     }
                // }

                _playerManager.CurrentState = AvatarState.ObjectInteraction;
                _agent.enabled = false;

                if (isAvatarOffRoot)
                {
                    Transform tappedObject = null;
                    Transform tappedObjectChild = null;
                    try
                    {
                        tappedObject = PhotonView.Find(viewID).gameObject.transform;
                        if (tappedObject.childCount > 0) tappedObjectChild = tappedObject.GetChild(0);
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("no child obj");
                        throw;
                    }
                    if (objectType != "OBJ_Int_Furn_05" && objectType != "OBJ_Int_Hobby_04")
                    {
                        Vector3 parentObjectToAvatar = transform.position - tappedObject.position;
                        Vector3 parentObjectToNicknamePosition = tappedObjectChild.position - tappedObject.position;
                        float angleToRotate = Vector3.Angle(parentObjectToNicknamePosition, parentObjectToAvatar);

                        var sign = Mathf.Sign(parentObjectToNicknamePosition.x * parentObjectToAvatar.z - parentObjectToNicknamePosition.z * parentObjectToAvatar.x);

                        Vector3 axisToRotate = sign > 0 ? Vector3.down : Vector3.up;

                        tappedObjectChild.RotateAround(tappedObject.position, axisToRotate, angleToRotate);

                        transform.position = tappedObjectChild.position;
                        Vector3 directionVector = (objPosition - transform.position).normalized;
                        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                        transform.rotation = directionQuaternion;
                    }
                    else
                    {
                        transform.position = tappedObjectChild.position;
                        Vector3 directionVector = (tappedObject.position - tappedObjectChild.position).normalized;
                        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                        transform.rotation = directionQuaternion;
                    }
                }

                else
                {
                    if (objectType != "OBJ_Int_Prop_03" && objectType != "OBJ_Int_Prop_04")
                    {
                        transform.position = objPosition;
                        transform.rotation = objRotation;
                    }

                    else
                    {
                        Vector3 directionVector = (objPosition - transform.position).normalized;
                        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
                        transform.rotation = directionQuaternion;
                        transform.position = objPosition;
                    }
                }

                if (objectType == "OBJ_Int_Hobby_01")
                {
                    // var loadObject = Addressables.LoadAssetAsync<GameObject>("OBJ_Int_01");
                    // GameObject OBJEffect;
                    // AddressableManager.AddressableLoad("OBJ_Int_03", out OBJEffect);
                    // loadObject.WaitForCompletion();
                    // yield return OBJEffect;
                    // objToDestroyOBJINT = OBJEffect;
                    if (_playerManager.VFXInteractionObject != null)
                    {
                        AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                        Destroy(_playerManager.VFXInteractionObject);
                    }
                    // _playerManager.VFXInteractionObject = Instantiate(objToDestroyOBJINT, transform.position, transform.rotation);
                    AddressableManager.AddressableInsLoad("OBJ_Int_01", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
                }

                else if (objectType == "OBJ_Int_Hobby_02")
                {
                    // var loadObject = Addressables.LoadAssetAsync<GameObject>("OBJ_Int_01");
                    // GameObject OBJEffect;
                    // AddressableManager.AddressableLoad("OBJ_Int_03", out OBJEffect);
                    // loadObject.WaitForCompletion();
                    // yield return OBJEffect;
                    // objToDestroyOBJINT = OBJEffect;
                    if (_playerManager.VFXInteractionObject != null)
                    {
                        AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                        Destroy(_playerManager.VFXInteractionObject);
                    }
                    // _playerManager.VFXInteractionObject = Instantiate(objToDestroyOBJINT, transform.position, transform.rotation);
                    AddressableManager.AddressableInsLoad("OBJ_Int_02", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
                }

                else if (objectType == "OBJ_Int_Hobby_03")
                {
                    // var loadObject = Addressables.LoadAssetAsync<GameObject>("OBJ_Int_01");
                    // GameObject OBJEffect;
                    // AddressableManager.AddressableLoad("OBJ_Int_03", out OBJEffect);
                    // loadObject.WaitForCompletion();
                    // yield return OBJEffect;
                    // objToDestroyOBJINT = OBJEffect;
                    if (_playerManager.VFXInteractionObject != null)
                    {
                        AddressableManager.ReleaseIns(_playerManager.VFXInteractionObject);
                        Destroy(_playerManager.VFXInteractionObject);
                    }
                    // _playerManager.VFXInteractionObject = Instantiate(objToDestroyOBJINT, transform.position, transform.rotation);
                    AddressableManager.AddressableInsLoad("OBJ_Int_03", transform.position, transform.rotation, out _playerManager.VFXInteractionObject);
                }

                //! -- future build when all the effects are added
                /*
                if (_playerManager.VFXInteractionObject == null)
                {
                    Match match = Regex.Match(objectType, @"(\w+)_(\w+)_(\w+)_(\w+)");
                    string objEffectName = match.Groups[1].Value + "_" + match.Groups[2].Value + "_" + match.Groups[4].Value;
                    var loadObject = Addressables.LoadAssetAsync<GameObject>(objEffectName);
                    loadObject.WaitForCompletion();
                    yield return loadObject;

                    objToDestroyOBJINT = loadObject.Result;
                }

                if (_playerManager.VFXInteractionObject != null) Destroy(_playerManager.VFXInteractionObject);
                    _playerManager.VFXInteractionObject = Instantiate(objToDestroyOBJINT, transform.position, transform.rotation);
                */
                if (objectType == "OBJ_Int_Prop_03" || objectType == "OBJ_Int_Prop_04")
                {
                    prop_Actual = PhotonView.Find(viewID).gameObject;
                    prop_Actual.SetActive(false);
                }

                // OBJIntEnterCheck = objectType;
                _animator.SetInteger("Interaction", interaction);
                yield break;
            }
        }
    }
    #endregion

    #region Avatar Interaction
    //아바타 인터랙션
    [PunRPC]
    private void StartAvatarInteractionCoroutine(Vector3 IKPosition)
    {
        _playerManager.CurrentState = AvatarState.AvatarInteraction;
        _animatorLookAtCoroutine = StartCoroutine(PhotonAvatarLookAtPosition(IKPosition));
    }

    private IEnumerator PhotonAvatarLookAtPosition(Vector3 IKPosition)
    {
        CancelAllCoroutine();
        bool reverse = false;

        Vector3 dir = Vector3.zero;

        _playerManager.AvatarLookAtPosition = IKPosition;

        while (!reverse)
        {
            _playerManager.AnimatorLookAtWeight = Mathf.Lerp(_playerManager.AnimatorLookAtWeight, 1, 5.5f * Time.deltaTime);

            if (_playerManager.AnimatorLookAtWeight >= 0.95f)
            {
                _playerManager.AnimatorLookAtWeight = 1;
                yield return new WaitForSeconds(0.5f);
                reverse = true;
            }

            if (_photonView.IsMine)
            {
                yield return new WaitForEndOfFrame();
            }

            else if (!_photonView.IsMine)
            {
                yield return null;
            }
        }

        while (reverse)
        {
            _playerManager.AnimatorLookAtWeight = Mathf.Lerp(_playerManager.AnimatorLookAtWeight, 0, 4.5f * Time.deltaTime);

            // if (_photonView.IsMine)
            // {
            //     dir = (new Vector3(PhotonIKPosition.x, transform.position.y, PhotonIKPosition.z) - transform.position).normalized;
            // }

            // else if (_photonView.IsMine)
            // {
            //     dir = (new Vector3(LocalIKPosition.x, transform.position.y, LocalIKPosition.z) - transform.position).normalized;
            // }

            dir = (new Vector3(_playerManager.AvatarLookAtPosition.x, transform.position.y, _playerManager.AvatarLookAtPosition.z) - transform.position).normalized;

            Quaternion directionQuaternion = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, directionQuaternion, 4.5f * Time.deltaTime);

            if (_playerManager.AnimatorLookAtWeight <= 0.05f)
            {
                _playerManager.AnimatorLookAtWeight = 0;
                transform.rotation = directionQuaternion;
                _playerManager.CurrentState = AvatarState.Idle;
                yield break;
            }

            if (_photonView.IsMine)
            {
                yield return new WaitForEndOfFrame();
            }

            else if (!_photonView.IsMine)
            {
                yield return null;
            }
        }
    }
    #endregion

    #region Avatar Movement RPC
    //싱글탭 : Nav Mesh Agent 이동
    [PunRPC]
    private void SingleTapMove(Vector3 tapPosition)
    {
        _playerManager.CurrentState = AvatarState.Moving;
        _animator.SetBool("isIdle", false);

        Vector3 directionVector = (tapPosition - transform.position).normalized;
        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
        transform.rotation = directionQuaternion;

        if ((transform.position - tapPosition).magnitude > 4)
        {
            _animator.SetTrigger("RunTrigger");
            _agent.speed = 3.5f;
        }

        else if ((transform.position - tapPosition).magnitude <= 4)
        {
            _animator.SetTrigger("WalkTrigger");
            _agent.speed = 2.5f;
        }

        if (_agent.enabled == false) _agent.enabled = true;
        _agent.SetDestination(tapPosition);
    }

    [PunRPC]
    //오브젝트 인터랙션 시 이동 함수
    private void OBJIntMove(Vector3 tapPosition)
    {
        if ((transform.position - tapPosition).magnitude < 1) return;

        _playerManager.CurrentState = AvatarState.Moving;
        _animator.SetBool("isIdle", false);

        Vector3 directionVector = (tapPosition - transform.position).normalized;
        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
        transform.rotation = directionQuaternion;

        if ((transform.position - tapPosition).magnitude > 4)
        {
            _animator.SetTrigger("RunTrigger");
            _agent.speed = 3.5f;
        }

        else if ((transform.position - tapPosition).magnitude <= 4)
        {
            _animator.SetTrigger("WalkTrigger");
            _agent.speed = 2.5f;
        }

        if (_agent.enabled == false) _agent.enabled = true;
        _agent.SetDestination(tapPosition);
    }

    //더블 탭 : 목표 위치로 점프해서 이동
    [PunRPC]
    private IEnumerator DoubleTapJump(Vector3 hitPoint)
    {
        _playerManager.CurrentState = AvatarState.Jumping;
        _animator.SetBool("isIdle", false);

        _agent.ResetPath();
        _animator.SetBool("isJumping", true);
        _agent.enabled = false;

        Vector3 jumpingInitialPosition = transform.position;

        float jumpHeight = Mathf.Clamp(Vector3.Distance(jumpingInitialPosition, hitPoint) * 0.25f, 2, 10);

        float interpolant = 0;

        Vector3 directionVector = (hitPoint - transform.position).normalized;
        Quaternion directionQuaternion = Quaternion.LookRotation(directionVector);
        transform.rotation = directionQuaternion;

        while (true)
        {
            interpolant += Constants.IsDebug ? 0.025f : 0.05f;

            //fps 60
            // interpolant += 0.025f;

            //fps 30
            // interpolant += 0.05f;

            transform.position = JumpingTrajectory(jumpingInitialPosition, hitPoint, jumpHeight, interpolant);

            Vector2 tranformPositionVector2 = new Vector2(transform.position.x, transform.position.z);
            Vector2 hitPointVector2 = new Vector2(hitPoint.x, hitPoint.z);

            if ((tranformPositionVector2 - hitPointVector2).magnitude < 0.3f)
            {
                _agent.enabled = true;
                _playerManager.CurrentState = AvatarState.Idle;

                _animator.SetBool("isJumping", false);
                _animator.SetBool("isIdle", true);
                yield return new WaitForSeconds(0.4f);
                // _animator.SetTrigger("IdleTrigger");
                _playerManager.CurrentState = AvatarState.Idle;
                yield break;
            }

            if (_photonView.IsMine)
            {
                yield return new WaitForEndOfFrame();
            }

            else if (!_photonView.IsMine)
            {
                yield return null;
            }
        }
    }

    //점프할 때 아바타 높이 계산 (현재 시작과 끝점의 Y축이 다르면 고장남, 특히 위에서 아래로갈 때)
    private Vector3 JumpingTrajectory(Vector3 start, Vector3 end, float height, float t)
    {
        Func<float, float> f = x => -4 * height * x * x + 4 * height * x;
        var mid = Vector3.Lerp(start, end, t);
        return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);

        // if (Mathf.Abs(start.y - end.y) < 0.5f)
        // {
        //     Func<float, float> f = x => -4 * height * x * x + 4 * height * x;
        //     var mid = Vector3.Lerp(start, end, t);
        //     return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
        // }

        // else
        // {
        //     float highestPoint = start.y > end.y ? start.y : end.y;
        //     Func<float, float> f = x => -4 * (height + highestPoint) * x * x + 4 * (height + highestPoint) * x;
        //     var mid = Vector3.Lerp(start, end, t);
        //     return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
        // }
    }
    #endregion
}