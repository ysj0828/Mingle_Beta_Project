using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Mediapipe;
using Mediapipe.Unity;
using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.Android;
//using UnityEngine.iOS;  // iOS 빌드 시 주석 제거해주기
using Mingle;
using TMPro;


namespace Mediapipe.Unity.Tutorial
{
    public class FaceAnimationController : MonoBehaviour
    {

        public TextMeshProUGUI EmotionText;

        [Header("[Setting]")]

        public bool EnableBrow = true;

        public bool EnableEye = true;

        public bool EnableMouth = true;

        // Lerp Interpolation 변수
#if UNITY_EDITOR
        private float _eyeLeapT = 0.4f; // range (0, 1)
#elif UNITY_IOS
        private float _eyeLeapT = 0.6f;
#elif UNITY_ANDROID
        private float _eyeLeapT = 0.7f;
#endif
        //[Range(0, 1)]
        private float _mouthLeapT = 0.65f; // range (0, 1)

        //[Range(0, 1)]
        private float _eyebrowLeapT = 0.2f; // range (0, 1)

        //[Range(0, 1)]
        private float __blendshapeT = 0.5f; // range (0, 1)

        // 얼굴 표정 비율 거리변수
        public static float DistanceOfLeftEyeHeight;

        public static float DistanceOfRightEyeHeight;

        public static float DistanceOfNoseHeight;

        public static float DistanceOfMouthWidth;

        public static float DistanceOfMouthHeight;

        public static float DistanceOfCenterMouth;

        public static float DistanceBetweenLeftPupilAndEyebrow;

        public static float DistanceBetweenRightPupilAndEyebrow;

        public static float DistanceBetweenBottomNoseAndJaw;

        public static float DistanceBetweenBottomNoseAndBottomMouth;

        public static float DistanceBetweenBottomNoseAndtopMouth;

        public static float DistanceBetweenNearLeftMouth;

        public static float DistanceBetweenNearRightMouth;

        public static float DistanceOfJawHeight;

        public static float DistanceBetweenJawAndTopMouth;

        // 사용하진 않지만 구현은 해둠
        // public static float distanceOfMouthLeftWidth;
        // public static float distanceOfMouthRightWidth;
        // public static float distanceBetweenLeftendmouthandtopnose;
        // public static float distanceBetweenRightendmouthandtopnose;
        // public static float distanceBetweenBottomLipandJaw;

        public static float L_Eye_Ratio;

        public static float R_Eye_Ratio;

        public static float Jaw_Open_Ratio;

        public static float L_Mouth_Wide_Ratio;

        public static float R_Mouth_Wide_Ratio;

        public static float L_Eyebrow_Up_Ratio;

        public static float R_Eyebrow_Up_Ratio;

        public static float EE_Ratio;

        public static float Woo_Ratio;

        public static float Shrug_Ratio;

        public static float Pucker_Ratio;

        public static float Wide_Ratio;

        public static float Sad_Ratio;


        [Header("[Target]")]
        public GameObject Face3D;
        public GameObject Face2D;
        private SkinnedMeshRenderer _face3D;  // Blendshape 조정할 3D버전 face 불러오기
        private SkinnedMeshRenderer _face2D;  // Blendshape 조정할 2D버전 face 불러오기

        private float _weight = 150f;  // Blendshape Weight 값을 증폭시키는 변수
        private float _eyeweight = 100f;
        private float _eyeblinkWeight = 3.3f;
        private float _eyebrowWeight = 1.5f;
        private float AAWeight = 1.5f;
        private float OwWeight = 1.2f;
        private float WooWeight = 1.5f;
        private float UmWeight = 1.5f;
        private float WideWeight = 1.4f;
        private float SadWeight = 1.5f;

#if UNITY_EDITOR 
        private float _blinkThreshold = 0.75f;
#elif UNITY_IOS
        private float _blinkThreshold = 0.65f;
#elif UNITY_ANDROID
        private float _blinkThreshold = 0.4f;
#endif
        private float _smileThreshold = 0.6f;


        // Lerp를 한 번 거쳐 Blendshape Weight에 넣을 값
        private float Jaw_Open;
        private float R_Mouth_Wide;
        private float L_Mouth_Wide;
        private float Mouth_EE;
        private float Mouth_Woo;
        private float Shrug;
        private float L_Eye_Blink;
        private float R_Eye_Blink;
        private float L_Eyebrow_Up;
        private float R_Eyebrow_Up;
        private float Mouth_Pucker;
        private float Mouth_Wide;
        private float Mouth_Sad;

