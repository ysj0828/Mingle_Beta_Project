using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

// RN에 메시지 전달하는 모듈
namespace Mingle
{
    public class NativeAPI
    {
#if UNITY_IOS && !UNITY_EDITOR
          [DllImport("__Internal")]
          public static extern void sendMessageToMobileApp(string message);
#endif
    }

    public class RNMessenger : MonoBehaviour
    {

        static public void SendToRN(string message)
        {
            Util.Log("SendToRN : ", message);
            if (Application.platform == RuntimePlatform.Android)
            {
#if !UNITY_EDITOR
                using (AndroidJavaClass jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
                {
                    jc.CallStatic("sendMessageToMobileApp", message);
                }
#endif
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if UNITY_IOS && !UNITY_EDITOR
                        NativeAPI.sendMessageToMobileApp(message);
#endif
            }
        }

        public static void SendCmd(string cmd)
        {
            JObject json = new JObject();
            json["cmd"] = cmd;
            SendToRN(JsonConvert.SerializeObject(json, Formatting.None));
        }

        // JSON타입 전달
        public static void SendJson(JObject json)
        {
            SendToRN(JsonConvert.SerializeObject(json, Formatting.None));
        }

        // string 데이터를 포함한 커맨드 결과 전달 
        public static void SendResult(JObject orignal_json, bool result, string message = "")
        {
            JObject json = new JObject();

            if (orignal_json.ContainsKey("cmd")) json["cmd"] = orignal_json["cmd"];
            if (orignal_json.ContainsKey("cmdId")) json["cmdId"] = orignal_json["cmdId"];
            json["result"] = result ? "success" : "fail";
            if (!string.IsNullOrEmpty(message)) json["message"] = message;

            SendToRN(JsonConvert.SerializeObject(json, Formatting.None));
        }

        // JSON 데이터를 포함한 커맨드 결과 전달 
        public static void SendResult(JObject orignal_json, bool result, JObject param)
        {
            JObject json = new JObject();

            if (orignal_json.ContainsKey("cmd")) json["cmd"] = orignal_json["cmd"];
            if (orignal_json.ContainsKey("cmdId")) json["cmdId"] = orignal_json["cmdId"];
            json["result"] = result ? "success" : "fail";
            json["params"] = param;

            SendToRN(JsonConvert.SerializeObject(json, Formatting.None));
        }
    }
}
