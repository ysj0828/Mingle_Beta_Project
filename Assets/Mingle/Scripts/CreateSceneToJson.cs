using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Mingle;

public class CreateSceneToJson : MonoBehaviour
{
    // Start is called before the first frame update

    List<string> exceptionNames = new List<string>(){
        "Main Camera",
        "[Debug Updater]",
        "PhotonMono",
    };

// [MenuItem("Tools/GameObjectToJson")]
    void GameObjectToJson()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
        JObject json = new JObject();
        JArray objects = new JArray();
        
        List<ObjectData> objectsDatas = new List<ObjectData>();
        // ObjectData[] objectsDatas = new objectData[]();

        foreach(GameObject go in allObjects)
        {
            if(exceptionNames.Contains(go.name) || go.transform.parent!=null) continue;

            ObjectData objectsData = new ObjectData(
                go.name,
                go.transform.position,
                go.transform.rotation,
                go.transform.localScale,
                null, 0,0,0
            );

            objectsDatas.Add(objectsData);
            

            // Debug.Log(go.name + ": " + go.transform.parent);
            // JObject objectJson = new JObject();
            // Vector3 position = go.transform.position;
            // JObject positionJson = new JObject();
            // Quaternion rotation = go.transform.rotation;
            // JObject rotationJson = new JObject();
            // Vector3 scale = go.transform.localScale;
            // JObject scaleJson = new JObject();
            // objectJson["name"] = go.name;
            // positionJson["x"] = position.x;
            // positionJson["y"] = position.y;
            // positionJson["z"] = position.z;
            // objectJson["position"] = positionJson;
            // rotationJson["x"] = rotation.x;
            // rotationJson["y"] = rotation.y;
            // rotationJson["z"] = rotation.z;
            // rotationJson["w"] = rotation.z;
            // objectJson["rotation"] = rotationJson;
            // scaleJson["x"] = scale.x;
            // scaleJson["y"] = scale.y;
            // scaleJson["z"] = scale.z;
            // objectJson["scale"] = scaleJson;
            // // Debug.Log(JsonConvert.SerializeObject(roomObject, Formatting.None));            
            // objects.Add(objectJson);
        }

        Debug.Log(objectsDatas.ToArray());

        Debug.Log(JsonUtility.ToJson(new Objects(objectsDatas.ToArray())));

        // json["objects"] = objects;
        // Debug.Log(JsonConvert.SerializeObject(json, Formatting.None));
        
        // Debug.Log(JsonUtility.ToJson(objects));
    }
}
