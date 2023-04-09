using Unity;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System;
using System.Linq;
using UnityEngine.UI;
using static MoodMe.FaceDetectorPostProcessing;

namespace MoodMe
{
    public class FaceDetector : MonoBehaviour
    {
        public NNModel Network;

        // 카메라에서 받아오는 이미지 크기
        private int ImageInputWidth = 640;
        private int ImageInputHeight = 480;

        // 얼굴 인식 모델에 들어가는 이미지 크기 
        private int ImageNetworkWidth = 320;
        private int ImageNetworkHeight = 240;

        // 모바일 카메라 회전 이슈로 추가된 이미지 크기 
        private int ImageMobileWidth = 180;
        private int ImageMobileHeight = 240;

        // 표정 인식 모델로 넘어갈 이미지 크기 
        private int ExportCropWidth = 48;
        private int ExportCropHeight = 48;

        [Range(1, 4)]
        public int ChannelCount = 3;

        [Range(0, 1)]
        public float DetectionThreshold = 0.7f;

        public WorkerFactory.Device Device = WorkerFactory.Device.Auto;

        public bool Process;
        public bool GUIPreview = true;
        public bool ExportCrop = true;

        public Preprocessing.ValueType CropFormat;

        public static Color32[] OutputCrop;

        public static float[] OutputCropFloat;

		public static Texture2D _cropTexture;

        private IWorker engine;

        private Tensor scores;
        private Tensor boxes;
        private Tensor output;
        private Tensor tensor;

        private List<FaceInfo> predict_boxes;

        private Color32[] rgba;
        private Color32[] resizeData;
        private Color32[] resultData;
        private Color32[] _outputPixelData;
        private float[] tensorData;
        private bool predictDone = false;
        private Texture2D _staticRectTexture;
        private Texture2D _outputTexture;
        private GUIStyle _staticRectStyle;
        private float xScale, yScale;

        private int GUIW, GUIH;

        void Start()
        {
            Model model = ModelLoader.Load(Network);
            string[] additionalOutputs = new string[] { "scores", "boxes" };
            engine = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, model, additionalOutputs, false);

#if UNITY_EDITOR
            xScale = ImageInputWidth / (float)ImageNetworkWidth;
            yScale = ImageInputHeight / (float)ImageNetworkHeight;
            _outputTexture = new Texture2D(ImageInputWidth, ImageInputHeight, TextureFormat.RGBA32, false);
#elif UNITY_IOS || UNITY_ANDROID
            xScale = 2.665f;
            yScale = 2.665f;
            _outputTexture = new Texture2D(ImageInputHeight, ImageInputWidth, TextureFormat.RGBA32, false);
            resultData = new Color32[ImageNetworkWidth * ImageNetworkHeight];
            _outputPixelData = new Color32[ImageInputWidth * ImageInputHeight];
#endif

