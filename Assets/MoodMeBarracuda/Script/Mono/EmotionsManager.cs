using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoodMe;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using TMPro;
using Mediapipe.Unity.Tutorial;

namespace MoodMe
{

    public class EmotionsManager : MonoBehaviour
    {
        //public MeshRenderer PreviewMR;
        //[Header("ENTER LICENSE HERE")]
        //public string Email = "";
        //public string AndroidLicense = "";
        //public string IosLicense = "";
        //public string OsxLicense = "";
        //public string WindowsLicense = "";

        [Header("Input")]
        public ManageEmotionsNetwork EmotionNetworkManager;
        public FaceDetector FaceDetectorManager;
        public TextMeshProUGUI EmotionText;

        //[Header("Performance")]
        //[Range(1, 60)]
        private int _processEveryNFrames = 3;
        [Header("Processing")]
        public bool FilterAllZeros = true;
        //[Range(0.1f, 60f)]
#if UNITY_EDITOR || UNITY_IOS
        private float _frequency = 20f;
#elif UNITY_ANDROID
        private float _frequency = 8f;
#endif
        //[Range(0.1f, 60f)]
        public float MinCutOff = 1.1f;

        [Header("Emotions")]
        public bool TestMode = false;
        [Range(0, 1f)]
        public float Angry;
        [Range(0, 1f)]
        public float Disgust;
        [Range(0, 1f)]
        public float Happy;
        [Range(0, 1f)]
        public float Neutral;
        [Range(0, 1f)]
        public float Sad;
        [Range(0, 1f)]
        public float Scared;
        [Range(0, 1f)]
        public float Surprised;
        [Range(0, 1f)]

        public static float EmotionIndex;


        public static MoodMeEmotions.MDMEmotions Emotions;
        public static bool BufferProcessed = false;
        public static bool ValidData = false;
        private static MoodMeEmotions.MDMEmotions s_currentEmotions;


        //Main buffer texture
        public static WebCamTexture CameraTexture;

        private EmotionsInterface _emotionNN;


        //Main buffer


        private byte[] _buffer;


        private int _nFramePassed;

        //private static DateTime timestamp;

        private OneEuroFilter _angryFilter, _disgustFilter, _happyFilter, _neutralFilter, _sadFilter, _scaredFilter, _surprisedFilter;

        string _currEmo = "neutral";
        string _stanEmo = "neutral";
        int _thres = 0;

        private newRotation _rotationManager = null;


        private void Awake()
        {
            newRotation[] rotationManagers = FindObjectsOfType<newRotation>();
            if (rotationManagers.Length > 0) _rotationManager = FindObjectsOfType<newRotation>()[0];
        }

        void Start()
        {
            // Make the game run as fast as possible
            Application.targetFrameRate = 200;

            _emotionNN = new EmotionsInterface(EmotionNetworkManager, FaceDetectorManager);

            _angryFilter = new OneEuroFilter(_frequency, MinCutOff);
            _disgustFilter = new OneEuroFilter(_frequency, MinCutOff);
            _happyFilter = new OneEuroFilter(_frequency, MinCutOff);
            _neutralFilter = new OneEuroFilter(_frequency, MinCutOff);
            _sadFilter = new OneEuroFilter(_frequency, MinCutOff);
            _scaredFilter = new OneEuroFilter(_frequency, MinCutOff);
            _surprisedFilter = new OneEuroFilter(_frequency, MinCutOff);

            EmotionText.text = "neutral";

        }

        void OnDestroy()
        {
            _emotionNN = null;
        }

