using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;
using Random = System.Random;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using TMPro;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Mingle
{
    public enum AvatarState
    {
        Idle,
        Moving,
        Jumping,
        Animation,
        ObjectInteraction,
        AvatarInteraction
    }

    public class PlayerActionManager : MonoBehaviourPunCallbacks//, IPunObservable
    {
        public static GameObject LocalPlayerInstance;

        /// <summary>
        /// 아바타 현재상태
        /// </summary>
        /// <remarks>
        /// 상태 종류 : Idle, Moving, Jumping, Animation, Object Interaction, Avatar Interaction
        /// </remarks>
        public AvatarState CurrentState
        {
            get => _currentState;
            set
            {
                _previousState = _currentState;
                _currentState = value;
                if (_photonView.IsMine) _cameraManager.CurrentState = value;

                switch (value)
                {
                    case AvatarState.Idle:
                        _animator.SetFloat("IdleBlend", UnityEngine.Random.Range(0, 2));
                        _animator.SetTrigger("IdleTrigger");
                        DestroyEffect();
                        NickNameObject.transform.localPosition = _nickNameOriginalLocalPosition;
                        // _animator.SetBool("isIdle", true);
                        // _animator.ResetTrigger("IdleTrigger");
                        break;

                    case AvatarState.Moving:
                        _animator.ResetTrigger("IdleTrigger");
                        break;

                    case AvatarState.Animation:
                        _animator.SetBool("isIdle", false);
                        _animator.ResetTrigger("IdleTrigger");
                        break;

                    case AvatarState.ObjectInteraction:
                        _animator.ResetTrigger("IdleTrigger");
                        break;

                    case AvatarState.AvatarInteraction:
                        _animator.ResetTrigger("IdleTrigger");
                        break;
                }
            }
        }

        // public SMBInheritTest CurrentStateSMB = null;

        private AvatarState _currentState = AvatarState.Idle;
        private AvatarState _previousState = AvatarState.Idle;

        public bool AllowIdleTransition = false;

        private string _hashtableKey;
        private string _currentInteractionObject;

        [HideInInspector] public DateTime emoticonStartTime;
        [SerializeField] private Animator _animator;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private PhotonView _photonView;
        public AudioSource SfxSource;
        public GameObject VFXObject;
        public GameObject VFXInteractionObject;

        private CameraManager _cameraManager;
        private RoomManager _roomManager;

        public GameObject NickNameObject;
        private string _nickName;
        private string _userID;

        private Vector3 _nickNameOriginalLocalPosition = new Vector3(0, -0.5f, 0);

        public Vector3 AvatarLookAtPosition
        {
            get => _avatarLookAtPosition;
            set
            {
                _avatarLookAtPosition = value;
            }
        }
        private Vector3 _avatarLookAtPosition;
        public float AnimatorLookAtWeight;

        private float _tapTime;
        private int _tappedGroundCounter;

        private CustomManager _customManager = null;
        //private PhotonManager _photonManager = null;
        private Player player;
        private MinglePlayer _minglePlayer;

        private Camera _mainCam;

        private void Awake()
        {
            _customManager = GetComponent<CustomManager>();
            SfxSource = gameObject.AddComponent<AudioSource>();
            //_photonManager = GetComponent<PhotonManager>();

            NickNameObject = transform.GetChild(transform.childCount - 1).gameObject;

            // GameManager.Instance.NickName = "";
            string[] nickNameSplit = _photonView.Owner.NickName.Split(Constants.Spliter);
            _nickName = nickNameSplit[0];
            if (nickNameSplit.Length >= 2) _userID = nickNameSplit[1];

            if (_photonView.IsMine)
            {
                PlayerActionManager.LocalPlayerInstance = this.gameObject;
                _roomManager = FindObjectOfType<RoomManager>();
                _roomManager._currentPlayerActionManager = this;

                if (_customManager)
                {
                    CharacterCustomUpdate(GameManager.Instance.Infomation.CharacterInfo);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                var roomkey = PhotonNetwork.CurrentRoom.CustomProperties.Keys.ToList();
                var roomvalue = PhotonNetwork.CurrentRoom.CustomProperties.Values.ToList();

                if (roomkey.Contains("RoomInfo"))
                {
                    var index = roomkey.IndexOf("RoomInfo");
                    roomkey.RemoveAt(index);
                    roomvalue.RemoveAt(index);
                }

                if (roomkey.Contains("curScn"))
                {
                    var index = roomkey.IndexOf("curScn");
                    roomkey.RemoveAt(index);
                    roomvalue.RemoveAt(index);
                }

                Debug.LogFormat("room keys : " + $"[{string.Join(", ", roomkey)}]");
                Debug.LogFormat("room values : " + $"[{string.Join(", ", roomvalue)}]");
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.touchCount > 5)
            {

                if (!_photonView.IsMine) return;

                foreach (var avatar in PhotonNetwork.PlayerList)
                {
                    var key = avatar.CustomProperties.Keys.ToList();
                    var value = avatar.CustomProperties.Values.ToList();

                    // Debug.LogFormat("room keys : " + $"[{string.Join(", ", key)}]");
                    // Debug.LogFormat("room values : " + $"[{string.Join(", ", value)}]");
                    Debug.LogFormat($"player {avatar.ActorNumber} keys : " + $"[{string.Join(", ", key)}]");
                    Debug.LogFormat($"player {avatar.ActorNumber} values : " + $"[{string.Join(", ", value)}]");
                }
                // var roomkey = PhotonNetwork.CurrentRoom.CustomProperties.Keys.ToList();
                // var roomvalue = PhotonNetwork.CurrentRoom.CustomProperties.Values.ToList();

                var playerkey = PhotonNetwork.LocalPlayer.CustomProperties.Keys.ToList();
                var playervalue = PhotonNetwork.LocalPlayer.CustomProperties.Values.ToList();

                // roomkey.RemoveAt(0);
                // roomkey.RemoveAt(1);
                // roomvalue.RemoveAt(0);
                // roomvalue.RemoveAt(1);

                // Debug.LogFormat("room keys : " + $"[{string.Join(", ", roomkey)}]");
                // Debug.LogFormat("room values : " + $"[{string.Join(", ", roomvalue)}]");
                Debug.LogFormat("player keys : " + $"[{string.Join(", ", playerkey)}]");
                Debug.LogFormat("player values : " + $"[{string.Join(", ", playervalue)}]");
            }

            NickNameObject.transform.LookAt(_mainCam.transform.position);
            NickNameObject.transform.Rotate(0, 180, 0);

            if (NickNameObject.GetComponent<TextMeshPro>().text != _nickName)
            {
                NickNameObject.GetComponent<TextMeshPro>().text = _nickName;
            }

            if (Time.time > _tapTime + 0.5f && _tappedGroundCounter != 0)
            {
                _tappedGroundCounter = 0;
            }
        }

        private void Start()
        {
            _mainCam = Camera.main;

            _photonView.RPC("ActivePlayer", RpcTarget.Others);
            // SubscriptEvents();
            StartCoroutine(SyncPlayerInitialPosition());

            //방 입장시 인터랙션 오브젝트 이름에 "(Clone)"이 있으면 삭제
            foreach (GameObject IntObj in GameObject.FindGameObjectsWithTag("ObjectInteractable"))
            {
                string newName = Regex.Replace(IntObj.name, @"(Clone)", "");
                newName = newName.TrimEnd('(', ')');
                string _key = newName + IntObj.GetComponent<PhotonView>().ViewID.ToString();

                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(_key))
                {
                    Hashtable updateHashtable = new Hashtable();
                    updateHashtable[_key] = false;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);
                }
            }

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.Keys.Count > 0)
                {
                    //!//!
                    foreach (GameObject avatar in GameObject.FindGameObjectsWithTag("PlayerAvatar"))
                    {
                        if (avatar.GetComponent<PhotonView>().ControllerActorNr != player.ActorNumber) continue;
                        else
                        {
                            string input = (string)player.CustomProperties.Keys.Last();

                            string[] parts = Regex.Split(input, @"_");
                            string last = parts[parts.Length - 1];
                            int viewID = int.Parse(Regex.Replace(last, @"[a-zA-Z]+", ""));

                            Random random = new Random();

                            float randomBlend = random.Next(0, int.Parse(parts[parts.Length - 2].TrimStart('a')));

                            GameObject int_object = PhotonView.Find(viewID).gameObject;

                            string newName = Regex.Replace(int_object.name, @"(Clone)", "");
                            newName = newName.TrimEnd('(', ')');

                            Match match = Regex.Match(newName, @"([A-Za-z]+_[A-Za-z0-9_]+)_([A-Za-z0-9]+)_([A-Za-z0-9]+)");

                            string prefabName = match.Groups[1].Value.TrimEnd('a', 'b'); //OBJ_Int_Props_03

                            Vector3 closestPoint = int_object.GetComponent<BoxCollider>().ClosestPoint(avatar.transform.position);
                            closestPoint = new Vector3(closestPoint.x, avatar.transform.position.y, closestPoint.z);

                            bool isAvatarOffRoot = Regex.Replace(parts[parts.Length - 1], @"\d+", "") == "T" ? true : false;

                            avatar.GetComponent<AvatarRPC>().ObjInteractionOnConnect(int_object.transform.position, int_object.transform.rotation, closestPoint, viewID, prefabName, isAvatarOffRoot);
                        }
                    }
                }
            }
        }

        [PunRPC]
        private void ActivePlayer()
        {
            CharacterCustomUpdate2(GameManager.Instance.Infomation.CharacterInfo);
        }

        private void OnDestroy()
        {
            if (_photonView.IsMine)
            {
                //_photonManager.PlayerEnter -= OnPlayerEnter;
                _cameraManager.TappedAvatar -= TapAvatar_Observer;
                _cameraManager.TappedGround -= TapGround_Observer;
                _cameraManager.TappedObject -= TapObject_Observer_Coroutine;
                // _cameraManager.TappedObject -= TapObject_Observer;
                _cameraManager.TappedNull -= TapNull_Observer;
                RNManager.Instance.OnAnimationUpdate -= OnUpdateAnimationEvent;
                RNManager.Instance.OnChatRoomEvent -= OnChatRoomEvent;
            }

            DestroyEffect();
        }

        // public override void OnJoinedRoom()
        // {
        //     Debug.Log("OnJoinedRoom");
        // }

        //다른 플레이어 입장
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            player = newPlayer;
            StartCoroutine(SyncPlayerInitialPosition());
        }

        //다른 플레이어 퇴장
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // base.OnPlayerLeftRoom(otherPlayer);
            if (otherPlayer.CustomProperties.Keys.Count > 0)
            {
                Hashtable updateHashtable = new Hashtable();
                foreach (var key in otherPlayer.CustomProperties.Keys)
                {
                    updateHashtable[key] = false;
                }
                PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);
            }
        }

        private void CharacterCustomUpdate(JObject json)
        {
            if (_photonView.IsMine)
            {
                _photonView.RPC("SyncCustom", RpcTarget.All, json.ToString());
            }
        }

        private void CharacterCustomUpdate2(JObject json)
        {
            if (_photonView.IsMine)
            {
                _photonView.RPC("SyncCustom", player, json.ToString());
            }
        }

        [PunRPC]
        private void SyncCustom(string cmd, PhotonMessageInfo info)
        {
            JObject json = new JObject();
            JObject pram = JObject.Parse(cmd);
            json["params"] = pram;
            try
            {
                _customManager.CustomSyncFromRPC(json);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }
        }

        /// <summary>
        ///   이벤트 등록
        /// </summary>
        public void SubscriptEvents()
        {
            //Event subscription
            if (_photonView.IsMine)
            {
                _cameraManager = Camera.main.transform.GetComponent<CameraManager>();
                _cameraManager.TappedAvatar += TapAvatar_Observer;
                _cameraManager.TappedGround += TapGround_Observer;
                _cameraManager.TappedObject += TapObject_Observer_Coroutine;
                // _cameraManager.TappedObject += TapObject_Observer;
                _cameraManager.TappedNull += TapNull_Observer;
                RNManager.Instance.OnAnimationUpdate += OnUpdateAnimationEvent;
                RNManager.Instance.OnChatRoomEvent += OnChatRoomEvent;
            }
        }

        /// <summary>
        ///   아바타에 있는 Photon Transform View 컴포넌트 비활성화
        /// </summary>
        /// <remarks>
        ///   Transform View는 지금은 초기 위치 싱크에만 사용
        /// </remarks>
        private IEnumerator SyncPlayerInitialPosition()
        {
            GameObject[] listOfPlayers = GameObject.FindGameObjectsWithTag("PlayerAvatar");
            yield return new WaitForSeconds(0.1f);

            while (listOfPlayers.Length != PhotonNetwork.CurrentRoom.PlayerCount)
            {
                listOfPlayers = CheckForPlayers(listOfPlayers);
                yield return new WaitForSeconds(0.05f);
            }

            foreach (GameObject player in listOfPlayers)
            {
                player.GetComponent<PhotonTransformView>().enabled = false;
            }

            yield break;
        }

        /// <summary>
        ///   방에 있는 아바타 수와 포톤 서버에서 인식한 플레이어 수 체크
        /// </summary>
        private GameObject[] CheckForPlayers(GameObject[] listOfPlayers)
        {
            GameObject[] checkForPlayerCount = null;
            // Debug.LogFormat("num player : {0}, PNetwork count : {1}", listOfPlayers.Length, PhotonNetwork.CurrentRoom.PlayerCount);
            if (listOfPlayers.Length != PhotonNetwork.CurrentRoom.PlayerCount)
            {
                checkForPlayerCount = GameObject.FindGameObjectsWithTag("PlayerAvatar");
            }

            if (checkForPlayerCount.Length == PhotonNetwork.CurrentRoom.PlayerCount)
            {
                return checkForPlayerCount;
            }

            else
            {
                return GameObject.FindGameObjectsWithTag("PlayerAvatar");
            }
        }

        #region Observer Functions
        /// <summary>
        ///   허공에 탭했을 때 이벤트
        /// </summary>
        private void TapNull_Observer()
        {
            if (CurrentState == AvatarState.Jumping) return;
            CurrentState = AvatarState.Idle;

            foreach (var key in PhotonNetwork.LocalPlayer.CustomProperties.Keys)
            {
                Hashtable hashTable = new Hashtable();
                hashTable[key] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(hashTable);
            }

            // PhotonNetwork.LocalPlayer.CustomProperties.Clear();
            PhotonNetwork.SetPlayerCustomProperties(null);

            _photonView.RPC("CancelAnimation", RpcTarget.All);
        }

        private void OnUpdateAnimationEvent(JObject json)
        {
            try
            {
                Util.Log("PlayerActionManager", json["cmd"].ToString());
                // if (CurrentState != AvatarState.Animation) StartCoroutine(json["cmd"].ToString(), json);
                StartCoroutine(json["cmd"].ToString(), json);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }
        }

        /// <summary>
        ///   RN에서 이모티콘 애니메이션 재생하는 json받았을 때
        /// </summary>
        /// <remarks>
        ///    받은 json을 정규식으로 이름/애니메이션 개수/애니메이션에 연동된 이펙트 index로 나누기
        /// </remarks>
        private IEnumerator UpdateAnimation(JObject json)
        {
            emoticonStartTime = DateTime.Now.AddSeconds(1);
            JObject param = json["params"].ToObject<JObject>();

            string input = param["Animation"].ToString();
            int animation_index = int.Parse(Regex.Match(input, @"[0-9]{2}").ToString());

            if (CurrentState == AvatarState.ObjectInteraction) yield break;

            CurrentState = AvatarState.Animation;

            DestroyEffect();

            input = input.Substring(4);
            //input : C_E_Happy_05_0101010201

            string animationClipName;
            string effectName = "";

            if (String.IsNullOrWhiteSpace(input))
            {
                yield break;
            }

            if (!input.Any(char.IsDigit))
            {
                //input = C_E_Happy 일 경우에만 해당 (단일 애니메이션 & 단일 이펙트/사운드)
                animationClipName = effectName = input;

                try
                {
                    _photonView.RPC("PlaySingleAnimation_RPC", RpcTarget.All, _photonView.ViewID, animationClipName, animation_index);
                    // RNMessenger.SendResult("Success");
                }
                catch (System.Exception)
                {
                    //! Error
                    // RNMessenger.SendResult("Fail");
                    throw;
                }

                yield break;
            }

            else
            {
                string prefix = Regex.Match(input, @"^.*?(?=[0-9])").ToString(); //C_E_Happy_

                string digits = Regex.Replace(input, prefix, string.Empty); //05_0101010201

                int animationMaxIndex = int.Parse(Regex.Match(digits, @"^[0-9]{2}").ToString()); //int.Parse(05) = 5 
                Random rng = new Random();
                int randomIndex = rng.Next(0, animationMaxIndex);

                animationClipName = prefix.TrimEnd('_'); //C_E_Happy

                if (digits.Length >= 3)
                {
                    //(여러 애니메이션 & 여러 이펙트/사운드)
                    string listElement = Regex.Match(digits, @"[^_]*$").ToString(); //0101010201

                    StringBuilder sb = new StringBuilder();

                    List<int> effectsIndexList = new List<int>();

                    for (int i = 0; i < listElement.Length; i++)
                    {
                        if (i % 2 == 0 && i != 0)
                        {
                            sb.Clear();
                        }

                        sb.Append(listElement[i]);

                        if (sb.Length == 2)
                        {
                            effectsIndexList.Add(int.Parse(sb.ToString())); // List<int> [1, 1, 1, 2, 1] 
                        }
                    }

                    if (randomIndex < effectsIndexList.Count)
                    {
                        int effectIndex = rng.Next(0, effectsIndexList.Count);

                        // if (effectsIndexList[randomIndex] == 0)
                        if (effectsIndexList[effectIndex] == 0)
                        {
                            effectName = string.Empty;
                        }

                        else
                        {
                            // effectName = prefix + "0" + effectsIndexList[randomIndex]; //C_E_Happy_0x
                            effectName = prefix + "0" + effectsIndexList[effectIndex]; //C_E_Happy_0x
                        }
                    }

                    else if (randomIndex >= effectsIndexList.Count)
                    {
                        if (effectsIndexList[randomIndex] == 0)
                        {
                            effectName = string.Empty;
                        }

                        else
                        {
                            effectName = prefix + "0" + effectsIndexList[randomIndex]; //C_E_Happy_0x
                        }
                    }
                }

                else
                {
                    //input = C_E_Happy_05 때만 해당 (여러 애니메이션 & 단일 이펙트/사운드)
                    effectName = prefix.TrimEnd('_'); //C_E_Happy
                }

                try
                {
                    _photonView.RPC("PlayRandomAnimation_RPC", RpcTarget.All, _photonView.ViewID, animationClipName, randomIndex, effectName, animation_index);
                    // RNMessenger.SendResult("Success");
                }
                catch (System.Exception)
                {
                    // RNMessenger.SendResult("Fail");
                    throw;
                }
            }
        }

        /// <summary>
        ///   아바타 탭 했을때 이벤트
        /// </summary>
        private void TapAvatar_Observer(bool isMine, int avatarTapCount, Transform hitTransform)
        {
            if (CurrentState == AvatarState.Moving)
            {
                _photonView.RPC("CancelAnimation", RpcTarget.All);
            }
            if (CurrentState == AvatarState.Jumping || CurrentState == AvatarState.ObjectInteraction || CurrentState == AvatarState.Animation || CurrentState == AvatarState.AvatarInteraction) return;
            // CurrentState = AvatarState.AvatarInteraction;
            if (isMine)
            {
                if (avatarTapCount == 1)
                {
                    float dotProduct = Vector3.Dot(hitTransform.forward, _mainCam.transform.forward);
                    if (dotProduct >= -1 && dotProduct <= 0.4f)
                    {
                        _photonView.RPC("StartAvatarInteractionCoroutine", RpcTarget.All, _mainCam.transform.position);
                    }

                    else
                    {
                        _photonView.RPC("SurpriseAvatar", RpcTarget.All);
                    }
                }

                else
                {
                    //제자리 점프
                }
            }

            else
            {
                _photonView.RPC("StartAvatarInteractionCoroutine", RpcTarget.All, hitTransform.position + new Vector3(0, 1.35f, 0));
            }
        }

        /// <summary>
        ///   바닥 탭했을 때 이벤트
        /// </summary>
        private void TapGround_Observer(Vector3 tapPosition)
        {
            if (CurrentState == AvatarState.Jumping)
            {
                return;
            }

            if (PhotonNetwork.LocalPlayer.CustomProperties.Keys.Count > 0)
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.Keys.Contains(PhotonNetwork.LocalPlayer.CustomProperties.Keys.ToList()[0].ToString()))
                {
                    Hashtable updateHashtable = new Hashtable();
                    updateHashtable[PhotonNetwork.LocalPlayer.CustomProperties.Keys.ToList()[0].ToString()] = false;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);
                }
            }

            // PhotonNetwork.LocalPlayer.CustomProperties.Clear();
            PhotonNetwork.SetPlayerCustomProperties(null);

            _tapTime = Time.time;
            _tappedGroundCounter++;
            if (_cameraManager.IsCameraFirstPerson)
            {
                _cameraManager.FirstPersonDynamicCamera.Priority = 0;
                _cameraManager.FirstPersonStationaryCamera.Priority = 20;
            }
            if (CurrentState == AvatarState.ObjectInteraction)
            {
                _tappedGroundCounter--;
                Hashtable updateHashtable = new Hashtable();
                updateHashtable[_hashtableKey] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);

                // PhotonNetwork.LocalPlayer.CustomProperties.Clear();

                // CurrentState = AvatarState.Idle;
                _photonView.RPC("CancelAnimation", RpcTarget.All);
            }

            else if (CurrentState == AvatarState.Animation || CurrentState == AvatarState.AvatarInteraction)
            {
                // CurrentState = AvatarState.Idle;
                _photonView.RPC("CancelAnimation", RpcTarget.All);
            }

            else if (CurrentState == AvatarState.Idle || CurrentState == AvatarState.Moving)
            {
                if (_tappedGroundCounter == 1)
                {
                    // CurrentState = AvatarState.Moving;
                    _photonView.RPC("SingleTapMove", RpcTarget.All, tapPosition);
                }

                else if (_tappedGroundCounter >= 2)
                {
                    CurrentState = AvatarState.Jumping;
                    _photonView.RPC("DoubleTapJump", RpcTarget.All, tapPosition);
                }
            }

            if (_tappedGroundCounter >= 2) _tappedGroundCounter = 0;

            // Debug.LogFormat("updated keys : " + $"[{string.Join(", ", PhotonNetwork.CurrentRoom.CustomProperties.Keys.ToArray())}]");
            // Debug.LogFormat("updated values : " + $"[{string.Join(", ", PhotonNetwork.CurrentRoom.CustomProperties.Values.ToArray())}]");
        }

        /// <summary>
        ///   인터랙션 오브젝트 탭 했을 때 이벤트
        /// </summary>
        /// <remarks>
        ///   OBJ_Int_"종류"_"넘버링"_"T/F"
        ///   <para>종류 : 가구, 취미, etc </para>
        ///   <para>넘버링 : 가구 프리팹 넘버링 </para>
        ///   <para>T/F : 애니메이션이 Root에서 벗어난 애니메이션인지 </para>
        /// </remarks>
        private void TapObject_Observer_Coroutine(Vector3 hitPosition, Quaternion hitRotation, Vector3 closestPoint, string hitName, int viewID)
        {
            StartCoroutine(TapObject_Observer(hitPosition, hitRotation, closestPoint, hitName, viewID));
        }

        private IEnumerator TapObject_Observer(Vector3 hitPosition, Quaternion hitRotation, Vector3 closestPoint, string hitName, int viewID)
        {
            if (_cameraManager.IsCameraFirstPerson)
            {
                _cameraManager.FirstPersonStationaryCamera.Priority = 0;
                _cameraManager.FirstPersonDynamicCamera.Priority = 20;
            }

            if (CurrentState == AvatarState.Moving)
            {
                _agent.ResetPath();
                CurrentState = AvatarState.Idle;
            }

            BoxCollider targetObjectCollider = PhotonView.Find(viewID).gameObject.GetComponent<BoxCollider>();
            Collider[] overlappingColliders = Physics.OverlapBox(targetObjectCollider.bounds.center, targetObjectCollider.bounds.extents, hitRotation, 1 << 3);
            bool myAvatarOverlapped = false;
            foreach (var col in overlappingColliders)
            {
                PhotonView view = null;
                if (col.gameObject.TryGetComponent<PhotonView>(out view))
                {
                    if (view.IsMine)
                    {
                        myAvatarOverlapped = true;
                        break;
                    }
                }
                else continue;
            }

            string newName = Regex.Replace(hitName, @"(Clone)", "");
            newName = newName.TrimEnd('(', ')');
            _hashtableKey = newName + viewID.ToString();

            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(_hashtableKey) && (bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] == true)
            {
                // Debug.LogError("already in use");
                yield break;
            }

            Match match = Regex.Match(newName, @"([A-Za-z]+_[A-Za-z0-9_]+)_([A-Za-z0-9]+)_([A-Za-z0-9]+)");

            string prefabName = match.Groups[1].Value.TrimEnd('a', 'b'); //OBJ_Int_Props_03

            int animationCount = int.Parse(Regex.Replace(match.Groups[2].Value, @"^[a-zA-Z]", string.Empty)); //a04 -> 04 -> (int)4
            bool avatarOffRoot = match.Groups[3].Value == "T" ? true : false; //"T" -> true, "F" -> false

            GameObject viewIDObject = PhotonView.Find(viewID).gameObject;

            Random random = new Random();

            float randomBlend = random.Next(0, animationCount);

            if (!myAvatarOverlapped && (bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] == true)
            {
                Hashtable objHashtable = new Hashtable();
                objHashtable.Add(_hashtableKey, false);
                PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
                while ((bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] != false) yield return new WaitForEndOfFrame();
            }

            if (CurrentState == AvatarState.ObjectInteraction)
            {
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(_hashtableKey) && (bool)PhotonNetwork.LocalPlayer.CustomProperties[_hashtableKey] == true)
                {
                    yield break;
                }

                else if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(_hashtableKey))
                {
                    //! tapped another object
                    Hashtable objToRemove = new Hashtable();
                    objToRemove.Add(PhotonNetwork.LocalPlayer.CustomProperties.Keys.First(), false);
                    PhotonNetwork.CurrentRoom.SetCustomProperties(objToRemove);
                    // PhotonNetwork.LocalPlayer.CustomProperties.Clear();
                    PhotonNetwork.SetPlayerCustomProperties(null);
                    while (PhotonNetwork.LocalPlayer.CustomProperties.Count != 0) yield return new WaitForEndOfFrame();

                    Hashtable objHashtable = new Hashtable();
                    objHashtable.Add(_hashtableKey, true);
                    PhotonNetwork.LocalPlayer.SetCustomProperties(objHashtable);
                    PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);

                    while ((bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] != true) yield return new WaitForEndOfFrame();

                    _photonView.RPC("StartObjInteractionCoroutine", RpcTarget.All, hitPosition, hitRotation, closestPoint, viewID, prefabName, randomBlend, avatarOffRoot);
                    yield break;
                }
            }

            else if (CurrentState == AvatarState.Moving || CurrentState == AvatarState.Idle)
            {
                // _photonView.RPC("CancelMove", RpcTarget.All);
                if ((bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] == false)
                {
                    Hashtable objHashtable = new Hashtable();
                    objHashtable.Add(_hashtableKey, true);
                    PhotonNetwork.LocalPlayer.SetCustomProperties(objHashtable);
                    PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
                    while ((bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] != true) yield return new WaitForEndOfFrame();

                    _photonView.RPC("StartObjInteractionCoroutine", RpcTarget.All, hitPosition, hitRotation, closestPoint, viewID, prefabName, randomBlend, avatarOffRoot);
                }

                else if ((bool)PhotonNetwork.CurrentRoom.CustomProperties[_hashtableKey] == true)
                {
                    _photonView.RPC("OBJIntMove", RpcTarget.All, hitPosition);
                }
                yield break;
            }

            else if (CurrentState == AvatarState.AvatarInteraction || CurrentState == AvatarState.Animation)
            {
                _photonView.RPC("CancelAnimation", RpcTarget.All);
                yield break;
            }

            else if (CurrentState == AvatarState.Jumping)
            {
                yield break;
            }
        }
        #endregion

        #region Avatar Interaction
        /// <summary>
        ///   아바타 탭 이벤트 : 아바타의 고개 방향 변경
        ///   <para>Animator 사용</para>
        /// </summary>
        private void OnAnimatorIK()
        {
            _animator.SetLookAtPosition(AvatarLookAtPosition);

            _animator.SetLookAtWeight(AnimatorLookAtWeight);
        }
        #endregion

        /// <summary>
        ///   이펙트 삭제
        /// </summary>
        public void DestroyEffect()
        {
            if (VFXObject != null) Destroy(VFXObject);
            if (VFXInteractionObject != null) Destroy(VFXInteractionObject);
            // if (SfxSource.isPlaying) SfxSource.Stop();
        }

        #region Other Stuff
        public PhotonView GetView()
        {
            return _photonView;
        }

        public void OnChatRoomEvent(JObject json)
        {
            if (!string.IsNullOrEmpty(json["cmd"]?.ToString()))
            {
                Util.Log("PlayerActionManager", json["cmd"].ToString());
                StartCoroutine(json["cmd"].ToString(), json);
            }
            else
            {
                RNMessenger.SendResult(json, false, "cmd function not found");
            }
        }
        //RN 용
        private IEnumerator ChangeChatAgit(JObject json)
        {
            try
            {
                JObject param = json["params"].ToObject<JObject>();
                if (param.ContainsKey("AgitName"))
                {
                    string AgitName = param["AgitName"].ToString();
                    switch (AgitName)
                    {
                        case "AGT_Default_10":
                            ChangeAgit(Constants.DefaultJSONS[0]);
                            break;
                        case "AGT_Default_20":
                            ChangeAgit(Constants.DefaultJSONS[1]);
                            break;
                        case "AGT_Default_30":
                            ChangeAgit(Constants.DefaultJSONS[2]);
                            break;
                        case "Temp":
                            ChangeAgit(Constants.DefaultJSONS[3]);
                            break;
                        default:
                            RNMessenger.SendResult(json, false, AgitName + " error");
                            yield break;
                    }
                }
                else if (param.ContainsKey("AgitJSON"))
                {
                    ChangeAgit(param["AgitJSON"].ToString());
                }
                else
                {
                    RNMessenger.SendResult(json, false, "AgitName or AgitJSON is required");
                    yield break;
                }

                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        public void LoadButton(int roomJsonIndex)
        {
            _photonView.RPC("LoadByName", RpcTarget.All, roomJsonIndex);
        }

        int updateAigtPlayer = 0;
        string loadAgitJson = null;

        private IEnumerator SetUpChangeAgit()
        {
            yield return new WaitForEndOfFrame();

            List<string> newInteractables = new List<string>();

            foreach (GameObject IntObj in GameObject.FindGameObjectsWithTag("ObjectInteractable"))
            {
                string newName = Regex.Replace(IntObj.name, @"(Clone)", "");
                newName = newName.TrimEnd('(', ')');
                string _key = newName + IntObj.GetComponent<PhotonView>().ViewID.ToString();

                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(_key))
                {
                    Hashtable updateHashtable = new Hashtable();
                    updateHashtable[_key] = false;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);
                }

                newInteractables.Add(_key);
            }

            foreach (string key in PhotonNetwork.CurrentRoom.CustomProperties.Keys)
            {
                if (key.StartsWith("OBJ") && !newInteractables.Contains(key))
                {
                    Hashtable updateHashtable = new Hashtable();
                    updateHashtable[key] = null;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(updateHashtable);
                }
            }

            yield break;
        }

        public void ChangeAgit(string agitJson)
        {
            loadAgitJson = agitJson;
            JObject json = JObject.Parse(agitJson);
            Hashtable objHashtable = new Hashtable();
            List<string> objects = new List<string>();
            foreach (JObject obj in json["objects"].ToObject<JArray>().Children<JObject>())
            {
                if (!objects.Contains(obj["name"].ToString())) objects.Add(obj["name"].ToString());
            }
            objHashtable["RoomInfo"] = objHashtable["RoomInfo"] + string.Join(",", objects);
            PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);

            updateAigtPlayer = PhotonNetwork.CurrentRoom.PlayerCount;

            _photonView.RPC("UpdateHash", RpcTarget.All);

            StartCoroutine(SetUpChangeAgit());
        }

        [PunRPC]
        public void UpdatedHash(PhotonMessageInfo info)
        {
            updateAigtPlayer--;
            if (updateAigtPlayer == 0)
            {
                Player MasterClient = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.CurrentRoom.MasterClientId);
                _photonView.RPC("LoadAgit", MasterClient, loadAgitJson);
            }
        }

        [PunRPC]
        public void LoadAgit(string agitJson)
        {
            if (!_roomManager) _roomManager = FindObjectOfType<RoomManager>();
            _roomManager.Load(agitJson);
        }

        [PunRPC]
        public void UpdateHash(PhotonMessageInfo info)
        {
            Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
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

            _photonView.RPC("UpdatedHash", info.Sender);
        }

        public void UpdateHashClient()
        {
            // _photonView.RPC("UpdateHash", RpcTarget.Others);
        }

        [PunRPC]
        public void LoadByName(int roomJsonIndex)
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            _roomManager.Load(Constants.DefaultJSONS[roomJsonIndex]);
        }

        [PunRPC]
        public void ChangeAgitLoad(string agitJson)
        {
            _roomManager.Load(agitJson);
        }

        [PunRPC]
        public void ReJoinRoom(string roomName)
        {
            // PhotonNetwork.LeaveRoom();
            // PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = Constants.MaxPlayerPerRoom }, Photon.Realtime.TypedLobby.Default);

            Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
            string objects = objHashtable["RoomInfo"].ToString();
            if (!string.IsNullOrEmpty(objects))
            {
                DefaultPool defaultPool = PhotonNetwork.PrefabPool as DefaultPool;
                foreach (string objectName in objects.Split(','))
                {
#if UNITY_EDITOR
                    if (objectName == "post processing") continue;
#endif
                    // AddressableManager.AddressableLoad(objectName, out preLoadObject);   
                    // defaultPool = PhotonNetwork.PrefabPool as DefaultPool

                    GameObject preLoadObject = AddressableManager.AddressableLoad(objectName);
                    if (defaultPool == null) defaultPool = PhotonNetwork.PrefabPool as DefaultPool;
                    if (!defaultPool.ResourceCache.ContainsKey(objectName))
                        defaultPool.ResourceCache.Add(objectName, preLoadObject);
                }
            }

            PhotonNetwork.Reconnect();
            PhotonNetwork.RejoinRoom(roomName);
            // _roomManager.UpdateHash();
        }

        private IEnumerator UpdatePlayer(JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();
            JArray urls = param["urls"].ToObject<JArray>();
            yield return new WaitForSeconds(1.0f);
            _minglePlayer = FindObjectOfType<MinglePlayer>();

            if (!_minglePlayer)
            {
                RNMessenger.SendResult(json, false, "player is empty");
                yield break;
            }

            try
            {
                if (urls.Count == 0)
                {
                    RNMessenger.SendResult(json, false, "url is empty");
                    yield break;
                }

                if (_photonView)
                {
                    if (urls.Count == 1)
                    {
                        string[] url = urls.ToObject<string[]>();
                        _photonView.RPC("UpdatePlayerUrl", RpcTarget.All, url[0]);
                    }
                    else
                    {
                        List<string> urlList = new List<string>();
                        foreach (JToken url in urls.ToObject<JArray>().Children<JToken>())
                        {
                            if (!urlList.Contains(url.ToString())) urlList.Add(url.ToString());
                            Debug.LogWarning("VideoName : " + url);
                        }
                        _photonView.RPC("UpdatePlayerUrl", RpcTarget.All, urlList.ToArray());
                    }
                }
                else
                {
                    RNMessenger.SendResult(json, false, "_photonView is empty");
                    yield break;
                }

                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }

        [PunRPC]
        void UpdatePlayerUrl(string[] urlArray)
        {
            if (!_minglePlayer) return;
            List<string> urlList = new List<string>();
            urlList.AddRange(urlArray);

            _minglePlayer.ClearVideo();
            _minglePlayer.AddVideo(urlList);
            _minglePlayer.RpcTargetAllVideoPlay();
        }

        [PunRPC]
        void UpdatePlayerUrl(string url)
        {
            if (!_minglePlayer) return;

            _minglePlayer.ClearVideo();
            _minglePlayer.AddVideo(url);
            _minglePlayer.RpcTargetAllVideoPlay();
        }

        private IEnumerator PlayPlayer(JObject json)
        {
            _minglePlayer = FindObjectOfType<MinglePlayer>();

            if (!_minglePlayer)
            {
                RNMessenger.SendResult(json, false, "player is empty");
                yield break;
            }

            try
            {
                _minglePlayer.Play();
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }


        private IEnumerator StopPlayer(JObject json)
        {
            _minglePlayer = FindObjectOfType<MinglePlayer>();

            if (!_minglePlayer)
            {
                RNMessenger.SendResult(json, false, "player is empty");
                yield break;
            }

            try
            {
                _minglePlayer.Stop();
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }

        private IEnumerator NextPlayer(JObject json)
        {
            _minglePlayer = FindObjectOfType<MinglePlayer>();

            if (!_minglePlayer)
            {
                RNMessenger.SendResult(json, false, "player is empty");
                yield break;
            }

            try
            {
                _minglePlayer.PlayNext();
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }

        [System.Serializable]
        public class ObjectContent
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 closestPoint;
            public int viewID;
            public string prefabName;
            public float randomBlend;
            public bool avatarOffRoot;
        }
        #endregion
    }
}