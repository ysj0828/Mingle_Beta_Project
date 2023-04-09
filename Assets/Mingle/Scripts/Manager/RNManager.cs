using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;

// RN 에서 메시지를 받는 모듈(싱글톤)
namespace Mingle
{
    public class RNManager : MonoBehaviour
    {
        // 포톤 콜백 처리
        private PhotonManager _photonManager = null;//new PhotonManager();
        // 씬 전환 관리
        private MingleSceneManager _sceneManager = null;

        // 씬별 이벤트 처리용 핸들러
        public event JsonEventHandler OnCharacterCustomEvent;
        public event JsonEventHandler OnCharacterPreviewEvent;
        public event JsonEventHandler OnRandomCharacterEvent;
        public event JsonEventHandler OnFeedEvent;
        public event JsonEventHandler OnRoomEvent;
        public event JsonEventHandler OnRoomEditEvent;
        public event JsonEventHandler OnSpaceEditEvent;
        public event JsonEventHandler OnAgitPreviewEvent;
        public event JsonEventHandler OnAgitSelectorEvent;
        public event JsonEventHandler OnInitializeAgitEvent;
        public event JsonEventHandler OnProfileEvent;
        public event JsonEventHandler OnProfileSelectorEvent;
        public event JsonEventHandler OnCharcterCustomSyncEvent;
        public event JsonEventHandler OnFacialRecordEvent;
        public event JsonEventHandler OnChatRoomEvent;
        public event JsonEventHandler OnAnimationUpdate;

        private static RNManager _instance;
        public static RNManager Instance
        {
            get
            {
                // 인스턴스가 없는 경우에 접근하려 하면 인스턴스를 할당해준다.
                if (!_instance)
                {
                    _instance = FindObjectOfType(typeof(RNManager)) as RNManager;

                    if (_instance == null)
                        Debug.Log("no Singleton obj");
                }
                return _instance;
            }
        }

        // 돈디스트로이 로 변경
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                _sceneManager = gameObject.GetComponent<MingleSceneManager>();
                _photonManager = gameObject.GetComponent<PhotonManager>();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 포톤 이벤트 핸들러 연결
        void Start()
        {
            _photonManager.JoinedRoom += OnPhotonJoinedRoom;
            _photonManager.LeaveRoom += OnPhotonLeavedRoom;
        }

        // 포톤 이벤트 핸들러 해제
        private void OnDestroy()
        {
            if (_photonManager)
            {
                _photonManager.JoinedRoom -= OnPhotonJoinedRoom;
                _photonManager.LeaveRoom -= OnPhotonLeavedRoom;
            }
        }

        // 방 접속 처리
        void OnPhotonJoinedRoom(string roomName)
        {
            Util.Log("GM OnPhotonJoinedRoom", roomName, PhotonNetwork.IsMasterClient.ToString());
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("ChatRoom");
            }
            StartCoroutine(LoadLevelLoading());
        }

        // 최소 0.3초 로딩창 보이게
        private IEnumerator LoadLevelLoading()
        {
            _sceneManager.ShowLoadingImage();
            yield return new WaitForSeconds(0.3f);
            while (PhotonNetwork.LevelLoadingProgress > 0 && PhotonNetwork.LevelLoadingProgress < 1)
            {
                yield return new WaitForEndOfFrame();
            }
            _sceneManager.HideLoadingImage();
            yield break;
        }

        // 방 나가기 처리
        void OnPhotonLeavedRoom(string roomName)
        {
            JObject json = new JObject();
            JObject param = new JObject();
            param["sceneName"] = "Empty";
            json["params"] = param;
            StartCoroutine(ChangeScene(json));
        }

        // RN에서 string 을 받는 부분
        // JSON으로 파싱후 실행
        public void Message(string message)
        {
            JObject json = new JObject();
            try
            {
                json = JObject.Parse(message);
                Util.Log("RECV : ", JsonConvert.SerializeObject(json, Formatting.None));
                RunJson(json);
            }
            catch (System.Exception exception)
            {
                // JSON파싱 실패 예외처리
                Debug.LogError(exception.Message);
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }
        }

