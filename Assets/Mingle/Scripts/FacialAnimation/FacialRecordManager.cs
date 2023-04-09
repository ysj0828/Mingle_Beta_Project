using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
//using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
//using Mediapipe;
using Mediapipe.Unity;
using UnityEngine.Android;
using Mediapipe.Unity.Tutorial;

namespace Mingle
{
    public class FacialRecordManager : MonoBehaviour
    {
        public bool IsPlaying = false;
        public int _targetResolution = 480;
        public int _targetRecordSec = 3;
        public int _targetFrameRate = 30;
        public int EmoIndex;
        public string _saveFrolderName = "/ScreenRecorder/";
        public Image LoadingBar;
        public Image BackShadow;
        public Transform HeadRotate;
        public SkinnedMeshRenderer Head3D;
        public SkinnedMeshRenderer Head2D;
        public GameObject Face3D;
        public GameObject EmotionBtn = null;

        float _lastCaptureFrameTime;
        float _progressTime;
        int _currentCaptureFrameNumber;
        int _num = 0;
        string _recorodPath = "";
        bool _isRecording = false;
        bool _pngOrder = true;
        Camera _recordCamera;
        Thread _recordThread;
        Stopwatch _stopWatch = new Stopwatch();
        RenderTexture _recordTexture;

        List<Texture2D> _pngForPlay = new List<Texture2D>();
        List<bool> _currentFace2DPlaying = new List<bool>();
        List<byte[]> _pngForPlayByte = new List<byte[]>();
        List<int> _currentFace2D = new List<int>();
        List<List<float>> _capptureBlendShapeWeights = new List<List<float>>();
        List<Quaternion> _currentHeadRotation = new List<Quaternion>();
        //Queue<byte[]> _frameQueue = new Queue<byte[]>();

        newRotation _headRotationManager = null;
        public ButtonEvent _buttonManager = null;

        private void Awake()
        {
            if (RNManager.Instance) RNManager.Instance.OnFacialRecordEvent += OnFacialRecordEvent;

            newRotation[] headRotationManagers = FindObjectsOfType<newRotation>();
            if (headRotationManagers.Length > 0) _headRotationManager = FindObjectsOfType<newRotation>()[0];

            // ButtonEvent[] buttonMnagers = FindObjectsOfType<ButtonEvent>();
            // if (buttonMnagers.Length > 0) _buttonManager = FindObjectsOfType<ButtonEvent>()[0];

            Application.targetFrameRate = _targetFrameRate;
            _recorodPath = Application.persistentDataPath + _saveFrolderName;
            _recordCamera = GetComponent<Camera>();
            if (!System.IO.Directory.Exists(_recorodPath))
            {
                System.IO.Directory.CreateDirectory(_recorodPath);
            }

            _recordTexture = new RenderTexture(_targetResolution, _targetResolution, 16);
            _recordCamera.targetTexture = _recordTexture;
            _currentCaptureFrameNumber = 0;
            LoadingBar.enabled = false;
        }
        void OnDestroy()
        {
            if (RNManager.Instance) RNManager.Instance.OnFacialRecordEvent -= OnFacialRecordEvent;
        }

        void OnFacialRecordEvent(JObject json)
        {
            if (!string.IsNullOrEmpty(json["cmd"]?.ToString()))
            {
                StartCoroutine(json["cmd"].ToString(), json);//Invoke(json["cmd"].ToString(), 0.0f);
            }
            else
            {
                Util.LogError("FacialRecordManager cmd Error ", JsonConvert.SerializeObject(json, Formatting.None));
                RNMessenger.SendResult(json, false, "cmd is null or empty");
            }
        }

        JObject _recordJson = new JObject();

        public void StartButton(string command)
        {
            JObject json = new JObject();
            if (command == "record") StartCoroutine("StartRecord", json);
            if (command == "reset") StartCoroutine("ResetRecord", json);
            if (command == "stop") StartCoroutine("StopAndResetRecord", json);
            if (command == "capture") StartCoroutine("CaptureOneFrame", json);
        }


        IEnumerator StartRecord(JObject json)
        {
            if (_isRecording) yield break;
            print("start");
            LoadingBar.enabled = true;
            _stopWatch.Start();
            _isRecording = true;
            _currentCaptureFrameNumber = 0;
            _lastCaptureFrameTime = Time.time;
            _capptureBlendShapeWeights.Clear();
            _currentHeadRotation.Clear();
            _currentFace2D.Clear();
            _currentFace2DPlaying.Clear();
            _recordJson = json;
            RNMessenger.SendResult(json, true);
            UpdateRecordState(true);
            //_recordThread = new Thread(FrameSaveWorker);
            //_recordThread.Start();
            yield break;
        }

