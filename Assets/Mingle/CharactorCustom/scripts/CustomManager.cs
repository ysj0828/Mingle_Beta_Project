using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using Photon.Pun;

namespace Mingle
{
    public class CustomManager : MonoBehaviour
    {
        private List<string> _defaultCustom = new List<string>
        {
            "G_Body_01_1",
            "G_Head_01_1",
            "G_Shirts_00_1",
            "G_Pants_00_1"
        };
        private string _jsonObjectName = "object_name";
        private string _jsonColor = "color";
        private Animator _shopAnimation;

        [Header("Current Character")]
        public GameObject Character = null;

        [Header("Main Parts")]
        [SerializeField] private SkinnedMeshRenderer _currentHair = null;
        [SerializeField] private SkinnedMeshRenderer _currentUpper = null;
        [SerializeField] private SkinnedMeshRenderer _currentPants = null;
        [SerializeField] private SkinnedMeshRenderer _currentShoes = null;
        [SerializeField] private SkinnedMeshRenderer _currentBody = null;
        [SerializeField] private MeshFilter _bodyMesh = null;
        [SerializeField] private SkinnedMeshRenderer _currentHead = null;
        [SerializeField] private SkinnedMeshRenderer _currentSocks = null;

        [Header("Accessories Parts Mesh")]
        [SerializeField] private MeshFilter _currentWatchMesh = null;
        [SerializeField] private MeshFilter _currentBeardMesh = null;
        [SerializeField] private MeshFilter _currentEarringRMesh = null;
        [SerializeField] private MeshFilter _currentEarringLMesh = null;
        [SerializeField] private MeshFilter _currentGlassesMesh = null;
        [SerializeField] private MeshFilter _currentBagHandMesh = null;
        [SerializeField] private MeshFilter _currentBagBackMesh = null;
        [SerializeField] private MeshFilter _currentBagCrossMesh = null;

        [Header("Accessories Parts Renderer")]
        [SerializeField] private MeshRenderer _currentWatchRenderer = null;
        [SerializeField] private MeshRenderer _currentBeardRenderer = null;
        [SerializeField] private MeshRenderer _currentEarringRRenderer = null;
        [SerializeField] private MeshRenderer _currentEarringLRenderer = null;
        [SerializeField] private MeshRenderer _currentGlassesRenderer = null;
        [SerializeField] private MeshRenderer _currentBagHandRenderer = null;
        [SerializeField] private MeshRenderer _currentBagBackRenderer = null;
        [SerializeField] private MeshRenderer _currentBagCrossRenderer = null;

        [Header("Facial")]
        [SerializeField] private SkinnedMeshRenderer _facialHead = null;
        [SerializeField] private SkinnedMeshRenderer _facialEye = null;

        [SerializeField] private HeadInCustomScene _headInCustomScene = HeadInCustomScene.None;
        public enum HeadInCustomScene
        {
            None,
            CharacterCustom,
            RandomCharacter,
        }

        [Header("SceneNameSetting")]
        [SerializeField] private CharacterSceneName _sceneName = CharacterSceneName.None;


        [Header("Capture")]
        [SerializeField] public HeadPicture[] _headPicture = null;

        public enum CharacterSceneName
        {
            None,
            CharacterCustom,
            CharacterPreview,
            RandomCharacter,
            RecordFacial,
            ChatRoom,
        }

        private List<GameObject> _changeParts = new List<GameObject>();
        private Dictionary<string, Mesh> _currentCustom = new Dictionary<string, Mesh>();

        private AsyncOperationHandle<TextAsset> _loadHandle;

        private bool _hasOnepice = false;
        private string _lastObjectName = "null";

        // private S3Manager S3manager = new S3Manager();
        private MaterialPropertyBlock mpb;
        private void Awake()
        {
            InitializeSceneSetting();

            foreach (string partName in Constants.AvatarBlendParts)
            {
                _currentCustom.Add(partName, null);
                _ResetBlendCheck.Add(partName, false);
            }
        }

