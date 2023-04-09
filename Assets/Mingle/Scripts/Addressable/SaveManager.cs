using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mingle
{
  public class SaveManager : MonoBehaviour
  {
    public List<ObjectData> objectInfoList = new List<ObjectData>();

    private void Start()
    {
      Objects objects = new Objects(objectInfoList.ToArray());
      FileHandler.SaveToJSON(objects, "objInfo");
    }
  }
}