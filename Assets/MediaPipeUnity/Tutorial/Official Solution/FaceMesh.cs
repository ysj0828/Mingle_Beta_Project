using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.CoordinateSystem;
// using static MoodMe.FaceDetectorPostProcessing;

using Stopwatch = System.Diagnostics.Stopwatch;

using UnityEngine.Android;
//using UnityEngine.iOS;  // iOS 빌드 시 주석 제거해주기

namespace Mediapipe.Unity.Tutorial
{
    public class FaceMesh : MonoBehaviour
    {
        [SerializeField] private TextAsset[] _configAsset;
        //[SerializeField] private RawImage _screen;

        // Screen size는 Android는 최소 320x240(그 밑으로 안내려감), iOS는 640x480(다른 크기는 안됨)으로 Inspector 창에서 설정해주기
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private int _fps;

        private CalculatorGraph _graph;
        private ResourceManager _resourceManager;

        private WebCamTexture _webCamTexture;
        private Color32[] colorResult;

        private int frontCamInt;
        private List<int> multifrontCamInt;  // frontCam이 여러개면 각 index를 저장해줄 리스트

        private OutputStream<ImageFramePacket, ImageFrame> outputVideoStream;
        private OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>> multiFaceLandmarksStream;

        private AllVariables singleton;

        public static Color32[] _inputPixelData { get; private set; }
        public static Color32[] _outputPixelData { get; private set; }

        public static Color32[] _processPixelData { get; private set; }

        public static Texture2D _inputTexture;
        public static Texture2D _outputTexture;

        public static bool WebcamReady = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        private void Awake()
        {
            // Android 빌드할 때 Webcam Permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
        }
#endif

        private IEnumerator Start()
        {
            Application.targetFrameRate = 300;
#if UNITY_IOS && !UNITY_EDITOR
            // iOS 빌드할 때 Webcam Permission
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
            if (WebCamTexture.devices.Length == 0)
            {
                throw new System.Exception("Web Camera devices are not found");
            }

            // front Camera 찾기
            List<int> multifrontCamInt = new List<int>();
            for (int i = 0; i < WebCamTexture.devices.Length; i++)
            {
                if (WebCamTexture.devices[i].isFrontFacing)
                {
                    multifrontCamInt.Add(i);
                    Debug.Log("frontCam: " + i);
                }
                else
                    Debug.Log("It is not frontCam");
            }

            var webCamDevice = WebCamTexture.devices[multifrontCamInt[0]];  // devices[0] ~ [?]
            _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
            _webCamTexture.Play();  // 카메라 작동


            if (AllVariables.instance == null)  // 랜드마크 싱글톤 변수 null이면 받아올 때까지 기다리기
            {
                yield return new WaitForEndOfFrame();
            }
            singleton = AllVariables.instance;  // 싱글톤 변수 정의


            yield return new WaitUntil(() => _webCamTexture.width > 16);

            WebcamReady = true;

            //_screen.rectTransform.sizeDelta = new Vector2(_width, _height);

            // input, output texture 가져오기
            _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _inputPixelData = new Color32[_width * _height];

#if UNITY_EDITOR
            _outputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);

#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            _outputTexture = new Texture2D(_height, _width, TextureFormat.RGBA32, false);
#endif

            _outputPixelData = new Color32[_width * _height];
            //_screen.texture = _outputTexture; 

            //_resourceManager = new LocalResourceManager();  // Build가 아닌 Editor Play만을 위한 리소스 매니저
            _resourceManager = new StreamingAssetsResourceManager();  // Build 하기 위한 리소스 매니저
            var stopwatch = new Stopwatch();
            //var screenRect = _screen.GetComponent<RectTransform>().rect;

            yield return _resourceManager.PrepareAssetAsync("face_detection_short_range.bytes");
            yield return _resourceManager.PrepareAssetAsync("face_landmark_with_attention.bytes");



#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            yield return GpuManager.Initialize(); // 이 코드가 cpu로 돌리는 editor 코드를 gpu 코드에 맞춰서 추가한 건가?

