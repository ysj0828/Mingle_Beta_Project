using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mingle
{
  public static class FileHandler
  {

    public static void SaveToJSON<T>(List<T> toSave, string filename)
    {
      Debug.Log(GetPath(filename));
      string content = JsonHelper.ToJson<T>(toSave.ToArray());
      WriteFile(GetPath(filename), content);
    }

    public static void SaveToJSON<T>(T toSave, string filename)
    {
      string content = JsonUtility.ToJson(toSave);
      WriteFile(GetPath(filename), content);
    }

    private static string GetPath(string filename)
    {
      return Application.persistentDataPath + "/" + filename;
    }

    private static void WriteFile(string path, string content)
    {
      FileStream fileStream = new FileStream(path, FileMode.Create);

      using (StreamWriter writer = new StreamWriter(fileStream))
      {
        writer.Write(content);
      }
    }
  }

  public static class JsonHelper
  {
    public static T[] FromJson<T>(string json)
    {
      Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
      return wrapper.objects;
    }

    public static string ToJson<T>(T[] array)
    {
      Wrapper<T> wrapper = new Wrapper<T>();
      wrapper.objects = array;
      return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
      Wrapper<T> wrapper = new Wrapper<T>();
      wrapper.objects = array;
      return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
      public T[] objects;
    }
  }
}