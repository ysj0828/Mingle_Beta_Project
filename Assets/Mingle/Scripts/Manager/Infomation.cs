using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Mingle
{
    // 정보 저장용 모듈 GameManager에서 사용
    public class Infomation
    {
        public string NickName { get; set; }    // 사용자 표시 이름
        public string Token { get; set; }       // 사용자 토큰
        public float Volume { get; set; }       // 볼륨
        public string AnimationString { get; set; } //애니메이션 이름

        // 채팅방 접속용
        public string RoomID;
        public JObject CharacterInfo = new JObject();
        // public string AgitName;
        public string AgitJsonString = null;
    }
}