            if (!GpuManager.IsInitialized)
            {
                throw new System.Exception("Failed to initialize GPU resources");
            }
#endif

#if UNITY_EDITOR
            _graph = new CalculatorGraph(_configAsset[0].text);  // cpu버전 parsing text file
#elif UNITY_ANDROID && !UNITY_EDITOR
            _graph = new CalculatorGraph(_configAsset[1].text);  //AOS gpu버전 parsing text file
#elif UNITY_IOS && !UNITY_EDITOR
            _graph = new CalculatorGraph(_configAsset[2].text);  // cpu버전 parsing text file
#endif

            outputVideoStream = new OutputStream<ImageFramePacket, ImageFrame>(_graph, "output_video");
            multiFaceLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(_graph, "multi_face_landmarks");  // FaceMeshGraph에서 가져오기 
            outputVideoStream.StartPolling().AssertOk();
            multiFaceLandmarksStream.StartPolling().AssertOk();
            _graph.StartRun().AssertOk();
            stopwatch.Start();

            while (true)
            {

                _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_inputPixelData));
                _inputTexture.Apply();

                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
                _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();

                yield return new WaitForEndOfFrame();

                if (outputVideoStream.TryGetNext(out var outputVideo))
                {
                    if (outputVideo.TryReadPixelData(_outputPixelData))
                    {
                        _outputTexture.SetPixels32(_outputPixelData);
                        _outputTexture.Apply();
                    }
                }

                yield return new WaitForThreadedTask(() => { SetLandmarks(); });  // 새로운 스레드로 보내기 
            }
        }


        private void OnDestroy()
        {
            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
            }

            else
            {
                return;
            }

            if (_graph != null)
            {
                try
                {
                    _graph.CloseInputStream("input_video").AssertOk();
                    _graph.CloseAllPacketSources().AssertOk();
                    _graph.WaitUntilDone().AssertOk();
                }
                finally
                {
                    _graph.Dispose();
                }
            }
            else
                return;
        }

        private void SetLandmarks()  // 얼굴 랜드마크 예측
        {
            if (multiFaceLandmarksStream.TryGetNext(out var multiFaceLandmarks))
            {
                if (multiFaceLandmarks != null && multiFaceLandmarks.Count > 0)
                {
                    foreach (var landmarks in multiFaceLandmarks)
                    {
                        singleton.topLeftEye = landmarks.Landmark[159];
                        singleton.bottomLeftEye = landmarks.Landmark[145];
                        singleton.leftLeftEye = landmarks.Landmark[33];
                        singleton.rightLeftEye = landmarks.Landmark[133];
                        singleton.topRightEye = landmarks.Landmark[386];
                        singleton.bottomRightEye = landmarks.Landmark[374];
                        singleton.leftRightEye = landmarks.Landmark[362];
                        singleton.rightRightEye = landmarks.Landmark[263];
                        singleton.centerLeftEyebrow = landmarks.Landmark[52];
                        singleton.centerRightEyebrow = landmarks.Landmark[282];
                        singleton.topNose = landmarks.Landmark[168];
                        singleton.middleNose = landmarks.Landmark[4];
                        singleton.bottomNose = landmarks.Landmark[94];
                        singleton.bottomLip = landmarks.Landmark[17];
                        singleton.topMouth = landmarks.Landmark[0];
                        singleton.bottomMouth = landmarks.Landmark[17];
                        singleton.LeftMouth = landmarks.Landmark[61];
                        singleton.RightMouth = landmarks.Landmark[291];
                        singleton.CenterTopMouth = landmarks.Landmark[13];
                        singleton.CenterBottomMouth = landmarks.Landmark[14];
                        singleton.LeftEar = landmarks.Landmark[127];
                        singleton.RightEar = landmarks.Landmark[356];
                        singleton.Nose = landmarks.Landmark[6];
                        singleton.Jaw = landmarks.Landmark[152];
                        singleton.leftIris = landmarks.Landmark[469];
                        singleton.rightIris = landmarks.Landmark[474];
                        singleton.belowLeftMouth = landmarks.Landmark[43];
                        singleton.belowRightMouth = landmarks.Landmark[273];
                    }
                }
                else
                    return;
            }
        }

        private void Update()
        {
            //FaceAnimationController.CalculateRatio();
            //print("rotateAngle : " + _webCamTexture.videoRotationAngle);
        }
    }
}
