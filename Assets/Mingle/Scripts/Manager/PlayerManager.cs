using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine.AI;

namespace Mingle
{
    public class PlayerManager : MonoBehaviour
    {
        public CustomManager _customManager;
        public RoomManager _roomManager;

        [SerializeField] private GameObject _playerPrefab;
        public static GameObject MyAvatar = null;

        private Camera _mainCam;

        private void Awake()
        {
            _mainCam = Camera.main;
        }

        public void RNM(string msg)
        {
            JObject json = new JObject();
            json = JObject.Parse(msg);

            if (!string.IsNullOrEmpty(json["target"]?.ToString()))
            {
                string target = json["target"]?.ToString();
                if (target == "Player")
                {
                    RNManager.Instance.Message(msg);
                }
            }
        }

        public void sub()
        {
            MyAvatar.GetComponent<PlayerActionManager>().SubscriptEvents();
        }

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // if (GameManager.Instance.Infomation.AgitName == "AGT_Default_10") _roomManager.Load(Constants.DefaultJSONS[0]);
                // else if (GameManager.Instance.Infomation.AgitName == "AGT_Default_20") _roomManager.Load(Constants.DefaultJSONS[1]);
                // else if (GameManager.Instance.Infomation.AgitName == "AGT_Default_30") _roomManager.Load(Constants.DefaultJSONS[2]);
                _roomManager.Load(GameManager.Instance.Infomation.AgitJsonString);

                ExitGames.Client.Photon.Hashtable objHashtable = new ExitGames.Client.Photon.Hashtable();

                List<string> objects = new List<string>();
                foreach (JObject obj in _roomManager._currentInfoJSON["objects"].ToObject<JArray>().Children<JObject>())
                {
                    if (!objects.Contains(obj["name"].ToString())) objects.Add(obj["name"].ToString());
                }
                objHashtable["RoomInfo"] = string.Join(",", objects);

                PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
            }

            if (_playerPrefab == null) Debug.LogError("Prefab empty");

            if (PlayerActionManager.LocalPlayerInstance == null)
            {
                Vector3 InstantiatePosition = Vector3.zero;
                if (GameObject.Find("BackGround_Free_04") || GameObject.Find("BackGround_Free_04(Clone)")) InstantiatePosition = new Vector3(0, 0, 4);
                else if (GameObject.Find("BackGround_Free_05") || GameObject.Find("BackGround_Free_05(Clone)")) InstantiatePosition = new Vector3(0, 3.67f, 0);

                MyAvatar = PhotonNetwork.Instantiate("G_Root_hand", InstantiatePosition, Quaternion.identity, 0);
                // MyAvatar = PhotonNetwork.Instantiate(this._playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
                // Debug.Log("xxxxxxxxxxx avatar instantiate : " + MyAvatar.GetComponent<PhotonView>().IsMine);
                _mainCam.transform.GetComponent<CameraManager>().SetAvatar(MyAvatar);
                // Camera.main.transform.GetComponent<CameraManager>().FindAvatar();
            }

            // if (RNManager.Instance) RNManager.Instance.OnCharcterCustomSyncEvent += CharcterSync;
        }

        void OnDestroy()
        {
            // if (RNManager.Instance) RNManager.Instance.OnCharcterCustomSyncEvent -= CharcterSync;
        }

        BoxCollider walkFloor = null;
        private void Update()
        {
            if (_mainCam.transform.GetComponent<CameraManager>().RefTarget == null)
            {
                _mainCam.transform.GetComponent<CameraManager>().SetAvatar(MyAvatar);
            }
            if (PhotonNetwork.IsMasterClient) return;
            if (walkFloor == null)
            {
                if (!GameObject.FindGameObjectWithTag("Walkable")) return;
                walkFloor = GameObject.FindGameObjectWithTag("Walkable").GetComponent<BoxCollider>();
                walkFloor.GetComponent<NavMeshSurface>().BuildNavMesh();
            }

        }

    }
}