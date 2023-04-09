using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

namespace Mingle
{
  public class Ingame : MonoBehaviour
  {
    JArray _objects;
    JArray _objectInfo;
    private string localJsonData = null;
    private string localObjectJsonData = null;

    TextAsset testJson;

    GameObject obj;
    List<GameObject> listObj = new List<GameObject>();

    private void Awake()
    {
      string _localVersionFilePath = Path.Combine(Application.persistentDataPath, "objInfo");

      if (File.Exists(_localVersionFilePath))
      {
        localJsonData = File.ReadAllText(_localVersionFilePath);
      }
      JsonLoad();
    }

    public void JsonLoad()
    {
      if (localJsonData != null)
      {
        JObject json = JObject.Parse(localJsonData);//string
        _objects = json["objects"].ToObject<JArray>();

        foreach (JObject item in _objects)
        {
          string name = item["name"].ToString();
          JObject position = item["position"].ToObject<JObject>();
          JObject rotation = item["rotation"].ToObject<JObject>();

          Vector3 objPos = new Vector3(position["x"].ToObject<float>(), position["y"].ToObject<float>(), position["z"].ToObject<float>());
          Quaternion objRot = new Quaternion(rotation["x"].ToObject<float>(), rotation["y"].ToObject<float>(), rotation["z"].ToObject<float>(), rotation["w"].ToObject<float>());

          AddressableManager.AddressableInsLoad(name, objPos, objRot, out obj);
          listObj.Add(obj);
        }
      }
    }

    public void ShopIns()
    {
      string reactObjname = "3";
      bool isHave = false;

      foreach (GameObject item in listObj)
      {
        if (item.name == reactObjname)
        {
          isHave = true;
        }
      }

      if (isHave)
      {
        AddressableManager.AddressableInsLoad(reactObjname, Vector3.zero, Quaternion.identity, out obj);
        listObj.Add(obj);
        isHave = false;
        Debug.Log("생성");
      }
      Debug.Log("이미 존재");
    }

    public void Release()
    {
      AddressableManager.ReleaseArrayIns(listObj);
    }
  }
}