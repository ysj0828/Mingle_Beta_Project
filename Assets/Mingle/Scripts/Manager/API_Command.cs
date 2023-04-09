using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 밍글에서 사용 하는  상수를 관리하는 클래스

namespace Mingle
{
  public static class API_Command
  {

    public static string GetFriends = "/Friends/";                    // 친구조회
    public static string GetAvatarObjcets = "/avatars/{0}/objects/";  // 아바타 조회
    public static string PutAvatarObjects = "/avatars/{0}/objects/";  // 아바타 수정
    public static string GetRoomObjects = "/rooms/{0}/objects/";      // 방 오브젝트 조회
    public static string PutRoomObjects = "/rooms/{0}/objects/";      // 방 오브젝트 수정
  }
}