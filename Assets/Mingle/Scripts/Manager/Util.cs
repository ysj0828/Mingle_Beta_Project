using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Mingle
{
    // public delegate void EventHandler(object sender, string msg);
    public delegate void VoidEventHandler();
    public delegate void StringEventHandler(string msg);
    public delegate void PlayerEventHandler(Player msg);
    public delegate void JsonEventHandler(JObject json);

    public static class Util
    {
        public static void Log(string message)
        {
            if (Constants.IsDebug) Debug.Log(message);
        }
        public static void Log(params string[] messages)
        {
            if (Constants.IsDebug) Debug.Log(string.Join(",", messages));
        }
        public static void LogError(string message)
        {
            if (Constants.IsDebug) Debug.LogError(message);
        }
        public static void LogError(params string[] messages)
        {
            if (Constants.IsDebug) Debug.LogError(string.Join(",", messages));
        }
    }
}