        private void InitializeSceneSetting()
        {
            // 콜백 이벤트 세팅
            switch (_sceneName)
            {
                case CharacterSceneName.CharacterCustom:
                    RNManager.Instance.OnCharacterCustomEvent += OnCharacterEvent;
                    break;
                case CharacterSceneName.CharacterPreview:
                    RNManager.Instance.OnCharacterPreviewEvent += OnCharacterEvent;
                    break;
                case CharacterSceneName.RandomCharacter:
                    RNManager.Instance.OnRandomCharacterEvent += OnCharacterEvent;
                    break;
                case CharacterSceneName.RecordFacial:
                    _defaultCustom = new List<string>
                    {
                        "G_Head_01_1",
                        "G_Hair_01_1"
                    };
                    break;
            }

            switch (_headInCustomScene)
            {
                case HeadInCustomScene.CharacterCustom:
                    RNManager.Instance.OnCharacterCustomEvent += OnCharacterEvent;
                    break;
                case HeadInCustomScene.RandomCharacter:
                    RNManager.Instance.OnRandomCharacterEvent += OnCharacterEvent;
                    break;
            }
        }

        void OnDestroy()
        {
            switch (_sceneName)
            {
                case CharacterSceneName.CharacterCustom:
                    RNManager.Instance.OnCharacterCustomEvent -= OnCharacterEvent;
                    break;
                case CharacterSceneName.CharacterPreview:
                    RNManager.Instance.OnCharacterPreviewEvent -= OnCharacterEvent;
                    break;
                case CharacterSceneName.RandomCharacter:
                    RNManager.Instance.OnRandomCharacterEvent -= OnCharacterEvent;
                    break;
            }

            switch (_headInCustomScene)
            {
                case HeadInCustomScene.CharacterCustom:
                    RNManager.Instance.OnCharacterCustomEvent -= OnCharacterEvent;
                    break;
                case HeadInCustomScene.RandomCharacter:
                    RNManager.Instance.OnRandomCharacterEvent -= OnCharacterEvent;
                    break;
            }
            // Release();
        }

        private void OnCharacterEvent(JObject json)
        {
            if (!string.IsNullOrEmpty(json["cmd"]?.ToString()))
            {
                Util.Log("CustomManager", json["cmd"].ToString());
                StartCoroutine(json["cmd"].ToString(), json);
            }
            else
            {
                if (_headInCustomScene == HeadInCustomScene.None) RNMessenger.SendResult(json, false, "cmd function not found");
            }
        }

        private void Start()
        {
            if (GameManager.Instance && GameManager.Instance.SceneInitalization != null) OnCharacterEvent(GameManager.Instance.SceneInitalization);
            else if (_sceneName != CharacterSceneName.ChatRoom) SetDefaultCustom();
        }

        public void CustomSyncFromRPC(JObject json)
        {
            StartCoroutine(UpdateCustoms(json));
        }

        //RN 용
        private IEnumerator UpdateHeadCustoms(JObject json)
        {
            //Costume Update
            JObject param = json["params"].ToObject<JObject>();

            foreach (KeyValuePair<string, JToken> property in param)
            {
                if (property.Key == Constants.AvartarPartHair || property.Key == Constants.AvartarPartHead || property.Key == Constants.AvartarPartEarring
                                    || property.Key == Constants.AvartarPartBeard || property.Key == Constants.AvartarPartGlasses)
                {
                    UpdatePart(property.Key, property.Value.ToObject<JObject>());
                }
            }

            if (_headInCustomScene == HeadInCustomScene.None) RNMessenger.SendResult(json, true);
            yield break;
        }

