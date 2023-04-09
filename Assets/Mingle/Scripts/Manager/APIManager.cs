using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Mingle
{
    // API서버와 통신 하는 모듈
    public class APIManager
    {
        // public string Token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1c2VyX2lkIjoiYzMzNTQxNGFiMjk1NDE5OGI0YTE1NTA4ODdkYzllOWMiLCJpc3MiOiIrODIxMDU1NTE2MjAxIiwiaWF0IjoxNjY5MzQ4NDcwLCJleHAiOjQ3OTM0ODYwNzB9.aSrHuYu91sdnzRjxudomtRPZcAiLdaemU6qGIJGV8d4";
        public string Get(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(Constants.APIServerAddress + url);
            return Request(request);
        }

        public string Put(string url, JObject json)
        {
            byte[] bodyData = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json, Formatting.None).ToString());
            UnityWebRequest request = UnityWebRequest.Put(Constants.APIServerAddress + url, bodyData);
            return Request(request);
        }

        public string Post(string url, JObject json)
        {
            string postData = JsonConvert.SerializeObject(json, Formatting.None);
            UnityWebRequest request = UnityWebRequest.Post(Constants.APIServerAddress + url, postData);
            return Request(request);
        }


        private string Request(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + GameManager.Instance.Token);

            request.timeout = Constants.APIServerTimeout;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone) Util.Log(request.uri.ToString(), operation.progress.ToString());

            if (request.result > UnityWebRequest.Result.Success)
            {
                Util.LogError("RequestError", request.responseCode.ToString(), request.error);
                return null;
            }

            return request.downloadHandler.text;
        }
    }
}
