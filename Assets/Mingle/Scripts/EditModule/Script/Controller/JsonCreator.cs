using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mingle;
using UnityEngine;

public class JsonCreator : MonoBehaviour
{
    public Objects[] objects = new Objects[3];

    private void Awake()
    {
        //Debug.Log(Application.persistentDataPath);
        JsonSaver();

        //objects[0] = LoadJson(gameObject.name);
        // for(int i = 0; i < 8; i++)
            // SaveJson(i.ToString(), objects[i]);

        //SaveJson("10", objects[0]);
        //SaveJson("20", objects[1]);
        //SaveJson("30", objects[2]);

        //objects[0] = LoadJson("10");
        //objects[1] = LoadJson("20");
        //objects[2] = LoadJson("30");
    }

    public void JsonSaver()
    {
        List<ObjectData> objcets = new List<ObjectData>();
        Transform[] data = transform.GetComponentsInChildren<Transform>();


        foreach (Transform i in data)
        {
            if (i != transform)
            {
                if(i.transform.parent!=transform) continue;
                // Debug.Log(i.name + " : " + (i.transform.parent==transform));
                ObjectManager tempOM = i.GetComponent<ObjectManager>();
                if (tempOM != null)
                {
                    objcets.Add(new ObjectData(i.name.Replace("(Clone)", ""), RoundVector(i.transform.position), RoundQuaternion(i.transform.rotation), RoundVector(i.transform.lossyScale), tempOM.GetID().shop_object_id, tempOM.GetID().inventory_object_id, tempOM.GetID().preset_detail_id, tempOM.GetID().room_preset_detail_id));
                }
                else
                {
                    string names = i.name;
                    for(int j = 0; j < 20; j++)
                    {
                        names = names.Replace(" (" + j + ")", "");
                    }

                    objcets.Add(new ObjectData(names, RoundVector(i.transform.position), RoundQuaternion(i.transform.rotation), RoundVector(i.transform.lossyScale), null,0,0,0));
                }
            }
        }

        Objects tempObjects = new Objects(objcets.ToArray());

        SaveJson(transform.name, new Objects(objcets.ToArray()));
    }

    public Vector3 RoundVector(Vector3 vector)
    {
        float x = (float)Math.Round(vector.x, 2);
        float y = (float)Math.Round(vector.y, 2);
        float z = (float)Math.Round(vector.z, 2);
        // Debug.Log(x +","+ y +","+ z);
        return new Vector3(x, y, z);
    }

    public Quaternion RoundQuaternion(Quaternion quaternion)
    {
        float x = (float)Math.Round(quaternion.x, 2);
        float y = (float)Math.Round(quaternion.y, 2);
        float z = (float)Math.Round(quaternion.z, 2);
        float w = (float)Math.Round(quaternion.w, 2);
        
        return new Quaternion(x, y, z, w);
    }

    public void SaveJson(string name, Objects objects)
    {
        Debug.Log(Application.persistentDataPath);
        File.WriteAllText(Application.persistentDataPath + "/" + name + ".json", String.Empty);
        StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/" + name + ".json", true);
        writer.WriteLine(JsonUtility.ToJson(objects));
        writer.Close();
    }

    public Objects LoadJson(string name)
    {
        FileStream fileStream = new FileStream(Application.persistentDataPath + "/" + name + ".json", FileMode.Open);
        byte[] data = new byte[fileStream.Length];
        fileStream.Read(data, 0, data.Length);
        fileStream.Close();
        string json = Encoding.UTF8.GetString(data);

        Objects objects = JsonUtility.FromJson<Objects>(json);

        return objects;
    }
}