        IEnumerator StopRecord(JObject json)
        {
            StartCoroutine("StopAndResetRecord", json);
            yield break;
        }


        void UpdateRecordState(bool isRunning)
        {

            JObject resultJson = new JObject();
            if (_recordJson.ContainsKey("cmd")) resultJson["cmd"] = _recordJson["cmd"]; ;
            if (_recordJson.ContainsKey("cmdId")) resultJson["cmdId"] = _recordJson["cmdId"];
            resultJson["state"] = isRunning ? "Started" : "Ended";
            resultJson["path"] = _recorodPath + "combine.png";
            resultJson["result"] = "success";
            //   JObject param = new JObject();
            //   param["state"] = isRunning ? "Started" : "Ended";
            //   resultJson["params"] = param;

            RNMessenger.SendJson(resultJson);

        }

        IEnumerator ResetRecord(JObject json) // 새로고침 버튼 && 녹화 중 취소 버튼 
        {
            BackShadow.enabled = true;
            //RootHead.SetActive(true);
            _pngForPlay.Clear();
            _pngForPlayByte.Clear();
            _capptureBlendShapeWeights.Clear();
            _currentHeadRotation.Clear();
            _currentFace2D.Clear();
            _currentFace2DPlaying.Clear();
            if (EmotionBtn) EmotionBtn.SetActive(true);
            _isRecording = false;
            IsPlaying = false;
            _num = 0;
            _progressTime = 0.0f;
            LoadingBar.fillAmount = 0;
            _pngOrder = true;
            _buttonManager.Face();
            RNMessenger.SendResult(json, true);
            yield break;
        }

        IEnumerator StopAndResetRecord(JObject json) // 녹화 중 취소 버튼 
        {
            _pngForPlay.Clear();
            _pngForPlayByte.Clear();
            _capptureBlendShapeWeights.Clear();
            _currentHeadRotation.Clear();
            _currentFace2D.Clear();
            _currentFace2DPlaying.Clear();
            _isRecording = false;
            IsPlaying = false;
            _progressTime = 0.0f;
            LoadingBar.fillAmount = 0;
            _buttonManager.Face();
            RNMessenger.SendResult(json, true);
            yield break;
        }

