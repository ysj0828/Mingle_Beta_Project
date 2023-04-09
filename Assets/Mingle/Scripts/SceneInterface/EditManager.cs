using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.SceneManagement;

namespace Mingle
{
    public class EditManager : MonoBehaviour
    {
        public EditController _editController;

        public string sceneName;

        private void Awake()
        {
        }

        void Start()
        {
            switch (sceneName)
            {
                case "RoomEditor":
                    RNManager.Instance.OnRoomEditEvent += OnAgitEvent;
                    break;
                case "SpaceEditor":
                    RNManager.Instance.OnSpaceEditEvent += OnAgitEvent;
                    break;
                case "AgitPreview":
                    RNManager.Instance.OnAgitPreviewEvent += OnAgitEvent;
                    break;
                case "AgitSelector":
                    RNManager.Instance.OnAgitSelectorEvent += OnAgitEvent;
                    break;
                case "InitializeAgit":
                    RNManager.Instance.OnInitializeAgitEvent += OnAgitEvent;
                    break;
                case "Profile":
                    RNManager.Instance.OnProfileEvent += OnAgitEvent;
                    break;
                case "ProfileSelector":
                    RNManager.Instance.OnProfileSelectorEvent += OnAgitEvent;
                    break;
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            if (GameManager.Instance.SceneInitalization != null) OnAgitEvent(GameManager.Instance.SceneInitalization);
        }

        void OnDestroy()
        {
            switch (sceneName)
            {
                case "RoomEditor":
                    RNManager.Instance.OnRoomEditEvent -= OnAgitEvent;
                    break;
                case "SpaceEditor":
                    RNManager.Instance.OnSpaceEditEvent -= OnAgitEvent;
                    break;
                case "AgitPreview":
                    RNManager.Instance.OnAgitPreviewEvent -= OnAgitEvent;
                    break;
                case "AgitSelector":
                    RNManager.Instance.OnAgitSelectorEvent -= OnAgitEvent;
                    break;
                case "InitializeAgit":
                    RNManager.Instance.OnInitializeAgitEvent -= OnAgitEvent;
                    break;
                case "Profile":
                    RNManager.Instance.OnProfileEvent -= OnAgitEvent;
                    break;
                case "ProfileSelector":
                    RNManager.Instance.OnProfileSelectorEvent -= OnAgitEvent;
                    break;
            }
        }
        public void DefaultAgitiLoad(string AgitName)
        {
            JObject testJson = new JObject();
            JObject param = new JObject();
            param["AgitName"] = AgitName;
            testJson["params"] = param;
            StartCoroutine("UpdateDefaultAgit", testJson);
        }

        void OnAgitEvent(JObject json)
        {
            if (!string.IsNullOrEmpty(json["cmd"]?.ToString()))
            {
                Util.Log("EditManager", json["target"]?.ToString(), json["cmd"]?.ToString());
                StartCoroutine(json["cmd"].ToString(), json);
            }
            else
            {
                RNMessenger.SendResult(json, false, "cmd function not found");
            }
        }

        private IEnumerator UpdateDefaultAgit(JObject json)
        {
            try
            {
                JObject param = json["params"].ToObject<JObject>();

                if (sceneName == Constants.RoomEditorSceneName || sceneName == Constants.SpaceEditorSceneName)
                {
                    if (sceneName == Constants.RoomEditorSceneName && param.ContainsKey("room_preset_id")) _editController.preset_id = param["room_preset_id"].ToString();
                    else if (sceneName == Constants.SpaceEditorSceneName && param.ContainsKey("preset_id")) _editController.preset_id = param["preset_id"].ToString();
                    else _editController.preset_id = "";

                    if (param.ContainsKey("inventory_space_id")) _editController.inventory_space_id = param["inventory_space_id"].ToObject<int>();
                    else _editController.inventory_space_id = 0;

                    if (param.ContainsKey("shop_space_id")) _editController.shop_space_id = param["shop_space_id"].ToString();
                    else _editController.shop_space_id = "";
                }

                if (param["AgitName"].ToString() == "AGT_Default_10") _editController.Load(Constants.DefaultJSONS[0]);
                else if (param["AgitName"].ToString() == "AGT_Default_20") _editController.Load(Constants.DefaultJSONS[1]);
                else if (param["AgitName"].ToString() == "AGT_Default_30") _editController.Load(Constants.DefaultJSONS[2]);
                else if (param["AgitName"].ToString() == "AGT_TEST") _editController.Load(Constants.DefaultJSONS[3]);

                JObject resultJson = new JObject();
                if (json.ContainsKey("cmd")) resultJson["cmd"] = json["cmd"];
                if (json.ContainsKey("cmdId")) resultJson["cmdId"] = json["cmdId"];
                resultJson["result"] = "success";
                JObject dataJSON = new JObject();
                dataJSON["AgitJSON"] = _editController.Save();
                resultJson["data"] = dataJSON;

                RNMessenger.SendJson(resultJson);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        private IEnumerator UpdateAgit(JObject json)
        {
            try
            {
                JObject parm = json["params"].ToObject<JObject>();

                if (sceneName == Constants.RoomEditorSceneName || sceneName == Constants.SpaceEditorSceneName)
                {
                    if (sceneName == Constants.RoomEditorSceneName && parm.ContainsKey("room_preset_id")) _editController.preset_id = parm["room_preset_id"].ToString();
                    else if (sceneName == Constants.SpaceEditorSceneName && parm.ContainsKey("preset_id")) _editController.preset_id = parm["preset_id"].ToString();
                    else _editController.preset_id = "";

                    if (parm.ContainsKey("inventory_space_id")) _editController.inventory_space_id = parm["inventory_space_id"].ToObject<int>();
                    else _editController.inventory_space_id = 0;

                    if (parm.ContainsKey("shop_space_id")) _editController.shop_space_id = parm["shop_space_id"].ToString();
                    else _editController.shop_space_id = "";
                }
                // Debug.Log(parm);
                _editController.Load(JsonConvert.SerializeObject(parm["AgitJSON"], Formatting.None));
                RNMessenger.SendResult(parm, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }
        private IEnumerator AddObject(JObject json)
        {
            try
            {
                JObject param = json["params"].ToObject<JObject>();

                if (!param.ContainsKey("shop_object_id"))
                {
                    RNMessenger.SendResult(json, false, "shop_object_id required");
                    yield break;
                }

                _editController.AddObject(param["name"].ToString(), param["shop_object_id"].ToString(), param.ContainsKey("inventory_object_id") ? param["inventory_object_id"].ToObject<int>() : 0);
                JObject resultParam = new JObject();
                resultParam["name"] = param["name"].ToString();
                RNMessenger.SendResult(json, true, resultParam);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        private IEnumerator ApplyObjects(JObject json)
        {
            Debug.Log("ApplyAgit");
            try
            {
                JObject creates = new JObject();
                if (!string.IsNullOrEmpty(_editController.preset_id))
                {
                    if (sceneName == Constants.RoomEditorSceneName) creates["room_preset_id"] = _editController.preset_id;
                    else if (sceneName == Constants.SpaceEditorSceneName) creates["preset_id"] = _editController.preset_id;
                }
                creates["inventory_space_id"] = _editController.inventory_space_id;
                creates["shop_space_id"] = _editController.shop_space_id;


                creates["objects"] = _editController.ReturnAddedObjects();
                JObject removes = new JObject();
                removes["objects"] = _editController.ReturnDeletedObjects();

                JObject applyJson = new JObject();

                if (!string.IsNullOrEmpty(_editController.preset_id))
                {
                    applyJson["creates"] = creates;
                    applyJson["removes"] = removes;
                }
                else
                {
                    if (sceneName == Constants.RoomEditorSceneName) applyJson["create_new_room_preset"] = creates;
                    else if (sceneName == Constants.SpaceEditorSceneName) applyJson["create_new_preset"] = creates;
                }

                Debug.Log(applyJson);

                RNMessenger.SendResult(json, true, applyJson);
            }
            catch (System.Exception exception)
            {

                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        private IEnumerator SaveObjects(JObject json)
        {
            Debug.Log("SaveObjects");
            try
            {
                RNMessenger.SendResult(json, true, _editController.Save());
            }
            catch (System.Exception exception)
            {

                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        private IEnumerator UpdatePresetDetailId(JObject json)
        {
            Debug.Log("UpdatePresetDetailId");
            try
            {
                JObject param = json["params"].ToObject<JObject>();
                if (param.ContainsKey("objects"))
                {
                    JArray objects = param["objects"].ToObject<JArray>();
                    _editController.UpdatePresetDetailId(objects);
                }
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }
        private IEnumerator UpdateRoomPresetDetailId(JObject json)
        {
            Debug.Log("UpdateRoomPresetDetailId");
            try
            {
                JObject param = json["params"].ToObject<JObject>();
                if (param.ContainsKey("objects"))
                {
                    JArray objects = param["objects"].ToObject<JArray>();
                    _editController.UpdateRoomPresetDetailId(objects);
                }
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }
        private IEnumerator GetCreates(JObject json)
        {
            try
            {
                Debug.Log(_editController.ReturnAddedObjects());
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {

                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }
        private IEnumerator GetRemoves(JObject json)
        {
            try
            {
                Debug.Log(_editController.ReturnDeletedObjects());
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {

                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

        private IEnumerator UpdateRotation(JObject json)
        {
            try
            {
                JObject param = json["params"].ToObject<JObject>();
                _editController.UpdateRotation(param["Rotation"].ToObject<float>());
                RNMessenger.SendResult(json, true);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }
        private IEnumerator DeleteObject(JObject json)
        {
            try
            {
                string name = _editController.DeleteObject();
                JObject param = new JObject();
                param["name"] = name;
                RNMessenger.SendResult(json, true, param);
            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
            }
            yield break;
        }

    }
}