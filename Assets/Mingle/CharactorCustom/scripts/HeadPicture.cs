using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.IO;

namespace Mingle
{
    public class HeadPicture : MonoBehaviour
    {

        public Camera _recordCamera;
        public int PhotoNumber = 0;

        private int _targetResolution = 480;

        private bool _check = false;

        string path = null;

        string _cpaturePath = null;

        //선언 부분
        RenderTexture _recordTexture;

        private void Start()
        {
            _cpaturePath = Application.persistentDataPath + "/capture/";
            //awake 함수
            _recordTexture = new RenderTexture(_targetResolution, _targetResolution, 16);
            _recordCamera.targetTexture = _recordTexture;
        }

        public string HeadPicturePhoto()
        {
            //update 함수
            RenderTexture.active = _recordTexture;
            Texture2D texture = new Texture2D(_targetResolution, _targetResolution, TextureFormat.RGBA32, false, false);
            texture.ReadPixels(new Rect(0, 0, _targetResolution, _targetResolution), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            Debug.Log(_cpaturePath);
            if (!System.IO.Directory.Exists(_cpaturePath))
            {
                System.IO.Directory.CreateDirectory(_cpaturePath);
            }
            path = _cpaturePath + PhotoNumber + ".png";
            Debug.Log("저장 : " + path);

            byte[] currentPNG = texture.GetRawTextureData();

            byte[] resultPNG = ImageConversion.EncodeArrayToPNG(currentPNG, GraphicsFormat.R8G8B8A8_SRGB, (uint)_targetResolution, (uint)_targetResolution);
            //PC 사진 저장
            File.WriteAllBytes(path, resultPNG);
            return path;
        }
    }
}