        IEnumerator CaptureOneFrame(JObject json)
        {
            RenderTexture.active = _recordTexture;
            Texture2D texture = new Texture2D(_targetResolution, _targetResolution, TextureFormat.RGBA32, false, false);
            texture.ReadPixels(new Rect(0, 0, _targetResolution, _targetResolution), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            byte[] capturedPNG = ImageConversion.EncodeArrayToPNG(texture.GetRawTextureData(), GraphicsFormat.R8G8B8A8_SRGB, (uint)_targetResolution, (uint)_targetResolution);
            File.WriteAllBytes(_recorodPath + "capture.png", capturedPNG);

            yield break;
        }

        IEnumerator UpdateAnimationEye(JObject json)
        {
            JObject param = json["params"].ToObject<JObject>();

            if (!param.ContainsKey("index"))
            {
                RNMessenger.SendResult(json, false, "index is required");
                yield break;
            }

            switch (param["index"].ToObject<int>())
            {
                case 0:
                    _buttonManager.Face();
                    break;
                case 1:
                    _buttonManager.Surprised();
                    break;
                case 2:
                    _buttonManager.Touched();
                    break;
                case 3:
                    _buttonManager.Angry();
                    break;
                case 4:
                    _buttonManager.Happy();
                    break;
            }
            RNMessenger.SendResult(json, true);
            yield break;
        }

        private void Update()
        {
            if (_isRecording) //progress bar 채우
            {
                _progressTime += Time.deltaTime;
                LoadingBar.fillAmount = _progressTime / (float)_targetRecordSec;
                // print("time : " + Time.deltaTime);
            }
        }


        private void FixedUpdate()
        {
            if (_isRecording)
            {
                float thisFrameTime = Time.time;
                float targetSec = 1.0f / _targetFrameRate;
                int capture = ((int)(thisFrameTime / targetSec)) - ((int)(_lastCaptureFrameTime / targetSec));
                if (capture > 0) StartCoroutine("SaveReadPixels");
                _lastCaptureFrameTime = thisFrameTime;

            }

            //BlendShape Repeat

            // print("IsPlaying " + IsPlaying);
            if (IsPlaying)
            {
                float thisFrameTime = Time.time;
                float targetSec = 1.0f / _targetFrameRate;
                int capture = ((int)(thisFrameTime / targetSec)) - ((int)(_lastCaptureFrameTime / targetSec));
                if (capture > 0)
                {
                    // print("num : " + _num);
                    if (_currentHeadRotation.Count <= _num) return;
                    HeadRotate.localRotation = _currentHeadRotation[_num];
                    if (!_currentFace2DPlaying[_num])
                    {
                        switch (_currentFace2D[_num])
                        {
                            case 0:
                                _buttonManager.Surprised();
                                break;
                            case 1:
                                _buttonManager.Angry();
                                break;
                            case 2:
                                _buttonManager.Touched();
                                break;
                            case 3:
                                _buttonManager.Happy();
                                break;
                        }
                    }
                    else _buttonManager.Face();
                    UpdateCaptureBlendShapeWeights(_num);
                    if (_pngOrder)
                    {
                        _num++;
                        if (_num == 89) _pngOrder = false;
                    }
                    else
                    {
                        _num--;
                        if (_num == 0) _pngOrder = true;
                    }
                    _lastCaptureFrameTime = thisFrameTime;
                }


            }

        }


        IEnumerator SaveReadPixels()
        {
            yield return new WaitForEndOfFrame();


            // 캡처프레임 확인 후 종료
            if (_currentCaptureFrameNumber >= _targetFrameRate * _targetRecordSec)
            {
                StartCoroutine("PlayPNG");
                _progressTime = 0.0f;
                _isRecording = false;
                //JObject json = new JObject();
                //json["cmd"] = RN_Command.OnEndRecord;
                //json["path"] = _recorodPath;
                if (EmotionBtn) EmotionBtn.SetActive(false);
                //RNMessenger.SendJson(json);
                yield break;
            }


            RenderTexture.active = _recordTexture;
            Texture2D texture = new Texture2D(_targetResolution, _targetResolution, TextureFormat.RGBA32, false, false);
            texture.ReadPixels(new Rect(0, 0, _targetResolution, _targetResolution), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            CaptureCurrentBlendShapeWeights();
            CaptureCurrentHeadRotation();
            CaptureCurrentFace2D();
            //_frameQueue.Enqueue(texture.GetRawTextureData());
            //_pngForPlay.Add(texture.EncodeToPNG());
            _pngForPlay.Add(texture);
            _currentCaptureFrameNumber++;

            yield break;
        }


        void CaptureCurrentHeadRotation()
        {
            Quaternion currentHeadRotation = new Quaternion();

            currentHeadRotation = _headRotationManager.head.localRotation;
            _currentHeadRotation.Add(currentHeadRotation);
        }

        void CaptureCurrentBlendShapeWeights()
        {
            List<float> currentBlendShapeWeights = new List<float>();
            if (Face3D.activeSelf)
            {
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(0));
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(1));
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(2));
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(4));
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(5));
                currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(6));
            }
            else
            {
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(0));
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(1));
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(2));
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(4));
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(5));
                currentBlendShapeWeights.Add(Head2D.GetBlendShapeWeight(6));
            }
            currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(12));
            currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(14));
            currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(15));
            currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(16));
            currentBlendShapeWeights.Add(Head3D.GetBlendShapeWeight(17));

            _capptureBlendShapeWeights.Add(currentBlendShapeWeights);
        }

        void CaptureCurrentFace2D()
        {
            _currentFace2DPlaying.Add(Face3D.activeSelf);

            if (!Face3D.activeSelf) _currentFace2D.Add(EmoIndex);
            else _currentFace2D.Add(4);

        }

        void UpdateCaptureBlendShapeWeights(int idx)
        {
            if (_capptureBlendShapeWeights.Count <= idx) return;

            List<float> currentBlendShapeWeights = _capptureBlendShapeWeights[idx];

            if (Face3D.activeSelf)
            {
                Head3D.SetBlendShapeWeight(0, currentBlendShapeWeights[0]);
                Head3D.SetBlendShapeWeight(1, currentBlendShapeWeights[1]);
                Head3D.SetBlendShapeWeight(2, currentBlendShapeWeights[2]);
                Head3D.SetBlendShapeWeight(4, currentBlendShapeWeights[3]);
                Head3D.SetBlendShapeWeight(5, currentBlendShapeWeights[4]);
                Head3D.SetBlendShapeWeight(6, currentBlendShapeWeights[5]);


            }
            else
            {
                Head2D.SetBlendShapeWeight(0, currentBlendShapeWeights[0]);
                Head2D.SetBlendShapeWeight(1, currentBlendShapeWeights[1]);
                Head2D.SetBlendShapeWeight(2, currentBlendShapeWeights[2]);
                Head2D.SetBlendShapeWeight(4, currentBlendShapeWeights[3]);
                Head2D.SetBlendShapeWeight(5, currentBlendShapeWeights[4]);
                Head2D.SetBlendShapeWeight(6, currentBlendShapeWeights[5]);
            }
            Head3D.SetBlendShapeWeight(12, currentBlendShapeWeights[6]);
            Head3D.SetBlendShapeWeight(14, currentBlendShapeWeights[7]);
            Head3D.SetBlendShapeWeight(15, currentBlendShapeWeights[8]);
            Head3D.SetBlendShapeWeight(16, currentBlendShapeWeights[9]);
            Head3D.SetBlendShapeWeight(17, currentBlendShapeWeights[10]);

        }



        IEnumerator PlayPNG()
        {
            UnityEngine.Debug.Log("PlayPNG");
            for (int i = 0; i < 90; i++)
            {
                byte[] currentPNG = _pngForPlay[i].GetRawTextureData();
                //_frameQueue.Enqueue(currentPNG);
                _pngForPlayByte.Add(currentPNG);
            }

            // Record UI Remove

            LoadingBar.enabled = false;
            BackShadow.enabled = false;


            // png start

            IsPlaying = true;
            _num = 0;
            _pngOrder = true;
            _lastCaptureFrameTime = Time.time;

            // combine pngs

            // _recordThread = new Thread(CombinePNGWorker);
            // _recordThread.Start();
            CombinePNGWorker();



            yield break;


        }

        void CombinePNGWorker()
        {
            Stopwatch cb = new Stopwatch();

            cb.Start();

            byte[] rawPNG = new byte[_pngForPlayByte[0].Length * 10];
            byte[] resultPNG = new byte[_pngForPlayByte[0].Length * 90];

            int pngCount = 10; // 가로에 들어가는 사진 개수
            for (int c = 0; c < 9; c++)// 이미지 9번 쌓기(10장 묶음)
            {
                for (int i = 0; i < 10; i++)//이미지 10장 저장(1행)
                {
                    for (int j = 0; j < _targetResolution; j++) //이미지 1장 저장
                    {
                        Array.Copy(_pngForPlayByte[i + (10 * c)], j * 4 * _targetResolution, rawPNG, (j * pngCount * 4 * _targetResolution) + i * (4 * _targetResolution), 4 * _targetResolution);
                    }

                }

                Buffer.BlockCopy(rawPNG, 0, resultPNG, (8 - c) * (_pngForPlayByte[0].Length * 10), _pngForPlayByte[0].Length * 10);
                Array.Clear(rawPNG, 0, _pngForPlayByte[0].Length * 10);

            }

            string compath = _recorodPath + "combine.png";

            byte[] resultPNG2 = ImageConversion.EncodeArrayToPNG(resultPNG, GraphicsFormat.R8G8B8A8_SRGB, (uint)_targetResolution * 10, (uint)_targetResolution * 9);
            File.WriteAllBytes(compath, resultPNG2);

            cb.Stop();
            UnityEngine.Debug.Log("CombineTime : " + cb.ElapsedMilliseconds.ToString() + "ms");
            cb.Reset();

            UpdateRecordState(false);
        }


        // 0001.png - 0179png 저장

        //void FrameSaveWorker()
        //{
        //    print("Run Thread FrameSaveWorker");
        //    int savingFrameNumber = 1;
        //    int bsavingFrameNumber = _targetFrameRate * _targetRecordSec * 2 - 1;

        //    while (_isRecording || _frameQueue.Count > 0)
        //    {
        //        if (_frameQueue.Count > 0 && savingFrameNumber <= 90)
        //        {
        //            // Generate file path
        //            string path = _recorodPath + savingFrameNumber.ToString().PadLeft(4, '0') + ".png";
        //            string path2 = _recorodPath + bsavingFrameNumber.ToString().PadLeft(4, '0') + ".png";

        //            // Dequeue the frame, encode it as a bitmap, and write it to the file
        //            byte[] pngbyte = ImageConversion.EncodeArrayToPNG(_frameQueue.Dequeue(), GraphicsFormat.R8G8B8A8_SRGB, (uint)_targetResolution, (uint)_targetResolution);
        //            //byte[] pngbyte = ImageConversion.EncodeArrayToPNG(_frameQueue.Dequeue(), GraphicsFormat.R8G8B8A8_SRGB, (uint)_targetResolution, (uint)_targetResolution);

        //            File.WriteAllBytes(path, pngbyte);
        //            File.WriteAllBytes(path2, pngbyte);
        //            savingFrameNumber++;// 0으로 초기화 되어있음
        //            bsavingFrameNumber--;
        //            print("Saved " + savingFrameNumber + " frames. " + _frameQueue.Count + " frames remaining.");
        //        }
        //    }
        //    _stopWatch.Stop();
        //    UnityEngine.Debug.Log("FrameSaveWorker : " + _stopWatch.ElapsedMilliseconds.ToString() + "ms");
        //    _stopWatch.Reset();
        //}





    }
}