        // Weight 범위 제한할 변수
        private float _leftEyeBlinkWeight;
        private float _rightEyeBlinkWeight;

        //눈 떨림 제한
        private bool _r_EyeIsOpened = true;
        private bool _l_EyeIsOpened = true;

        public List<KalmanFilter> Filter_array_mouth = new List<KalmanFilter>();
        public List<KalmanFilter> Filter_array_eye = new List<KalmanFilter>();
        public List<Mat> X_array_mouth = new List<Mat>();
        public List<Mat> X_array_eye = new List<Mat>();
        private Mat _tmpArrayMouth = new Mat();
        private Mat _tmpArrayEye = new Mat();

        private FacialRecordManager _recordManager = null;

        private newRotation _rotationManager = null;

        private ButtonEvent _buttonManager = null;

        private void Awake()
        {
            FacialRecordManager[] recordManagers = FindObjectsOfType<FacialRecordManager>();
            if (recordManagers.Length > 0) _recordManager = FindObjectsOfType<FacialRecordManager>()[0];

            newRotation[] rotationManagers = FindObjectsOfType<newRotation>();
            if (rotationManagers.Length > 0) _rotationManager = FindObjectsOfType<newRotation>()[0];

            ButtonEvent[] buttonMnagers = FindObjectsOfType<ButtonEvent>();
            if (buttonMnagers.Length > 0) _buttonManager = FindObjectsOfType<ButtonEvent>()[0];
        }

        private void Start()
        {
            _face3D = Face3D.GetComponent<SkinnedMeshRenderer>();
            _face2D = Face2D.GetComponent<SkinnedMeshRenderer>();

            KalmanFilter kal_mouth = new KalmanFilter(6, 6, 0, CvType.CV_32FC1);  // 지속적으로 변화하는 시스템에 이상적인 노이즈 제거 필터
            Mat measurement_mouth = new Mat(6, 6, CvType.CV_32FC1);
            Core.setIdentity(measurement_mouth);
            Mat transition_mouth = new Mat(6, 6, CvType.CV_32FC1);
            Core.setIdentity(transition_mouth);
            Mat processnoise_mouth = new Mat(6, 6, CvType.CV_32FC1);
            Core.setIdentity(processnoise_mouth);

            KalmanFilter kal_eye = new KalmanFilter(2, 2, 0, CvType.CV_32FC1);  // 지속적으로 변화하는 시스템에 이상적인 노이즈 제거 필터
            Mat measurement_eye = new Mat(2, 2, CvType.CV_32FC1);
            Core.setIdentity(measurement_eye);
            Mat transition_eye = new Mat(2, 2, CvType.CV_32FC1);
            Core.setIdentity(transition_eye);
            Mat processnoise_eye = new Mat(2, 2, CvType.CV_32FC1);
            Core.setIdentity(processnoise_eye);

#if UNITY_EDIOR || UNITY_IOS
            processnoise_mouth *= 0.5;
            processnoise_eye *= 0.15;  // 노이즈 제거하는 정도, 숫자 작을수록 노이즈 많이 제거함 (최대 1)
#elif UNITY_ANDROID && !UNITY_EDITOR
            processnoise_mouth *= 0.725;
            processnoise_eye *= 0.3;
#endif
            kal_mouth.set_measurementMatrix(measurement_mouth);
            kal_mouth.set_transitionMatrix(transition_mouth);
            kal_mouth.set_processNoiseCov(processnoise_mouth);

            kal_eye.set_measurementMatrix(measurement_eye);
            kal_eye.set_transitionMatrix(transition_eye);
            kal_eye.set_processNoiseCov(processnoise_eye);


            _tmpArrayMouth = Mat.zeros(6, 1, CvType.CV_32FC1);
            Filter_array_mouth.Add(kal_mouth);
            X_array_mouth.Add(_tmpArrayMouth);

            _tmpArrayEye = Mat.zeros(2, 1, CvType.CV_32FC1);
            Filter_array_eye.Add(kal_eye);
            X_array_eye.Add(_tmpArrayEye);
        }

