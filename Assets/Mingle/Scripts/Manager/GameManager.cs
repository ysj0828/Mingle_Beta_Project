using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

namespace Mingle
{
    // 정보를 저장하는 싱글톤 게임 매니져 모듈 
    public class GameManager : MonoBehaviourPunCallbacks
    {
        // Unity에서 API 비호출 시나리오 로 비활성화
        // private APIManager _apiManager = new APIManager();
        // 정보를 저장하는 클래스
        public Infomation Infomation = new Infomation();
        public JObject SceneInitalization = null;
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType(typeof(GameManager)) as GameManager;

                    if (_instance == null)
                        Debug.Log("no Singleton obj");
                }
                return _instance;
            }
        }

        public string Token
        {
            get { return Infomation.Token; }
            set { Infomation.Token = value; }
        }

        public float Volume
        {
            get { return Infomation.Volume; }
            set
            {
                Infomation.Volume = value;
                AudioListener.volume = value;
            }
        }

        public string NickName
        {
            get { return Infomation.NickName; }
            set { Infomation.NickName = value; }
        }

        public string AnimationString
        {
            get => Infomation.AnimationString;
            set
            {
                Infomation.AnimationString = value;
                // AnimationEvent?.Invoke(value);
            }
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Volume = 1;
                AudioListener.volume = 1f;
                DontDestroyOnLoad(gameObject);
                AddressableManager.UpdateCatalog();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // FPS 설정
            Application.targetFrameRate = Constants.TargetFPS;

            // _photonManager = gameObject.GetComponent<PhotonManager>();
            // _photonManager.JoinedRoom += OnPhotonJoinedRoom;
            // StartCoroutine(_apiManager.api_test());
            // _apiManager.api_test();
        }

        private void OnDestroy()
        {
            // _photonManager.JoinedRoom -= OnPhotonJoinedRoom;
        }

        public void discon()
        {
            PhotonNetwork.Disconnect();
        }

        void Update()
        {
            // if (Input.GetKeyDown(KeyCode.A))
            // {
            //     Debug.Log("room : " + PhotonNetwork.CurrentRoom.PlayerCount);
            // }

            // else if (Input.GetKeyDown(KeyCode.B))
            // {
            //     Debug.Log("master : " + PhotonNetwork.CountOfPlayersOnMaster);
            // }

            // else if (Input.GetKeyDown(KeyCode.C))
            // {
            //     Debug.Log("list length : " + PhotonNetwork.PlayerList.Length);
            // }
        }

        ///////////////////////////// Private Callbacks /////////////////////////////
        void OnPhotonJoinedRoom(string msg)
        {
            Util.Log("GM OnPhotonJoinedRoom", msg);
            //PhotonNetwork.LoadLevel("Test");
        }

        ///////////////////////////// Public methods /////////////////////////////

        public void SendToRN(JObject json)
        {
            RNMessenger.SendToRN(JsonConvert.SerializeObject(json, Formatting.None));
        }
    }
}