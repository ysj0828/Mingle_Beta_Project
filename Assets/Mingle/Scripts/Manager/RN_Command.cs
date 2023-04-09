using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 밍글에서 사용 하는  상수를 관리하는 클래스

namespace Mingle
{
  public static class RN_Command
  {
    public static string RequestToken = "RequestToken";             // API 토큰 요청
    // Facial Record
    public static string OnStartRecorod = "OnStartRecorod";
    public static string OnEndRecord = "OnEndRecord";
    public static string OnStartEncoding = "OnStartEncoding";
    public static string OnProgressEncoding = "OnProgressEncoding";
    public static string OnEndEncoding = "OnEndEncoding";
  }
}