        public static void CalculateRatio()
        {
            if (AllVariables.instance.topRightEye == null || AllVariables.instance.LeftMouth == null)
                return;

            DistanceOfLeftEyeHeight = new Vector3(AllVariables.instance.topRightEye.X - AllVariables.instance.bottomRightEye.X, AllVariables.instance.topRightEye.Y - AllVariables.instance.bottomRightEye.Y, AllVariables.instance.topRightEye.Z - AllVariables.instance.bottomRightEye.Z).magnitude;
            DistanceOfRightEyeHeight = new Vector3(AllVariables.instance.topLeftEye.X - AllVariables.instance.bottomLeftEye.X, AllVariables.instance.topLeftEye.Y - AllVariables.instance.bottomLeftEye.Y, AllVariables.instance.topLeftEye.Z - AllVariables.instance.bottomLeftEye.Z).magnitude;
            DistanceOfNoseHeight = new Vector3(AllVariables.instance.topNose.X - AllVariables.instance.bottomNose.X, AllVariables.instance.topNose.Y - AllVariables.instance.bottomNose.Y, AllVariables.instance.topNose.Z - AllVariables.instance.bottomNose.Z).magnitude;
            DistanceOfMouthWidth = new Vector3(AllVariables.instance.LeftMouth.X - AllVariables.instance.RightMouth.X, AllVariables.instance.LeftMouth.Y - AllVariables.instance.RightMouth.Y, AllVariables.instance.LeftMouth.Z - AllVariables.instance.RightMouth.Z).magnitude;
            DistanceOfMouthHeight = new Vector3(AllVariables.instance.topMouth.X - AllVariables.instance.bottomMouth.X, AllVariables.instance.topMouth.Y - AllVariables.instance.bottomMouth.Y, AllVariables.instance.topMouth.Z - AllVariables.instance.bottomMouth.Z).magnitude;
            DistanceOfCenterMouth = new Vector3(AllVariables.instance.CenterTopMouth.X - AllVariables.instance.CenterBottomMouth.X, AllVariables.instance.CenterTopMouth.Y - AllVariables.instance.CenterBottomMouth.Y, AllVariables.instance.CenterTopMouth.Z - AllVariables.instance.CenterBottomMouth.Z).magnitude;
            DistanceBetweenLeftPupilAndEyebrow = new Vector3(AllVariables.instance.centerLeftEyebrow.X - (AllVariables.instance.leftLeftEye.X + AllVariables.instance.rightLeftEye.X) / 2, AllVariables.instance.centerLeftEyebrow.Y - (AllVariables.instance.leftLeftEye.Y + AllVariables.instance.rightLeftEye.Y) / 2, AllVariables.instance.centerLeftEyebrow.Z - (AllVariables.instance.leftLeftEye.Z + AllVariables.instance.rightLeftEye.Z) / 2).magnitude;
            DistanceBetweenRightPupilAndEyebrow = new Vector3(AllVariables.instance.centerRightEyebrow.X - (AllVariables.instance.leftRightEye.X + AllVariables.instance.rightRightEye.X) / 2, AllVariables.instance.centerRightEyebrow.Y - (AllVariables.instance.leftRightEye.Y + AllVariables.instance.rightRightEye.Y) / 2, AllVariables.instance.centerRightEyebrow.Z - (AllVariables.instance.leftRightEye.Z + AllVariables.instance.rightRightEye.Z) / 2).magnitude;
            DistanceBetweenBottomNoseAndJaw = new Vector3(AllVariables.instance.bottomNose.X - AllVariables.instance.Jaw.X, AllVariables.instance.bottomNose.Y - AllVariables.instance.Jaw.Y, AllVariables.instance.bottomNose.Z - AllVariables.instance.Jaw.Z).magnitude;
            DistanceBetweenBottomNoseAndBottomMouth = new Vector3(AllVariables.instance.bottomNose.X - AllVariables.instance.bottomMouth.X, AllVariables.instance.bottomNose.Y - AllVariables.instance.bottomMouth.Y, AllVariables.instance.bottomNose.Z - AllVariables.instance.bottomMouth.Z).magnitude;
            DistanceBetweenBottomNoseAndtopMouth = new Vector3(AllVariables.instance.bottomNose.X - AllVariables.instance.topMouth.X, AllVariables.instance.bottomNose.Y - AllVariables.instance.topMouth.Y, AllVariables.instance.bottomNose.Z - AllVariables.instance.topMouth.Z).magnitude;

            // sad blendshape 계산_1
            DistanceBetweenNearLeftMouth = new Vector3(AllVariables.instance.belowLeftMouth.X - AllVariables.instance.LeftMouth.X, AllVariables.instance.belowLeftMouth.Y - AllVariables.instance.LeftMouth.Y, AllVariables.instance.belowLeftMouth.Z - AllVariables.instance.LeftMouth.Z).magnitude;
            DistanceBetweenNearRightMouth = new Vector3(AllVariables.instance.belowRightMouth.X - AllVariables.instance.RightMouth.X, AllVariables.instance.belowRightMouth.Y - AllVariables.instance.RightMouth.Y, AllVariables.instance.belowRightMouth.Z - AllVariables.instance.RightMouth.Z).magnitude;

            // sad blendshape 계산_8
            DistanceOfJawHeight = new Vector3(AllVariables.instance.Jaw.X - AllVariables.instance.bottomLip.X, AllVariables.instance.Jaw.Y - AllVariables.instance.bottomLip.Y, AllVariables.instance.Jaw.Z - AllVariables.instance.bottomLip.Z).magnitude;
            DistanceBetweenJawAndTopMouth = new Vector3(AllVariables.instance.Jaw.X - AllVariables.instance.topMouth.X, AllVariables.instance.Jaw.Y - AllVariables.instance.topMouth.Y, AllVariables.instance.Jaw.Z - AllVariables.instance.topMouth.Z).magnitude;

            L_Eye_Ratio = GetLeftEyeCloseRatio(DistanceOfLeftEyeHeight, DistanceOfNoseHeight);
            R_Eye_Ratio = GetRightEyeCloseRatio(DistanceOfRightEyeHeight, DistanceOfNoseHeight);
            Jaw_Open_Ratio = GetJawOpenRatio(DistanceOfCenterMouth, DistanceOfNoseHeight);
            L_Eyebrow_Up_Ratio = GetLeftEyebrowUPRatio(DistanceBetweenLeftPupilAndEyebrow, DistanceOfNoseHeight);
            R_Eyebrow_Up_Ratio = GetRightEyebrowUPRatio(DistanceBetweenRightPupilAndEyebrow, DistanceOfNoseHeight);
            EE_Ratio = GetMouthEERatio(DistanceOfCenterMouth, DistanceOfMouthWidth, DistanceBetweenBottomNoseAndJaw);
            Shrug_Ratio = GetMouthShrugRatio(DistanceOfMouthHeight, DistanceOfNoseHeight);
            Pucker_Ratio = GetMouthPuckerRatio(DistanceOfMouthWidth, DistanceBetweenBottomNoseAndJaw, DistanceOfNoseHeight);
            Woo_Ratio = GetMouthWooRatio(DistanceBetweenBottomNoseAndtopMouth, DistanceOfNoseHeight);
            Wide_Ratio = GetMouthWideRatio(DistanceOfMouthWidth, DistanceOfNoseHeight);
            Sad_Ratio = GetMouthSadRatio(DistanceOfJawHeight, DistanceBetweenJawAndTopMouth, DistanceBetweenNearLeftMouth, DistanceBetweenNearRightMouth, DistanceOfNoseHeight);
            //print("Sad_Ratio : " + Sad_Ratio); 
        }