            _staticRectStyle = new GUIStyle();
            _staticRectTexture = new Texture2D(1, 1);
            _staticRectTexture.SetPixel(0, 0, new Color(1f, 0f, 0f, 0.5f));
            _staticRectTexture.Apply();
            _staticRectStyle.normal.background = _staticRectTexture;
        }

 
        void Update()
        {
            if (!Process) return;

            rgba = Mediapipe.Unity.Tutorial.FaceMesh._inputPixelData;
            if (rgba.Length == (ImageInputHeight * ImageInputWidth))
            {
                Preprocessing.InputImage = rgba;

#if UNITY_EDITOR
                _outputTexture.SetPixels32(rgba);
                _outputTexture.Apply();
                tensorData = Preprocessing.Preprocess(ImageInputWidth, ImageInputHeight, ImageNetworkWidth, ImageNetworkHeight, TextureFormat.RGB24, Preprocessing.OrientationType.Upsidedown, Preprocessing.ValueType.LinearNormalized);

#elif UNITY_IOS || UNITY_ANDROID
#if UNITY_ANDROID
                // Android 카메라 270도 회전으로 이미지도 회전 
                _outputPixelData = Preprocessing.Preprocess(ImageInputWidth, ImageInputHeight, ImageInputWidth, ImageInputHeight, Preprocessing.OrientationType.CW90);
#elif UNITY_IOS
                // IOS 카메라 90도 회전으로 이미지도 회전 
                _outputPixelData = Preprocessing.Preprocess(ImageInputWidth, ImageInputHeight, ImageInputWidth, ImageInputHeight, Preprocessing.OrientationType.ACW90);
#endif
                _outputTexture.SetPixels32(_outputPixelData);
                _outputTexture.Apply();

                Preprocessing.InputImage = _outputPixelData;
                resizeData = Preprocessing.Preprocess(ImageInputHeight,ImageInputWidth, ImageMobileWidth, ImageMobileHeight, Preprocessing.OrientationType.Upsidedown);

                
                // 얼굴 인식 모델 input 사이즈 (320x240)에 맞춰 이미지 변경 
                for (int i = 0; i < 240; i++)
                {
                    int inputIndex = 0;
                    for (int j = 0; j < 320; j++)
                    {
                        if (j < 140)
                        {
                            resultData[(320 * i) + j][0] = 0;
                            resultData[(320 * i) + j][1] = 0;
                            resultData[(320 * i) + j][2] = 0;
                            resultData[(320 * i) + j][3] = 255;
                        }
                        else
                        {
                            resultData[(320 * i) + j][0] = resizeData[inputIndex + (180 * i)][0];
                            resultData[(320 * i) + j][1] = resizeData[inputIndex + (180 * i)][1];
                            resultData[(320 * i) + j][2] = resizeData[inputIndex + (180 * i)][2];
                            resultData[(320 * i) + j][3] = 255;
                            inputIndex += 1;
                        }
                    }
                }

                Preprocessing.InputImage = resultData;
                tensorData = Preprocessing.Preprocess(ImageNetworkWidth, ImageNetworkHeight, ImageNetworkWidth, ImageNetworkHeight, TextureFormat.RGB24, Preprocessing.OrientationType.Source, Preprocessing.ValueType.LinearNormalized);
#endif
                tensor = new Tensor(1, ImageNetworkHeight, ImageNetworkWidth, ChannelCount, tensorData);
                //DateTime timestamp;
                //timestamp = DateTime.Now;
                output = engine.Execute(tensor).CopyOutput();
                //Debug.Log("FACE DETECTOR INFERENCE TIME: " + (DateTime.Now - timestamp).TotalMilliseconds + " ms");
                scores = engine.PeekOutput("scores");
                boxes = engine.PeekOutput("boxes");
                predictDone = false;
                predict_boxes = Predict(ImageNetworkWidth, ImageNetworkHeight, scores.ToReadOnlyArray(), boxes.ToReadOnlyArray(), DetectionThreshold);
                predictDone = true;

                FaceInfo _bestBox = GetBestBox(predict_boxes);
                _bestBox = GetBiggestSquare(_bestBox);

                int _boxSide = Mathf.CeilToInt(_bestBox.x2 - _bestBox.x1);

                if ((_boxSide > 0) && ExportCrop)
                {
                    try
                    {
                        int _xCrop = 0;
                        int _yCrop = 0;

                        Rect _souceRec = new Rect(0, 0, 0, 0);

#if UNITY_EDITOR
                        _xCrop = ImageInputWidth - (int)(_bestBox.x2 * xScale);
                        _yCrop = ImageInputHeight - (int)(_bestBox.y2 * yScale);
#elif UNITY_IOS || UNITY_ANDROID
                        _xCrop = ImageInputHeight - (int)((_bestBox.x2 - 140) * xScale); //xScale = ImageInputWidth / (float)ImageNetworkWidth;
                        _yCrop = ImageInputWidth - (int)(_bestBox.y2 * yScale);
#endif

                        int _xBoxSide = (int)(_boxSide * xScale);
                        int _yBoxSide = (int)(_boxSide * yScale);

                        Vector2 _cropStart = new Vector2(_xCrop, _yCrop);
                        Vector2 _cropEnd = new Vector2(_xCrop + _xBoxSide, _yCrop + _yBoxSide);

#if UNITY_EDITOR
                        _souceRec = new Rect(0, 0, ImageInputWidth, ImageInputHeight);
#elif UNITY_IOS || UNITY_ANDROID
                        _souceRec = new Rect(0, 0, ImageInputHeight, ImageInputWidth);
#endif


                        if (_souceRec.Contains(_cropStart) && _souceRec.Contains(_cropEnd))
                        {
                            OutputCrop = new Color32[_boxSide * _boxSide];
                            _cropTexture = new Texture2D((int)(_boxSide * xScale), (int)(_boxSide * yScale), TextureFormat.RGBA32, false);
#if UNITY_EDITOR
                            _cropTexture.SetPixels(0, 0, (int)(_boxSide * xScale), (int)(_boxSide * yScale), Mediapipe.Unity.Tutorial.FaceMesh._inputTexture.GetPixels(_xCrop, _yCrop, _xBoxSide, _yBoxSide));
#elif UNITY_IOS || UNITY_ANDROID
                            _cropTexture.SetPixels(0, 0, (int)(_boxSide * xScale), (int)(_boxSide * yScale), _outputTexture.GetPixels(_xCrop, _yCrop, _xBoxSide, _yBoxSide));
#endif
                            _cropTexture.Apply();

                            OutputCrop = _cropTexture.GetPixels32();
                            Preprocessing.InputImage = OutputCrop;
                            OutputCrop = Preprocessing.Preprocess((int)(_boxSide * xScale), (int)(_boxSide * yScale), ExportCropWidth, ExportCropHeight, Preprocessing.OrientationType.Source);
                            if (CropFormat != Preprocessing.ValueType.Color32)
                            {
                                OutputCropFloat = Preprocessing.Preprocess(ExportCropWidth, ExportCropHeight, ExportCropWidth, ExportCropHeight, TextureFormat.RGBA32, Preprocessing.OrientationType.Source, Preprocessing.ValueType.Linear);
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                        Debug.Log("CROP EXCEPTION:" + ex.Message);
                    }

                }

                //foreach (FaceInfo faceInfo in predict_boxes)
                //{
                //    Vector3 UL = new Vector3(faceInfo.x1, faceInfo.y1, 0);
                //    Vector3 UR = new Vector3(faceInfo.x2, faceInfo.y1, 0);
                //    Vector3 DL = new Vector3(faceInfo.x1, faceInfo.y2, 0);
                //    Vector3 DR = new Vector3(faceInfo.x2, faceInfo.y2, 0);
                //    Debug.Log("BOX " + faceInfo.x1 + "," + faceInfo.y1 + "," + faceInfo.x2 + "," + faceInfo.y2 + "=" + faceInfo.score);
                //}

                scores.Dispose();
                boxes.Dispose();
                output.Dispose();
                tensor.Dispose();
                Process = false;
            }

        }

        private FaceInfo GetBestBox(List<FaceInfo> predict_boxes)
        {
            FaceInfo _bestBox = new FaceInfo()
            {
                x1 = 0,
                x2 = 0,
                y1 = 0,
                y2 = 0,
                score = 0
            };
            foreach (FaceInfo box in predict_boxes)
            {
                if (box.score > _bestBox.score)
                {
                    _bestBox = box;
                }
            }
            return _bestBox;
        }

        private FaceInfo GetBiggestSquare(FaceInfo Box)
        {
            float boxWidth = Box.x2 - Box.x1;
            float boxHeight = Box.y2 - Box.y1;

            float bigEdge = boxWidth > boxHeight ? boxWidth : boxHeight;
            float smallEdge = boxWidth < boxHeight ? boxWidth : boxHeight;

            float bigX0 = boxWidth > boxHeight ? (Box.x1) : (Box.x1 - (bigEdge - smallEdge) / 2);
            float bigY0 = boxWidth < boxHeight ? (Box.y1) : (Box.y1 - (bigEdge - smallEdge) / 2);

            return new FaceInfo()
            {
                x1 = bigX0,
                y1 = bigY0,
                x2 = bigX0 + bigEdge,
                y2 = bigY0 + bigEdge
            };

        }

        private void OnGUI()
        {


            if (predictDone && GUIPreview)
            {
                GUIW = Screen.width;
                GUIH = Screen.height;
                float ratio = (GUIW>=GUIH?GUIW:GUIH) / ImageNetworkWidth;
                int centerSquareWidth = Mathf.FloorToInt((GUIW / 2) - ((ImageNetworkWidth / 2) * ratio));
                int centerSquareHeight = Mathf.FloorToInt((GUIH / 2) - ((ImageNetworkHeight / 2) * ratio));
                for (int i = 0; i < predict_boxes.Count; i++)
                {
                    float boxWidth = predict_boxes[i].x2 - predict_boxes[i].x1;
                    float boxHeight = predict_boxes[i].y2 - predict_boxes[i].y1;

                    float bigEdge = boxWidth > boxHeight ? boxWidth : boxHeight;
                    float smallEdge = boxWidth < boxHeight ? boxWidth : boxHeight;

                    float bigX0 = boxWidth > boxHeight ? (predict_boxes[i].x1 * ratio) : ((predict_boxes[i].x1 - (bigEdge - smallEdge) / 2) * ratio);
                    float bigY0 = boxWidth < boxHeight ? (predict_boxes[i].y1 * ratio) : ((predict_boxes[i].y1 - (bigEdge - smallEdge) / 2) * ratio);

                    //float bigX1 = boxWidth > boxHeight ? (predict_boxes[i][2] * ratio) : ((predict_boxes[i][2] + (bigEdge - smallEdge) / 2) * ratio);
                    //float bigY1 = boxWidth < boxHeight ? (predict_boxes[i][3] * ratio) : ((predict_boxes[i][3] + (bigEdge - smallEdge) / 2) * ratio);

                    //float smallX0 = boxWidth < boxHeight ? (predict_boxes[i].x1 * ratio) : ((predict_boxes[i].x1 + (bigEdge - smallEdge) / 2) * ratio);
                    //float smallY0 = boxWidth > boxHeight ? (predict_boxes[i].y1 * ratio) : ((predict_boxes[i].y1 + (bigEdge - smallEdge) / 2) * ratio);

                    //float smallX1 = boxWidth < boxHeight ? (predict_boxes[i][2] * ratio) : ((predict_boxes[i][2] - (bigEdge - smallEdge) / 2) * ratio);
                    //float smallY1 = boxWidth > boxHeight ? (predict_boxes[i][3] * ratio) : ((predict_boxes[i][3] - (bigEdge - smallEdge) / 2) * ratio);

                    Rect rectInst = new Rect(centerSquareWidth + bigX0,
                                            centerSquareHeight + bigY0,
                                            bigEdge * ratio,
                                            bigEdge * ratio);

                    //GUI.Box(rectInst, GUIContent.none, _staticRectStyle); // Draw Red Box on the Screen

                    //rectInst = new Rect(centerSquareWidth + smallX0,
                    //                                        centerSquareHeight + smallY0,
                    //                                        smallEdge * ratio,
                    //                                        smallEdge * ratio);

                    //GUI.Box(rectInst, GUIContent.none, _staticRectStyle);

                }

            }
        }

        void OnDestroy()
        {
            scores.Dispose();
            boxes.Dispose();
            output.Dispose();
            tensor.Dispose();
			engine.Dispose();				 
        }

    }
}