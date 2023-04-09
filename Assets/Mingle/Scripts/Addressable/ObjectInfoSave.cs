using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mingle
{
    [System.Serializable]
    public class ObjectData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public string shop_object_id = null;
        public int inventory_object_id = 0;
        public int preset_detail_id = 0;
        public int room_preset_detail_id = 0;

        public ObjectData(string name, Vector3 position, Quaternion rotation, Vector3 scale, string shop_object_id, int inventory_object_id=0, int preset_detail_id=0, int room_preset_detail_id=0)
        {
            this.name = name;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.shop_object_id = shop_object_id;
            this.inventory_object_id = inventory_object_id;
            this.preset_detail_id = preset_detail_id;
            this.room_preset_detail_id = room_preset_detail_id;
        }
    }

    [System.Serializable]
    public class Objects
    {
        public ObjectData[] objects;

        public Objects(ObjectData[] objects)
        {
            this.objects = objects;
        }
    }

    public class ObjectInfoSave : MonoBehaviour
    {
        SaveManager manager;

        void Awake()
        {
            manager = FindObjectOfType<SaveManager>();
            manager.objectInfoList.Add(new ObjectData(gameObject.name, transform.position, transform.rotation, transform.localScale, null, 0, 0, 0));
        }
    }
}