        public static float GetLeftEyeCloseRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceOfLeftEyeHeight / DistanceOfNoseHeight)
#if UNITY_EDITOR
            return Mathf.InverseLerp(0.18f, 0.03f, ratio);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return Mathf.InverseLerp(0.18f, 0.05f, ratio);
#endif
        }

        public static float GetRightEyeCloseRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceOfRightEyeHeight / DistanceOfNoseHeight)
#if UNITY_EDITOR
            return Mathf.InverseLerp(0.18f, 0.03f, ratio);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return Mathf.InverseLerp(0.18f, 0.05f, ratio);
#endif
        }

        public static float GetLeftEyebrowUPRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceBetweenLeftPupilAndEyebrow / DistanceOfNoseHeight)
            return Mathf.InverseLerp(0.40f, 0.60f, ratio);
        }

        public static float GetRightEyebrowUPRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceBetweenRightPupilAndEyebrow / DistanceOfNoseHeight)
            return Mathf.InverseLerp(0.40f, 0.60f, ratio);
        }

        public static float GetJawOpenRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceOfCenterMouth / DistanceOfNoseHeight)
            return Mathf.InverseLerp(0.02f, 0.80f, ratio);
        }

        public static float GetMouthShrugRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceOfMouthHeight / DistanceOfNoseHeight)
            //print("result : " + ratio);
            return Mathf.InverseLerp(0.40f, 0.03f, ratio);
        }

        public static float GetMouthWooRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceBetweenBottomNoseAndtopMouth / DistanceOfNoseHeight)
            return Mathf.InverseLerp(0.30f, 0.10f, ratio);
        }

        public static float GetMouthPuckerRatio(float num1, float num2, float num3)
        {
            float ratio1 = (num1 / num3);  // (DistanceOfMouthWidth / DistanceOfNoseHeight)
            float ratio2 = (num2 / num3);  // (DistanceBetweenBottomNoseAndJaw / DistanceOfNoseHeight)
#if UNITY_EDITOR
            float newratio1 = Mathf.InverseLerp(0.7f, 0.5f, ratio1);
            float newratio2 = Mathf.InverseLerp(0.87f, 1.20f, ratio2);

            float ratio = 0f;

            if (newratio1 > 0.5f && newratio2 > 0.5f)
            {
                ratio = (newratio1 + newratio2) / 2;
            }
            return Mathf.InverseLerp(0.5f, 1.0f, ratio);

#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            float newratio1 = Mathf.InverseLerp(1.20f, 0.70f, ratio1);
            float newratio2 = Mathf.InverseLerp(1.30f, 1.70f, ratio2);

            float ratio = 0f;

            if (newratio1 > 0.3f && newratio2 > 0.5f)
            {
                ratio = (newratio1 + newratio2) / 2;
            }

            return Mathf.InverseLerp(0.40f, 1.00f, ratio);
#endif
        }

        public static float GetMouthEERatio(float num1, float num2, float num3)
        {
            float ratio1 = (num1 / num3);  // (DistanceOfCenterMouth / DistanceBetweenBottomNoseAndJaw)
            float ratio2 = (num2 / num3);  // (DistanceOfMouthWidth / DistanceBetweenBottomNoseAndJaw)

            float newratio1 = Mathf.InverseLerp(0.00f, 0.60f, ratio1);
            float newratio2 = Mathf.InverseLerp(0.00f, 0.80f, ratio2);

            float ratio = 0f;

            if (newratio1 < 0.6f && newratio2 > 0.3f)
            {
                ratio = (newratio1 + newratio2) / 2;
            }
#if UNITY_EDITOR
            return Mathf.InverseLerp(0.30f, 0.70f, ratio);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return Mathf.InverseLerp(0.55f, 0.70f, ratio);
#endif
        }

        public static float GetMouthWideRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);  // (DistanceOfMouthWidth / DistanceOfNoseHeight)