        // 파싱 한 JSON 처리
        // target에 핸들러 Invoke를 호출 하여 JSON 전달
        void RunJson(JObject json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json["target"]?.ToString()))
                {
                    string target = json["target"]?.ToString();
                    Util.Log(json["target"]?.ToString());
                    Util.Log(json["cmd"]?.ToString());
                    if (target == Constants.CharacterCustomSceneName)
                    {
                        Util.Log("OnCharacterCustomEvent");
                        OnCharacterCustomEvent.Invoke(json);
                    }
                    else if (target == Constants.CharacterPreviewSceneName)
                    {
                        Util.Log("OnCharacterPreviewEvent");
                        OnCharacterPreviewEvent.Invoke(json);
                    }
                    else if (target == Constants.RandomCharacterSceneName)
                    {
                        Util.Log("OnRandomCharacterEvent");
                        OnRandomCharacterEvent.Invoke(json);
                    }
                    else if (target == Constants.FeedSceneName)
                    {
                        Util.Log("OnFeedEvent");
                        OnFeedEvent.Invoke(json);
                    }
                    else if (target == Constants.RoomSceneName)
                    {
                        Util.Log("OnRoomEvent");
                        OnRoomEvent.Invoke(json);
                    }
                    else if (target == Constants.RoomEditorSceneName)
                    {
                        Util.Log("RoomEditorSceneName");
                        OnRoomEditEvent.Invoke(json);
                    }
                    else if (target == Constants.SpaceEditorSceneName)
                    {
                        Util.Log("SpaceEditorSceneName");
                        OnSpaceEditEvent.Invoke(json);
                    }
                    else if (target == Constants.AgitPreviewSceneName)
                    {
                        Util.Log("OnAgitPreviewEvent");
                        OnAgitPreviewEvent.Invoke(json);
                    }
                    else if (target == Constants.AgitSelectorSceneName)
                    {
                        Util.Log("OnAgitSelectorEvent");
                        OnAgitSelectorEvent.Invoke(json);
                    }
                    else if (target == Constants.InitializeAgitSceneName)
                    {
                        Util.Log("OnInitializeAgitEvent");
                        OnInitializeAgitEvent.Invoke(json);
                    }
                    else if (target == Constants.ProfileSceneName)
                    {
                        Util.Log("OnProfileEvent");
                        OnProfileEvent.Invoke(json);
                    }
                    else if (target == Constants.ProfileSelectorSceneName)
                    {
                        Util.Log("OnProfileSelectorEvent");
                        OnProfileSelectorEvent.Invoke(json);
                    }
                    else if (target == Constants.CharacterCustomSyncSceneName)
                    {
                        Util.Log("OnCharcterCustomSyncEvent");
                        OnCharcterCustomSyncEvent.Invoke(json);
                    }
                    else if (target == Constants.FacialRecordSceneName)
                    {
                        Util.Log("OnFacialRecordEvent");
                        OnFacialRecordEvent.Invoke(json);
                    }
                    else if (target == Constants.ChatRoomSceneName)
                    {
                        Util.Log("OnChatRoomEvent");
                        OnChatRoomEvent.Invoke(json);
                    }
                    else if (target == Constants.AnimationUpdate)
                    {
                        Util.Log("OnAnimationUpdate");
                        OnAnimationUpdate.Invoke(json);
                    }
                }
                else if (!string.IsNullOrEmpty(json["cmd"]?.ToString()))
                {
                    // target이 없고 cmd가 있을때 처리
                    Util.Log("target Not found : " + json.ToString());
                    // cmd 명의 코루틴 실행
                    StartCoroutine(json["cmd"].ToString(), json);
                    RNMessenger.SendResult(json, true);
                }
                else
                {
                    RNMessenger.SendResult(json, false, "target and cmd is empty");
                }
            }
            catch (System.Exception exception)
            {
                // 예러 예외처리
                Debug.LogError(exception.Message);
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }
        }

        // 씬전환
        // JSON["params"]["sceneName"] 으로 씬 전환
        private IEnumerator ChangeScene(JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();
            string sceneName = param["sceneName"].ToString();

            // 전환하는 씬이 Empty씬이라면 초기 씬으로 강제 전환
            if (sceneName != Constants.EmptySceneName && _sceneManager.HasScene(Constants.ChatRoomSceneName))
            {
                // 씬을 전환할때 챗팅방 씬이 있다면 기존 씬 비활성화 후 씬 로드
                if (sceneName == Constants.ChatRoomSceneName)
                {
                    // 전환하는 씬이 채팅방 이라면 기존 로드된씬 비활성화 후 채팅방으로 전환
                    _sceneManager.SetInActiveAllScene();
                    _sceneManager.SetActiveScene(sceneName);
                    RunJson(param);

                    int countLoaded = SceneManager.sceneCount;

                    for (int i = 0; i < countLoaded; i++)
                    {
                        if (SceneManager.GetSceneAt(i).name != Constants.ChatRoomSceneName)
                        {
                            SceneManager.UnloadScene(SceneManager.GetSceneAt(i));
                            i--;
                        }
                    }
                    RNMessenger.SendResult(json, true);
                }
                else
                {
                    int countLoaded = SceneManager.sceneCount;

                    for (int i = 0; i < countLoaded; i++)
                    {
                        if (SceneManager.GetSceneAt(i).name == sceneName)
                        {
                            SceneManager.UnloadScene(SceneManager.GetSceneAt(i));
                            break;
                        }
                    }
                    _sceneManager.ChangeScene(json);
                }

            }
            else
            {
                // 씬전환
                _sceneManager.ChangeScene(json);
            }

            yield break;
        }

        // 토큰업데이트
        private IEnumerator ResponseToken(JObject json)
        {
            GameManager.Instance.Token = json["token"].ToString();
            yield break;
        }

        // 방 접속
        private IEnumerator ConnectToChatRoom(JObject json)
        {
            try
            {
                _sceneManager.ShowLoadingImage();
                JObject param = json["params"].ToObject<JObject>();

                // 방접속 정보 업데이트
                GameManager.Instance.Infomation.RoomID = param["RoomID"].ToString();
                GameManager.Instance.Infomation.CharacterInfo = param["CharacterInfo"].ToObject<JObject>();
                GameManager.Instance.Infomation.NickName = param["NickName"].ToString();
                GameManager.Instance.NickName = param["NickName"].ToString();
                PhotonNetwork.NickName = param["NickName"].ToString() + Constants.Spliter + param["UserID"].ToString();

                // 방정보 프리셋 분기
                if (param.ContainsKey("AgitName"))
                {
                    // AgitName 을 가지고 있다면 저장되어있는 프리셋 로드
                    string AgitName = param["AgitName"].ToString();
                    switch (AgitName)
                    {
                        case "AGT_Default_10":
                            GameManager.Instance.Infomation.AgitJsonString = Constants.DefaultJSONS[0];
                            break;
                        case "AGT_Default_20":
                            GameManager.Instance.Infomation.AgitJsonString = Constants.DefaultJSONS[1];
                            break;
                        case "AGT_Default_30":
                            GameManager.Instance.Infomation.AgitJsonString = Constants.DefaultJSONS[2];
                            break;
                        case "AGT_TEST":
                            GameManager.Instance.Infomation.AgitJsonString = Constants.DefaultJSONS[3];
                            break;
                        default:
                            RNMessenger.SendResult(json, false, AgitName + " error");
                            yield break;
                    }
                }
                else if (param.ContainsKey("AgitJSON"))
                {
                    // AgitJSON을 가지고 있다면 방정보에 AgitJSON 설정 
                    GameManager.Instance.Infomation.AgitJsonString = param["AgitJSON"].ToString();
                }
                else
                {
                    RNMessenger.SendResult(json, false, "AgitName or AgitJSON is required");
                    yield break;
                }
            }
            catch (System.Exception exception)
            {
                _sceneManager.HideLoadingImage();
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
            // 포톤 방접속
            _photonManager.JoinOrCreateRoom(GameManager.Instance.Infomation.RoomID, json);

            yield break;
        }

        // 방 나가기
        private IEnumerator LeaveChatRoom(JObject json)
        {
            _sceneManager.ShowLoadingImage();
            PhotonNetwork.LeaveRoom();
            // json["Animation"].ToString()
            yield break;
        }

        // 애니메이션 업데이트
        private IEnumerator UpdateAnimation(JObject json)
        {
            Util.Log("update animation coroutine");
            try
            {
                GameManager.Instance.AnimationString = json["Animation"].ToString();
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }

        // 볼륨 업데이트
        private IEnumerator UpdateVolume(JObject json)
        {
            Debug.Log(json);
            try
            {
                Debug.Log(json);
                JObject param = json["params"].ToObject<JObject>();
                Debug.Log(param);
                GameManager.Instance.Volume = param["volume"].ToObject<float>();
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }

            yield break;
        }

    }
}