        void LateUpdate()
        {
            //If a Render Texture is provided in the VideoTexture (or just a still image), Webcam image will be ignored
            BufferProcessed = false;
            ValidData = false;
            if (!TestMode)
            {
                if (Mediapipe.Unity.Tutorial.FaceMesh.WebcamReady)
                {

                    _nFramePassed = (_nFramePassed + 1) % _processEveryNFrames;
                    if (_nFramePassed == 0)
                    {
                        try
                        {
                            _emotionNN.ProcessFrame();
                            BufferProcessed = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex.Message);
                            BufferProcessed = false;
                        }

                        if (BufferProcessed)
                        {
                            ValidData = !_emotionNN.DetectedEmotions.AllZero;
                            if (!(_emotionNN.DetectedEmotions.AllZero && FilterAllZeros))
                            {
                                s_currentEmotions = _emotionNN.DetectedEmotions;
                                Emotions = Filter(Emotions, s_currentEmotions, _frequency, MinCutOff);
                                Angry = Emotions.angry;
                                Disgust = Emotions.disgust;
                                Happy = Emotions.happy;
                                Neutral = Emotions.neutral;
                                Sad = Emotions.sad;
                                Scared = Emotions.scared;
                                Surprised = Emotions.surprised;
                                //화면에 현재 감정 출력
                                //print("Head Rotation : " + _rotationManager.head.localRotation.z); //max 0.18
                                if (Sad >= 0.55)
                                {
                                    //if (_rotationManager.head.localRotation.z > 0.1 && 입꼬리가 :( 가 아닐 때 )
#if UNITY_EDITOR || UNITY_ANDROID
                                    if (FaceAnimationController.Sad_Ratio < 0.3 || _rotationManager.head.localRotation.z > 0.17)
#elif UNITY_IOS
                                    if (FaceAnimationController.Sad_Ratio < 0.5 || _rotationManager.head.localRotation.z > 0.17)
#endif
                                    {
                                        _currEmo = "Neutral";
                                    }
                                    else
                                    {
                                        _currEmo = "Sad";
                                    }
                                }
                                else
                                {
                                    if (Surprised >= 0.55)  // max 0.9 
                                    {
                                        if (Angry >= 0.4) _currEmo = "Angry";
                                        else _currEmo = "Surprised";
                                    }
                                    else if (Surprised < 0.55 && Angry >= 0.4)
                                    {
                                        if (Happy >= 0.7) _currEmo = "Happy";
                                        else _currEmo = "Angry";
                                    }
                                    else
                                    {
                                        if (Happy >= 0.4) _currEmo = "Happy";  //활짝 웃었을 때만 변하려면 0.9 
                                        else _currEmo = "Neutral";
                                    }
                                }
                                //감정 출력이 반복되는 빈도
                                if (_stanEmo == _currEmo)
                                {
                                    _thres += 1;
                                    if (_thres == 2)
                                    {
                                        if (_currEmo == "sad") _thres += 1;
                                        else
                                        {
                                            EmotionText.text = _currEmo;
                                            _thres = 0;
                                        }
                                    }
#if UNITY_EDITOR || UNITY_IOS
                                    if (_thres == 8)
#elif UNITY_ANDROID
                                    if (_thres == 4)
#endif
                                    {
                                        EmotionText.text = _currEmo;
                                        _thres = 0;
                                    }
                                }
                                else _stanEmo = _currEmo;
                                //addend
                            }
                            else
                            {
                                ValidData = false;
                                BufferProcessed = false;
                            }
                        }
                        else
                        {
                            Emotions.Error = true;
                        }
                    }
                }
            }
            else
            {
                Emotions.angry = Angry;
                Emotions.disgust = Disgust;
                Emotions.happy = Happy;
                Emotions.neutral = Neutral;
                Emotions.sad = Sad;
                Emotions.scared = Scared;
                Emotions.surprised = Surprised;
            }
            EmotionIndex = (((3f * Happy + Surprised - (Sad + Scared + Disgust + Angry)) / 3f) + 1f) / 2f;

            _angryFilter.UpdateParams(_frequency, MinCutOff);
            _disgustFilter.UpdateParams(_frequency, MinCutOff);
            _happyFilter.UpdateParams(_frequency, MinCutOff);
            _neutralFilter.UpdateParams(_frequency, MinCutOff);
            _sadFilter.UpdateParams(_frequency, MinCutOff);
            _scaredFilter.UpdateParams(_frequency, MinCutOff);
            _surprisedFilter.UpdateParams(_frequency, MinCutOff);
        }

        // Smoothing function
        MoodMeEmotions.MDMEmotions Filter(MoodMeEmotions.MDMEmotions target, MoodMeEmotions.MDMEmotions source, float _frequency, float mincutoff)
        {
            target.angry = _angryFilter.Filter(source.angry);
            target.disgust = _disgustFilter.Filter(source.disgust);
            target.happy = _happyFilter.Filter(source.happy);
            target.neutral = _neutralFilter.Filter(source.neutral);
            target.sad = _sadFilter.Filter(source.sad);
            target.scared = _scaredFilter.Filter(source.scared);
            target.surprised = _surprisedFilter.Filter(source.surprised);

            return target;
        }
    }

}