#if UNITY_EDITOR
            return Mathf.InverseLerp(0.65f, 0.95f, ratio);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return Mathf.InverseLerp(1.15f, 1.50f, ratio);
#endif
        }

        public static float GetMouthSadRatio(float num1, float num2, float num3, float num4, float num5)
        {
            float jawRatio = num1 / num2;
            float mouthRatio = (num3 + num4) / num5;
            //print("jawRatio : " + jawRatio);
            //print("mouthRatio : " + mouthRatio);
#if UNITY_EDITOR
            float jawRatioResult = Mathf.InverseLerp(0.67f, 0.72f, jawRatio);
            float mouthRatioResult = Mathf.InverseLerp(0.22f, 0.16f, mouthRatio);
#elif UNITY_IOS
            float jawRatioResult = Mathf.InverseLerp(0.675f, 0.725f, jawRatio);
            float mouthRatioResult = Mathf.InverseLerp(0.22f, 0.16f, mouthRatio);
#elif UNITY_ANDROID
            float jawRatioResult = Mathf.InverseLerp(0.67f, 0.72f, jawRatio);
            float mouthRatioResult = Mathf.InverseLerp(0.22f, 0.16f, mouthRatio);
#endif
            float resultRatio = 0;

            if (Wide_Ratio > 0.3 || Shrug_Ratio > 0.45) resultRatio = ((jawRatioResult * 0.5f) + (mouthRatioResult * 1.5f)) / 2;  // jaw가 커야한다
            else resultRatio = ((jawRatioResult * 1.6f) + (mouthRatioResult * 0.4f)) / 2; // mouth가 커야한다

            return resultRatio;
        }


        public void UpdateFaceAnimation()
        {
            Jaw_Open = Mathf.Lerp(Jaw_Open, Jaw_Open_Ratio * _weight, _mouthLeapT);
            L_Mouth_Wide = Mathf.Lerp(L_Mouth_Wide, L_Mouth_Wide_Ratio * _weight, _mouthLeapT);
            R_Mouth_Wide = Mathf.Lerp(R_Mouth_Wide, R_Mouth_Wide_Ratio * _weight, _mouthLeapT);
            Mouth_Woo = Mathf.Lerp(Mouth_Woo, Woo_Ratio * _weight, _mouthLeapT);
            Mouth_EE = Mathf.Lerp(Mouth_EE, EE_Ratio * _weight, _mouthLeapT);
            Shrug = Mathf.Lerp(Shrug, Shrug_Ratio * _weight, _mouthLeapT);
            L_Eye_Blink = Mathf.Lerp(L_Eye_Blink, L_Eye_Ratio * _eyeweight, _eyeLeapT);
            R_Eye_Blink = Mathf.Lerp(R_Eye_Blink, R_Eye_Ratio * _eyeweight, _eyeLeapT);
            L_Eyebrow_Up = Mathf.Lerp(L_Eyebrow_Up, L_Eyebrow_Up_Ratio * _weight, _eyebrowLeapT);
            R_Eyebrow_Up = Mathf.Lerp(R_Eyebrow_Up, R_Eyebrow_Up_Ratio * _weight, _eyebrowLeapT);
            Mouth_Pucker = Mathf.Lerp(Mouth_Pucker, Pucker_Ratio * _weight, _mouthLeapT);
            Mouth_Wide = Mathf.Lerp(Mouth_Wide, Wide_Ratio * _weight, _mouthLeapT);
            Mouth_Sad = Mathf.Lerp(Mouth_Sad, Sad_Ratio * _weight, _mouthLeapT);
        }

        private void FixedUpdate()
        {
            CalculateRatio();
        }

        private void LateUpdate()
        {

            // 감정 분석값에 따라 2D Layer 변하도록 
            //switch (EmotionText.text)
            //{
            //    case "Neutral":
            //        _buttonManager.Face();
            //        break;
            //    case "Happy":
            //        _buttonManager.Happy();
            //        break;
            //    case "Surprised":
            //        _buttonManager.Surprised();
            //        break;
            //    case "Angry":
            //        _buttonManager.Angry();
            //        break;
            //    case "Sad":
            //        _buttonManager.Touched();
            //        break;
            //}

            UpdateFaceAnimation();
            if (_recordManager != null && _recordManager.IsPlaying) return;


            //원래 android 에서만 있던 코드 시작 ===
            // Kalman Filter 생성
            Mat observation_mouth = new Mat();
            observation_mouth = Mat.zeros(6, 1, CvType.CV_32FC1); // all value initialize to 0
            observation_mouth.put(0, 0, Jaw_Open);
            observation_mouth.put(1, 0, Mouth_Pucker);
            observation_mouth.put(2, 0, Mouth_Woo);
            observation_mouth.put(3, 0, Shrug);
            observation_mouth.put(4, 0, Mouth_Wide);
            observation_mouth.put(5, 0, Mouth_Sad);

            for (int j = 0; j < 1; j++)
            {
                Filter_array_mouth[j].correct(observation_mouth);
                X_array_mouth[j] = Filter_array_mouth[j].predict();
            }

            double[] Jaw_Open_Array = X_array_mouth[0].get(0, 0);
            double[] Mouth_Pucker_Array = X_array_mouth[0].get(1, 0);
            double[] Mouth_Woo_Array = X_array_mouth[0].get(2, 0);
            double[] Shrug_Array = X_array_mouth[0].get(3, 0);
            double[] Mouth_Wide_Array = X_array_mouth[0].get(4, 0);
            double[] Mouth_Sad_Array = X_array_mouth[0].get(5, 0);

            Jaw_Open = (float)Jaw_Open_Array[0];
            Mouth_Pucker = (float)Mouth_Pucker_Array[0];
            Mouth_Woo = (float)Mouth_Woo_Array[0];
            Shrug = (float)Shrug_Array[0];
            Mouth_Wide = (float)Mouth_Wide_Array[0];
            Mouth_Sad = (float)Mouth_Sad_Array[0];

            //원래 android 에서만 있던 코드 끝 ===

            if (Face3D.activeSelf == false)  // 만약 Face3D가 비활성화 되어있으면
            {
                if (Face2D != null)  // Face2D로 blendshape 동작
                {
                    if (_face2D.sharedMesh.blendShapeCount == 0) return;
                    if (EnableMouth)
                    {
                        _face2D.SetBlendShapeWeight(2, Mathf.Lerp(0f, Jaw_Open * AAWeight, __blendshapeT));
                        _face2D.SetBlendShapeWeight(4, Mathf.Lerp(0f, Mouth_Pucker * OwWeight, __blendshapeT));
                        _face2D.SetBlendShapeWeight(5, Mathf.Lerp(0f, Mouth_Woo * WooWeight, __blendshapeT));
                        _face2D.SetBlendShapeWeight(6, Mathf.Lerp(0f, Shrug * UmWeight, __blendshapeT));
                        _face2D.SetBlendShapeWeight(0, Mathf.Lerp(0f, Mouth_Wide * WideWeight, __blendshapeT));
                        // Sad BlendShape 추가 
                        _face2D.SetBlendShapeWeight(1, Mathf.Lerp(0f, Mouth_Sad * SadWeight, __blendshapeT));
                    }
                }
                else
                {
                    Debug.LogError("There is no Face2D");
                }
            }
            else  // 만약 Face3D가 활성화 되어있으면
            {

                if (Face3D != null)  // Face3D로 blendshape 동작
                {
                    if (_face3D.sharedMesh.blendShapeCount == 0) return;

                    /// Kalman Filter 생성
                    Mat observation_eye = new Mat();
                    observation_eye = Mat.zeros(2, 1, CvType.CV_32FC1); // all value initialize to 0
                    observation_eye.put(0, 0, L_Eye_Blink);
                    observation_eye.put(1, 0, R_Eye_Blink);

                    for (int j = 0; j < 1; j++)
                    {
                        Filter_array_eye[j].correct(observation_eye);
                        X_array_eye[j] = Filter_array_eye[j].predict();
                    }

                    double[] L_Eye_Blink_Array = X_array_eye[0].get(0, 0);
                    double[] R_Eye_Blink_Array = X_array_eye[0].get(1, 0);

                    _leftEyeBlinkWeight = Mathf.Clamp((float)L_Eye_Blink_Array[0], 0, 100);
                    _rightEyeBlinkWeight = Mathf.Clamp((float)R_Eye_Blink_Array[0], 0, 100);

                    if (EnableEye)
                    {
                        if (Wide_Ratio >= _smileThreshold)  // Smile일 때 눈이 다 감기지 않도록
                        {
                            _leftEyeBlinkWeight = Mathf.Clamp(_leftEyeBlinkWeight, 0, 64);
                            _rightEyeBlinkWeight = Mathf.Clamp(_rightEyeBlinkWeight, 0, 64);
                        }

                        // Blink Threshold Change
#if UNITY_EDITOR
                        if (_rotationManager.head.localRotation.z < 0) _blinkThreshold = 0.45f; // Rotation.z 값은 최소 -0.173
                        else _blinkThreshold = 0.75f;
#elif UNITY_IOS
                        if (_rotationManager.head.localRotation.z < -0.01) _blinkThreshold = 0.5f; // Rotation.z 값은 최소 -0.173
                        else _blinkThreshold = 0.65f;
#elif UNITY_ANDROID
                        if (_rotationManager.head.localRotation.z < -0.01) _blinkThreshold = 0.25f; // Rotation.z 값은 최소 -0.173
                        else _blinkThreshold = 0.4f;
#endif
                        if (R_Eye_Ratio > _blinkThreshold - 0.05 && R_Eye_Ratio <= _blinkThreshold + 0.05)
                        {
                            if (!_r_EyeIsOpened) _face3D.SetBlendShapeWeight(12, Mathf.Lerp(_rightEyeBlinkWeight, 115f, _eyeLeapT)); //F_Eye_R_Close
                        }
                        else if (R_Eye_Ratio > _blinkThreshold + 0.05)
                        {
                            _face3D.SetBlendShapeWeight(12, Mathf.Lerp(_rightEyeBlinkWeight, 115f, _eyeLeapT));  // 일정 범위 이상에서 눈이 다 감기도록
                            _r_EyeIsOpened = false;
                        }
                        //else if (R_Eye_Ratio < _blinkThreshold)
                        else if (R_Eye_Ratio <= _blinkThreshold - 0.05)
                        {
                            _face3D.SetBlendShapeWeight(12, Mathf.Lerp(0f, _rightEyeBlinkWeight, _eyeLeapT)); 
                            _r_EyeIsOpened = true;
                        }

                        //if (R_Eye_Ratio > _blinkThreshold)
                        //{
                        //    _face3D.SetBlendShapeWeight(10, Mathf.Lerp(_rightEyeBlinkWeight, 110f, _eyeLeapT));  // 일정 범위 이상에서 눈이 다 감기도록
                        //    _r_EyeIsOpened = false;
                        //}
                        //else if (R_Eye_Ratio <= _blinkThreshold)
                        //{
                        //    _face3D.SetBlendShapeWeight(10, Mathf.Lerp(0f, _rightEyeBlinkWeight, _eyeLeapT));
                        //    _r_EyeIsOpened = true;
                        //}

                        if (L_Eye_Ratio > _blinkThreshold - 0.05 && L_Eye_Ratio <= _blinkThreshold + 0.05)
                        {
                            if (!_l_EyeIsOpened) _face3D.SetBlendShapeWeight(14, Mathf.Lerp(_leftEyeBlinkWeight, 115f, _eyeLeapT)); //F_Eye_L_Close
                        }
                        else if (L_Eye_Ratio > _blinkThreshold + 0.05)
                        {
                            _face3D.SetBlendShapeWeight(14, Mathf.Lerp(_leftEyeBlinkWeight, 115f, _eyeLeapT));  // 일정 범위 이상에서 눈이 다 감기도록
                            _l_EyeIsOpened = false;
                        }
                        //else if (R_Eye_Ratio < _blinkThreshold)
                        else if (L_Eye_Ratio <= _blinkThreshold - 0.05)
                        {
                            _face3D.SetBlendShapeWeight(14, Mathf.Lerp(0f, _leftEyeBlinkWeight, _eyeLeapT));
                            _l_EyeIsOpened = true;
                        }

                        //if (L_Eye_Ratio > _blinkThreshold)
                        //{
                        //    _face3D.SetBlendShapeWeight(12, Mathf.Lerp(_leftEyeBlinkWeight, 110f, _eyeLeapT));  // 일정 범위 이상에서 눈이 다 감기도록
                        //    _l_EyeIsOpened = false;
                        //}
                        //else if (L_Eye_Ratio <= _blinkThreshold)
                        //{
                        //    _face3D.SetBlendShapeWeight(12, Mathf.Lerp(0f, _leftEyeBlinkWeight, _eyeLeapT));
                        //    _l_EyeIsOpened = true;
                        //}
                        //print("R_Eye_Ratio : " + R_Eye_Ratio);
                        //print("_rightEyeBlinkWeight : " + _rightEyeBlinkWeight);
                    }

                    // if (EnableBrow)
                    // {
                    //     _face3D.SetBlendShapeWeight(0, Mathf.Lerp(0f, ((float)R_Brow_Up_array[0]) * _eyebrowWeight, _eyeLeapT));
                    //     _face3D.SetBlendShapeWeight(1, Mathf.Lerp(0f, ((float)L_Brow_Up_array[0]) * _eyebrowWeight, _eyeLeapT));
                    // }

                    if (EnableMouth)
                    {
                        _face3D.SetBlendShapeWeight(2, Mathf.Lerp(0f, Jaw_Open * AAWeight, __blendshapeT)); //F_AA
                        _face3D.SetBlendShapeWeight(4, Mathf.Lerp(0f, Mouth_Pucker * OwWeight, __blendshapeT));//F_OW
                        _face3D.SetBlendShapeWeight(5, Mathf.Lerp(0f, Mouth_Woo * WooWeight, __blendshapeT));//F_Woo
                        _face3D.SetBlendShapeWeight(6, Mathf.Lerp(0f, Shrug * UmWeight, __blendshapeT));//F_UM
                        _face3D.SetBlendShapeWeight(0, Mathf.Lerp(0f, Mouth_Wide * WideWeight, __blendshapeT));//F_Emo_Smile
                        // Sad BlendShape 추가
                        if (_face3D.sharedMesh.blendShapeCount >= 18) _face3D.SetBlendShapeWeight(1, Mathf.Lerp(0f, Mouth_Sad * SadWeight, __blendshapeT));//F_Emo_Sad
                    }
                }
                else
                {
                    Debug.LogError("There is no Face3D");
                }
            }
        }
    }
}