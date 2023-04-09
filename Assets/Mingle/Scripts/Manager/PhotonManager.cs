using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Mingle
{
    // 포톤 콜벡처리 모듈
    public class PhotonManager : MonoBehaviourPunCallbacks
    {
        // IsMasterClient
        public event StringEventHandler JoinedRoom;
        public event StringEventHandler LeaveRoom;
        public event PlayerEventHandler PlayerEnter;

        private string _targetRommName = null;
        private JObject _connectJson = null;

        public static string connectToChatRoomJsonString = null;

        void Start()
        {
            Util.Log("PhotonManager Start");
            PhotonNetwork.GameVersion = Constants.GameVersion;
            PhotonNetwork.AutomaticallySyncScene = true;
            // PhotonNetwork.KeepAliveInBackground = 6000000;
            PhotonNetwork.KeepAliveInBackground = 10;
            PhotonNetwork.ConnectUsingSettings();
        }

        #region Public Methods

        public override void OnConnectedToMaster()
        {
            Util.Log("PUN OnConnectedToMaster", PhotonNetwork.NetworkClientState.ToString());
            if (!string.IsNullOrEmpty(_targetRommName)) JoinOrCreateRoom(_targetRommName);
            // if (_is_connect) JoinOrCreateRoom(_infomation.RoomID);
            // JoinOrCreateRoom("Test111");
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.SetPlayerCustomProperties(null);
            SceneManager.LoadScene(0);
            // LeaveRoom.Invoke("OnLeftRoom");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            SendRoomUserIDs("UpdateRoomUsers");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            SendRoomUserIDs("DisConnectedRoomUsers");
        }

        private void SendRoomUserIDs(string cmd)
        {
            JObject json = new JObject();
            json["cmd"] = cmd;
            JObject param = new JObject();
            param["UserIDs"] = GetCurrentRoomUserIDs();
            json["params"] = param;

            RNMessenger.SendJson(json);
        }

        private JArray GetCurrentRoomUserIDs()
        {
            JArray roomIDs = new JArray();

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                string[] nickNameSplit = player.NickName.Split(Constants.Spliter);
                if (nickNameSplit.Length >= 2) roomIDs.Add(nickNameSplit[1]);
            }

            return roomIDs;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Util.Log("OnDisconnected");
            // PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.Reconnect();

            if ((cause == DisconnectCause.ClientTimeout || cause == DisconnectCause.ServerTimeout) && connectToChatRoomJsonString != null)
            {
                // Debug.LogWarning("disconnected and rejoining 1");
                //reconnect
                transform.GetComponent<RNManager>().Message(connectToChatRoomJsonString);
                // Debug.LogWarning("disconnected and rejoining 2");
            }
        }

        public void JoinOrCreateRoom(string roomName, JObject connectJson = null)
        {
            // Util.Log("JoinOrCreateRoom to", roomName);
            _targetRommName = roomName;
            _connectJson = connectJson;
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
            {
                bool ret = PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = Constants.MaxPlayerPerRoom }, Photon.Realtime.TypedLobby.Default);
                Util.Log("JoinOrCreateRoom ret", ret.ToString());
            }
        }

        public override void OnCreatedRoom()
        {
            Util.Log("OnCreatedRoom");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Util.Log("OnCreateRoomFailed", returnCode.ToString(), message.ToString());
        }

        public override void OnJoinedRoom()
        {
            _targetRommName = null;

            if (!PhotonNetwork.IsMasterClient)
            {
                float startTime = Time.realtimeSinceStartup;
                ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
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
                Debug.Log("KAIKAI Room " + string.Format("{0:0.00} ", (Time.realtimeSinceStartup - startTime)));
            }


            JoinedRoom.Invoke(PhotonNetwork.CurrentRoom.Name);
            // StartCoroutine(PhotonLoad(_infomation.room_template_uuid));
            // GameObject go = PhotonNetwork.Instantiate("G_Root_(1) Variant", Vector3.zero, Quaternion.identity);
            // Debug.Log("go : " + go);
            // PhotonNetwork.LoadLevel("CreateRoom");
            // RNMessenger.SendToRN("OnJoinedRoom");
            if (_connectJson != null)
            {
                RNMessenger.SendResult(_connectJson, true);
                _connectJson = null;
            }

            SendRoomUserIDs("ConnectedRoomUsers");
        }
        #endregion
    }
}