        //RN 용
        private IEnumerator UpdateCustoms(JObject json)
        {
            float startTime = Time.realtimeSinceStartup;

            //Costume Update
            JObject param = json["params"].ToObject<JObject>();

            ResetCustom();
            SetDefaultCustom();

            if (_sceneName == CharacterSceneName.RecordFacial)
            {
                // 헤드 먼저 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartHead)
                    {
                        UpdatePart(property.Key, property.Value.ToObject<JObject>());
                    }
                }
                // 페이셜 관련 커스텀만 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartHead) continue;
                    if (!IsFacialPart(property.Key)) continue;
                    UpdatePart(property.Key, property.Value.ToObject<JObject>());
                }
                // 컬러적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key != Constants.AvartarPartBody) continue;
                    JObject part = property.Value.ToObject<JObject>();
                    string[] color = part[_jsonColor].ToString().Split(',');
                    if (color.Length == 1) SetCustomColor(color[0], null, property.Key);
                    else if (color.Length == 2) SetCustomColor(color[0], color[1], property.Key);
                }
            }
            else
            {
                // 기본 바디 적용
                GameObject startobj = AddressableManager.AddressableCustomLoad("G_Body_01_1");
                UpdateCustom(startobj);
                // 헤드 먼저 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartHead)
                    {
                        UpdatePart(property.Key, property.Value.ToObject<JObject>());
                    }
                }
                // 바디 먼저 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartBody)
                    {
                        UpdatePart(property.Key, property.Value.ToObject<JObject>());
                    }
                }
                // 바디를 제외하고 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartBody || property.Key == Constants.AvartarPartHead) continue;
                    UpdatePart(property.Key, property.Value.ToObject<JObject>());
                }

                if (_headInCustomScene == HeadInCustomScene.None) RNMessenger.SendResult(json, true);
            }

            Debug.Log("KAIKAI Custom " + string.Format("{0:0.00} ", (Time.realtimeSinceStartup - startTime)));
            yield break;
        }


        //RN 용
        private IEnumerator CaptureHead(JObject json)
        {
            if (_headInCustomScene != HeadInCustomScene.None) yield break;

            try
            {
                if (_headPicture == null || _headPicture.Length < 2)
                {
                    RNMessenger.SendResult(json, false, "There are no heads to capture.");
                    yield break;
                }

                JObject param = new JObject();
                param["ClosedMouth"] = _headPicture[0].HeadPicturePhoto();
                param["OpenedMouth"] = _headPicture[1].HeadPicturePhoto();
                RNMessenger.SendResult(json, true, param);

            }
            catch (System.Exception exception)
            {
                RNMessenger.SendResult(json, false, exception.Message);
                throw;
            }
            yield break;
        }


        private bool IsFacialPart(string type)
        {
            foreach (string partType in Constants.FacialParts)
            {
                if (partType == type) return true;
            }

            return false;
        }

        private IEnumerator UpdateCustom(JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();


            if (_sceneName == CharacterSceneName.RecordFacial)
            {
                // 헤드 먼저 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartHead)
                    {
                        UpdatePart(property.Key, property.Value.ToObject<JObject>());
                        break;
                    }
                }
                // 페이셜 관련 커스텀만 적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key == Constants.AvartarPartHead) continue;
                    if (!IsFacialPart(property.Key)) continue;
                    // Debug.Log(property.Key);
                    UpdatePart(property.Key, property.Value.ToObject<JObject>());
                }
                // 컬러적용
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    if (property.Key != Constants.AvartarPartBody) continue;
                    JObject part = property.Value.ToObject<JObject>();
                    string[] color = part[_jsonColor].ToString().Split(',');
                    if (color.Length == 1) SetCustomColor(color[0], null, Constants.AvartarPartBody);
                    else if (color.Length == 2) SetCustomColor(color[0], color[1], Constants.AvartarPartBody);
                }
            }
            else
            {
                foreach (KeyValuePair<string, JToken> property in param)
                {
                    UpdatePart(property.Key, property.Value.ToObject<JObject>(), true);
                }
            }

            if (_headInCustomScene == HeadInCustomScene.None) RNMessenger.SendResult(json, true);
            yield break;
        }

        void UpdatePart(string type, JObject part, bool showAnimation = false)
        {
            try
            {
                string name = part[_jsonObjectName].ToString();
                if (type != "buy")
                {
                    if (name == "")
                    {
                        UndressCustom(type);
                        return;
                    }

                    GameObject startobj = AddressableManager.AddressableCustomLoad(name);
                    if (startobj == null)
                    {
                        Debug.LogError("Object Name Error ! Object Name이 어드레서블에 없습니다. / " + " Name: " + name + " Type:" + type);
                        return;
                    }
                    UpdateCustom(startobj);

                    if (_facialHead != null && startobj.tag == Constants.AvartarPartHead)
                    {
                        GameObject emo_obj;
                        emo_obj = AddressableManager.AddressableCustomLoad(name.Substring(0, 10) + "Emo");
                        if (emo_obj == null) Debug.LogError("Object Name Error ! Object Name이 어드레서블에 없습니다. / " + " Name: " + name + " Type:" + type);
                        else UpdateCustom(emo_obj);
                    }
                    if (_facialEye != null && startobj.tag == Constants.AvartarPartHead)
                    {
                        GameObject emo_obj;
                        emo_obj = AddressableManager.AddressableCustomLoad(name.Substring(0, 10) + "Eye");
                        if (emo_obj == null) Debug.LogError("Object Name Error ! Object Name이 어드레서블에 없습니다. / " + " Name: " + name + " Type:" + type);
                        else UpdateCustom(emo_obj);
                    }

                    if (!string.IsNullOrEmpty(part[_jsonColor]?.ToString()))
                    {
                        string[] color = part[_jsonColor].ToString().Split(',');
                        if (color.Length == 1) SetCustomColor(color[0], null, type);
                        else if (color.Length == 2) SetCustomColor(color[0], color[1], type);
                    }
                }

                if (name != _lastObjectName && showAnimation)
                {
                    UpdateShopAnimation(type);
                    _lastObjectName = name;
                }

            }
            catch (System.Exception exception)
            {
                Debug.LogError(exception);
                Debug.LogError(exception.Message);
                throw;
            }
        }

        private void ResetCustom()
        {
            // 커스텀 초기화(악세사리 포함) 민몸/민머리 만 남도록 
            if (_currentHair) _currentHair.sharedMesh = null;
            if (_currentUpper) _currentUpper.sharedMesh = null;
            if (_currentPants) _currentPants.sharedMesh = null;
            if (_currentSocks) _currentSocks.sharedMesh = null;
            if (_currentShoes) _currentShoes.sharedMesh = null;
            if (_currentWatchMesh) _currentWatchMesh.sharedMesh = null;
            if (_currentBeardMesh) _currentBeardMesh.sharedMesh = null;
            if (_currentEarringRMesh) _currentEarringRMesh.sharedMesh = null;
            if (_currentEarringLMesh) _currentEarringLMesh.sharedMesh = null;
            if (_currentGlassesMesh) _currentGlassesMesh.sharedMesh = null;
            if (_currentBagHandMesh) _currentBagHandMesh.sharedMesh = null;
            if (_currentBagBackMesh) _currentBagBackMesh.sharedMesh = null;
            if (_currentBagCrossMesh) _currentBagCrossMesh.sharedMesh = null;
        }

        private void ResetHeadCustom()
        {
            // 커스텀 초기화(안경 귀걸이 수염) 민머리 헤어
        }

        public void StringToGameObjectUpdate(string stringObj)
        {
            GameObject currentObj = AddressableManager.AddressableCustomLoad(stringObj);
            UpdateCustom(currentObj);
        }


        public void UpdateCustom(GameObject obj)
        {
            bool BlendShape = true;

            //어드레서블 오브젝트 릴리즈용
            _changeParts.Add(obj);
            //매쉬,블랜드쉐잎 저장
            SkinnedMeshRenderer changePartsSkinnedMesh = new SkinnedMeshRenderer();
            if (obj.tag == Constants.AvartarPartBody || obj.tag == Constants.AvartarPartHead || obj.tag == Constants.AvartarPartEmoHead || obj.tag == Constants.AvartarPartEmoEye || obj.tag == Constants.AvartarPartHair)
            {
                changePartsSkinnedMesh = obj.GetComponentsInChildren<SkinnedMeshRenderer>()[0];
            }
            else if (Constants.AvatarBlendParts.Contains(obj.tag))
            {
                if (obj.GetComponentInChildren<MeshFilter>() != null)
                {
                    changePartsSkinnedMesh = obj.GetComponentsInChildren<SkinnedMeshRenderer>()[0];
                    _ResetBlendCheck[obj.tag] = true;
                    BlendShape = false;
                }
                else
                {
                    _ResetBlendCheck[obj.tag] = false;
                    changePartsSkinnedMesh = obj.GetComponentsInChildren<SkinnedMeshRenderer>()[1];
                }
            }
            //커스텀 파츠 판단 옷/악세사리
            if (Constants.AvatarParts.Contains(obj.tag))
            {
                //원피스 유무 판단
                if (obj.tag == Constants.AvartarPartTop || obj.tag == Constants.AvartarPartBottom) _hasOnepice = false;
                else if (obj.tag == Constants.AvartarPartOnepiece) _hasOnepice = true;

                //매쉬 본 재설정
                Transform[] newBones = new Transform[changePartsSkinnedMesh.bones.Length];

                for (int i = 0; i < changePartsSkinnedMesh.bones.Length; i++)
                {
                    foreach (Transform newBone in transform.Find("Root").GetComponentsInChildren<Transform>())
                    {
                        if (changePartsSkinnedMesh.bones[i].name == newBone.name)
                        {
                            newBones[i] = newBone;
                        }
                    }
                }

                //파츠 업데이트
                MainPartsUpdate(obj.tag, changePartsSkinnedMesh, newBones);
                //블랜드쉐잎 업데이트
                if (BlendShape == true) UpdateBlendShape(obj);
                else ResetBlendShape();
            }
            else if (Constants.AvatarAccessories.Contains(obj.tag))
            {
                AccessoriesUpdate(obj.tag, obj);
            }
            else
            {
                Debug.LogError("Obj Tag Error ! ObjectName : " + obj.name + " : " + obj.tag);
                return;
            }
        }

        private void MainPartsUpdate(string tag, SkinnedMeshRenderer changePartsSkinnedMesh, Transform[] newBones)
        {
            switch (tag)
            {
                case var value when value == Constants.AvartarPartHair:
                    _currentHair.bones = newBones;
                    _currentHair.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentHair.materials = changePartsSkinnedMesh.sharedMaterials;

                    break;
                case var value when value == Constants.AvartarPartTop:
                    _currentUpper.bones = newBones;
                    _currentUpper.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentUpper.materials = changePartsSkinnedMesh.sharedMaterials;
                    if (_hasOnepice == true)
                    {
                        _currentPants.sharedMesh = null;
                    }
                    break;
                case var value when value == Constants.AvartarPartBottom:
                    _currentPants.bones = newBones;
                    _currentPants.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentPants.materials = changePartsSkinnedMesh.sharedMaterials;
                    if (_hasOnepice == true)
                    {
                        _currentUpper.sharedMesh = null;
                    }
                    break;
                case var value when value == Constants.AvartarPartOnepiece:
                    _currentUpper.sharedMesh = null;
                    _currentPants.bones = newBones;
                    _currentPants.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentPants.materials = changePartsSkinnedMesh.sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartShoes:
                    _currentShoes.bones = newBones;
                    _currentShoes.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentShoes.materials = changePartsSkinnedMesh.sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartBody:
                    if (_currentBody == null) return;
                    _currentBody.bones = newBones;
                    _currentBody.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentBody.materials = changePartsSkinnedMesh.sharedMaterials;
                    _currentBody.SetPropertyBlock(mpb);
                    break;
                case var value when value == Constants.AvartarPartHead:
                    _currentHead.bones = newBones;
                    _currentHead.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentHead.materials = changePartsSkinnedMesh.sharedMaterials;
                    _currentHead.SetPropertyBlock(mpb);

                    if (_facialHead) _facialHead.bones = newBones;
                    if (_facialEye) _facialEye.bones = newBones;
                    break;
                case var value when value == Constants.AvartarPartSocks:
                    _currentSocks.bones = newBones;
                    _currentSocks.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _currentSocks.materials = changePartsSkinnedMesh.sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartEmoHead:
                    _facialHead.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _facialHead.materials = changePartsSkinnedMesh.sharedMaterials;
                    _facialHead.SetPropertyBlock(mpb);
                    break;
                case var value when value == Constants.AvartarPartEmoEye:
                    Debug.Log("Constants.AvartarPartEmoEye");
                    _facialEye.sharedMesh = changePartsSkinnedMesh.sharedMesh;
                    _facialEye.materials = changePartsSkinnedMesh.sharedMaterials;
                    _facialEye.SetPropertyBlock(mpb);
                    break;
            }
        }

        private void AccessoriesUpdate(string tag, GameObject obj)
        {
            switch (tag)
            {
                case var value when value == Constants.AvartarPartWatch:
                    _currentWatchMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    _currentWatchRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;

                    break;
                case var value when value == Constants.AvartarPartGlasses:
                    _currentGlassesMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    _currentGlassesRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartBeard:
                    _currentBeardMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    _currentBeardRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartEarring:
                    _currentEarringLMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    _currentEarringLRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    _currentEarringRMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    _currentEarringRRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    break;
                case var value when value == Constants.AvartarPartBag:
                    _currentBagBackMesh.sharedMesh = null;
                    _currentBagCrossMesh.sharedMesh = null;
                    _currentBagHandMesh.sharedMesh = null;
                    if (obj.name.Contains("Back"))
                    {
                        _currentBagBackMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                        _currentBagBackRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    }
                    else if (obj.name.Contains("Cross"))
                    {
                        _currentBagCrossMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                        _currentBagCrossRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    }
                    else if (obj.name.Contains("Hand"))
                    {
                        _currentBagHandMesh.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                        _currentBagHandRenderer.materials = obj.GetComponent<MeshRenderer>().sharedMaterials;
                    }
                    break;
            }
        }

        private void UndressCustom(string type)
        {
            switch (type)
            {
                case var value when value == Constants.AvartarPartWatch:
                    _currentWatchMesh.sharedMesh = null;
                    break;
                case var value when value == Constants.AvartarPartGlasses:
                    _currentGlassesMesh.sharedMesh = null;
                    break;
                case var value when value == Constants.AvartarPartBeard:
                    _currentBeardMesh.sharedMesh = null;
                    break;
                case var value when value == Constants.AvartarPartEarring:
                    _currentEarringLMesh.sharedMesh = null;
                    _currentEarringRMesh.sharedMesh = null;
                    break;
                case var value when value == Constants.AvartarPartBag:
                    _currentBagBackMesh.sharedMesh = null;
                    _currentBagCrossMesh.sharedMesh = null;
                    _currentBagHandMesh.sharedMesh = null;
                    break;
                case var value when value == Constants.AvartarPartShoes:
                    if (_currentBody == null) return;
                    _currentShoes.sharedMesh = null;
                    _ResetBlendCheck[type] = true;
                    ResetBlendShape();
                    break;
                case var value when value == Constants.AvartarPartSocks:
                    if (_currentBody == null) return;
                    _currentSocks.sharedMesh = null;
                    _ResetBlendCheck[type] = true;
                    ResetBlendShape();
                    break;
                case var value when value == Constants.AvartarPartHair:
                    _currentHair.sharedMesh = null;
                    break;
            }
        }

        string _blendShirtsName = "";
        string _blendPantsName = "";
        string _blendShoesName = "";
        string _blendSocksName = "";
        private Dictionary<string, bool> _ResetBlendCheck = new Dictionary<string, bool>();

        private void BlendReset(string blendName)
        {
            if (_currentBody.sharedMesh.GetBlendShapeIndex(blendName) < 0) return;
            int BlendNum = _currentBody.sharedMesh.GetBlendShapeIndex(blendName);
            // Debug.Log("BlendNum " + BlendNum);
            _currentBody.SetBlendShapeWeight(BlendNum, 0);
        }

        private void ResetBlendShape()
        {

            if (_ResetBlendCheck[Constants.AvartarPartTop] == true)
            {
                BlendReset(_blendShirtsName);
            }

            if (_ResetBlendCheck[Constants.AvartarPartBottom] == true)
            {
                BlendReset(_blendPantsName);
            }

            if (_ResetBlendCheck[Constants.AvartarPartSocks] == true)
            {
                BlendReset(_blendSocksName);
            }

            if (_ResetBlendCheck[Constants.AvartarPartShoes] == true)
            {
                BlendReset(_blendShoesName);
            }

        }

        private void UpdateBlendShape(GameObject obj)
        {
            if (_currentBody == null) return;
            if (obj.CompareTag(Constants.AvartarPartHead) || obj.CompareTag(Constants.AvartarPartHair)) return;

            Vector3[] deltaVertices = new Vector3[_currentBody.sharedMesh.vertexCount];
            Vector3[] deltaNormals = new Vector3[_currentBody.sharedMesh.vertexCount];
            Vector3[] deltaTangents = new Vector3[_currentBody.sharedMesh.vertexCount];

            Mesh currentBlendMesh = obj.GetComponentsInChildren<SkinnedMeshRenderer>()[0].sharedMesh;
            if (obj.CompareTag(Constants.AvartarPartBottom) || obj.CompareTag(Constants.AvartarPartOnepiece))
            {
                _currentCustom[Constants.AvartarPartBottom] = currentBlendMesh;
                _blendPantsName = currentBlendMesh.GetBlendShapeName(0);
                if (_hasOnepice == true && obj.tag == Constants.AvartarPartBottom)
                {
                    _hasOnepice = false;
                    StringToGameObjectUpdate("G_Shirts_00_1");
                }
                else if (_hasOnepice == true && obj.tag == Constants.AvartarPartOnepiece)
                {
                    _currentCustom[Constants.AvartarPartTop] = null;
                }

            }
            else if (obj.CompareTag(Constants.AvartarPartShoes))
            {
                _currentCustom[Constants.AvartarPartShoes] = currentBlendMesh;
                _blendShoesName = currentBlendMesh.GetBlendShapeName(0);

            }
            else if (obj.CompareTag(Constants.AvartarPartSocks))
            {
                _currentCustom[Constants.AvartarPartSocks] = currentBlendMesh;
                _blendSocksName = currentBlendMesh.GetBlendShapeName(0);

            }
            else if (obj.CompareTag(Constants.AvartarPartTop))
            {
                _currentCustom[Constants.AvartarPartTop] = currentBlendMesh;
                _blendShirtsName = currentBlendMesh.GetBlendShapeName(0);
                if (_hasOnepice == true)
                {
                    _hasOnepice = false;
                    StringToGameObjectUpdate("G_Pants_00_1");
                }
            }

            Mesh BodyMesh = _bodyMesh.mesh;
            //Mesh mainBodyMesh = _currentBody.sharedMesh;

            BodyMesh.ClearBlendShapes();
            foreach (Mesh custom_blendmesh in _currentCustom.Values)
            {
                if (custom_blendmesh == null) continue;
                string shapeName = custom_blendmesh.GetBlendShapeName(0);

                int frameCount = custom_blendmesh.GetBlendShapeFrameCount(0);
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    float frameWeight = custom_blendmesh.GetBlendShapeFrameWeight(0, frameIndex);
                    custom_blendmesh.GetBlendShapeFrameVertices(0, frameIndex, deltaVertices, deltaNormals, deltaTangents);

                    BodyMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                }
            }
            _currentBody.sharedMesh = BodyMesh;
            for (int i = 0; i < BodyMesh.blendShapeCount; i++)
            {
                _currentBody.SetBlendShapeWeight(i, 100);
            }
            ResetBlendShape();

        }

        private void SetDefaultCustom()
        {
            foreach (string name in _defaultCustom)
            {
                GameObject startobj = AddressableManager.AddressableCustomLoad(name);
                UpdateCustom(startobj);
            }
        }

        public void SetCustomColor(string hexCode1, string hexCode2, string objTypeName)
        {
            // Util.Log("SetCustomColor", objTypeName, hexCode1, hexCode2);
            if (hexCode1 == "")
            {

                if (objTypeName == Constants.AvartarPartBody)
                {
                    if (_currentBody == null) return;
                    Debug.LogError("Body hexCode1 Null");
                    Color HeadColor = _currentHead.sharedMaterials[0].GetColor("_BaseColor");
                    _currentBody.sharedMaterials[0].SetColor("_BaseColor", HeadColor);
                }
                else if (objTypeName == Constants.AvartarPartHead)
                {
                    Debug.LogError("Head hexCode1 Null");
                    Color BodyColor = _currentBody.sharedMaterials[0].GetColor("_BaseColor");
                    _currentHead.sharedMaterials[0].SetColor("_BaseColor", BodyColor);
                }
                return;
            }

            Color HexToColor1;
            if (ColorUtility.TryParseHtmlString(hexCode1, out HexToColor1))
            {
                if (objTypeName.Contains(Constants.AvartarPartBody) || objTypeName.Contains(Constants.AvartarPartHead))
                {
                    if (_currentBody) _currentBody.materials[0].SetColor("_BASE_COLOR", HexToColor1);
                    if (_currentHead) _currentHead.materials[0].SetColor("_BASE_COLOR", HexToColor1);
                    if (_facialHead) _facialHead.materials[0].SetColor("_BASE_COLOR", HexToColor1);
                }
                else if (objTypeName.Contains(Constants.AvartarPartHair))
                {
                    Color HexToColor2;
                    //헤어 단색 적용
                    if (hexCode2 == null || hexCode2 == "")
                    {
                        HexToColor2 = HexToColor1;
                        foreach (var Hairmeterial in _currentHair.materials)
                        {
                            Hairmeterial.SetColor("_Color1", HexToColor1);
                            Hairmeterial.SetColor("_Color2", HexToColor2);
                        }
                    }
                    else
                    {
                        //헤어 그라데이션 적용
                        ColorUtility.TryParseHtmlString(hexCode2, out HexToColor2);
                        foreach (var Hairmeterial in _currentHair.materials)
                        {
                            Hairmeterial.SetColor("_Color1", HexToColor1);
                            Hairmeterial.SetColor("_Color2", HexToColor2);
                        }
                    }

                }
                else if (objTypeName.Contains(Constants.AvartarPartBeard))
                {
                    _currentBeardRenderer.materials[0].SetColor("_BASE_COLOR", HexToColor1);
                }
                else
                {
                    // Debug.LogError("Color Error ! Type:" + objTypeName + "  Color값은 Body/Head/Hair/Beard만 넣어주세요");
                    return;
                }
            }
        }

        private void UpdateShopAnimation(string type)
        {
            if (Character == null) return;
            _shopAnimation = Character.GetComponent<Animator>();

            if (!(_shopAnimation.GetCurrentAnimatorStateInfo(0).IsName("Idle01"))) return;

            switch (type)
            {
                case var value when value == Constants.AvartarPartHair:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Hair_Animation");
                    break;
                case var value when value == Constants.AvartarPartTop:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Top_Animation");
                    break;
                case var value when value == Constants.AvartarPartBottom:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Bottom_Animation");
                    break;
                case var value when value == Constants.AvartarPartShoes:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Shoes_Animation");
                    break;
                case var value when value == Constants.AvartarPartOnepiece:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 3));
                    _shopAnimation.Play("Onpiece_Animation");
                    break;
                case var value when value == Constants.AvartarPartWatch:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Watch_Animation");
                    break;
                case var value when value == Constants.AvartarPartBag:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 1));
                    _shopAnimation.Play("Bag_Animation");
                    break;
                case var value when value == Constants.AvartarPartEarring:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Earring_Animation");
                    break;
                case var value when value == Constants.AvartarPartBeard:
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Beard_Animation");
                    break;
                case "buy":
                    _shopAnimation.SetFloat("Blend", Random.Range(0, 2));
                    _shopAnimation.Play("Buy_Animation");
                    break;
            }
        }

        public void Release()
        {
            AddressableManager.ReleaseArray(_changeParts);
            Addressables.Release(_loadHandle);
        }
    }
}