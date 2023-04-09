using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

namespace Mediapipe.Unity.Tutorial
{
    public class IrisTracking : MonoBehaviour
    {
        private Vector3 BothIrisPos;  // 왼쪽 눈동자와 오른쪽 눈동자의 평균 좌표
        private Vector3 LeftIrisPos;  // 왼쪽 눈동자 좌표
        private Vector3 RightIrisPos;  // 오른쪽 눈동자 좌표

        // 왼쪽 눈을 감았을 때
        private float _distanceOfCenterIrisandFrontEye_LeftClosed;
        private float _distanceOfCenterIrisandBottomEye_LeftClosed;
        private float _distanceOfEyeWidth_LeftClosed;
        private float _distanceOfEyeHeight_LeftClosed;
        private float _eye_Left_Ratio_LeftClosed;
        private float _eye_Right_Ratio_LeftClosed;
        private float _eye_Up_Ratio_LeftClosed;
        private float _iris_Left_LeftClosed;
        private float _iris_Right_LeftClosed;
        private float _iris_Up_LeftClosed;

        // 오른쪽 눈을 감았을 때
        private float _distanceOfCenterIrisandFrontEye_RightClosed;
        private float _distanceOfCenterIrisandBottomEye_RightClosed;
        private float _distanceOfEyeWidth_RightClosed;
        private float _distanceOfEyeHeight_RightClosed;
        private float _eye_Left_Ratio_RightClosed;
        private float _eye_Right_Ratio_RightClosed;
        private float _eye_Up_Ratio_RightClosed;
        private float _iris_Left_RightClosed;
        private float _iris_Right_RightClosed;
        private float _iris_Up_RightClosed;

        // 두 눈을 뜨거나 감았을 때
        private float _distanceOfCenterIrisandFrontEye;
        private float _distanceOfCenterIrisandBottomEye;
        private float _eye_Left_Ratio;
        private float _eye_Right_Ratio;
        private float _eye_Up_Ratio;
        private float _iris_Left;
        private float _iris_Right;
        private float _iris_Up;

        private float _weight = 100f;  // Blendshape 값을 증폭
        private float _closedWeight = 150f;
        private float _eyeBlinkWeight = 0.05f;  // 눈 감는 비율 threshold
        private float _irisLeapT = 0.6f;  // Blendshape Lerp values 조절

        public SkinnedMeshRenderer Head;
        private AllVariables singleton;

        public List<KalmanFilter> Filter_array = new List<KalmanFilter>();
        public List<Mat> X_array = new List<Mat>();
        private Mat _tmpArray = new Mat();

        private FacialRecordManager _recordManager = null;

        private void Awake()
        {
            FacialRecordManager[] recordManagers = FindObjectsOfType<FacialRecordManager>();
            if (recordManagers.Length > 0) _recordManager = FindObjectsOfType<FacialRecordManager>()[0];
        }

        void Start()
        {
            KalmanFilter kal = new KalmanFilter(9, 9, 0, CvType.CV_32FC1);  // 지속적으로 변화하는 시스템에 이상적인 노이즈 제거 필터
            Mat measurement = new Mat(9, 9, CvType.CV_32FC1);
            Core.setIdentity(measurement);
            Mat transition = new Mat(9, 9, CvType.CV_32FC1);
            Core.setIdentity(transition);
            Mat processnoise = new Mat(9, 9, CvType.CV_32FC1);
            Core.setIdentity(processnoise);

#if UNITY_EDIOR || UNITY_IOS
            processnoise *= 0.01;  // 노이즈 제거하는 정도, 숫자 작을수록 노이즈 많이 제거함 (최대 1)
#elif UNITY_ANDROID && !UNITY_EDITOR
            processnoise *= 0.045;
#endif

            kal.set_measurementMatrix(measurement);
            kal.set_transitionMatrix(transition);
            kal.set_processNoiseCov(processnoise);

            _tmpArray = Mat.zeros(9, 1, CvType.CV_32FC1);
            Filter_array.Add(kal);
            X_array.Add(_tmpArray);
        }

        private void FixedUpdate()
        {
            singleton = AllVariables.instance;  // 싱글톤 변수 정의

            if (singleton.topLeftEye == null || singleton.bottomLeftEye == null ||
                singleton.leftLeftEye == null || singleton.rightLeftEye == null ||
                singleton.leftIris == null || singleton.rightIris == null)
                return;

            else
            {
                BothIrisPos = new Vector3((singleton.leftIris.X * 50 + singleton.rightIris.X * 50), (singleton.leftIris.Y * 50 + singleton.rightIris.Y * 50), (singleton.leftIris.Z * 50 + singleton.rightIris.Z * 50));  // 왼쪽 눈동자와 오른쪽 눈동자의 평균 좌표
                LeftIrisPos = new Vector3(singleton.leftIris.X * 100, singleton.leftIris.Y * 100, singleton.leftIris.Z * 100);  // 왼쪽 눈동자 좌표
                RightIrisPos = new Vector3(singleton.rightIris.X * 100, singleton.rightIris.Y * 100, singleton.rightIris.Z * 100);  // 오른쪽 눈동자 좌표

                // 왼쪽 눈을 감았을 때
                _distanceOfCenterIrisandFrontEye_LeftClosed = new Vector3(singleton.rightRightEye.X * 100 - RightIrisPos.x, singleton.rightRightEye.Y * 100 - RightIrisPos.y, singleton.rightRightEye.Z * 100 - RightIrisPos.z).magnitude;
                _distanceOfCenterIrisandBottomEye_LeftClosed = new Vector3(singleton.bottomRightEye.X * 100 - RightIrisPos.x, singleton.bottomRightEye.Y * 100 - RightIrisPos.y, singleton.bottomRightEye.Z * 100 - RightIrisPos.z).magnitude;
                _distanceOfEyeWidth_LeftClosed = new Vector3(singleton.rightRightEye.X * 100 - singleton.leftRightEye.X * 100, singleton.rightRightEye.Y * 100 - singleton.leftRightEye.Y * 100, singleton.rightRightEye.Z * 100 - singleton.leftRightEye.Z * 100).magnitude;
                _distanceOfEyeHeight_LeftClosed = new Vector3(singleton.topRightEye.X * 100 - singleton.bottomRightEye.X * 100, singleton.topRightEye.Y * 100 - singleton.bottomRightEye.Y * 100, singleton.topRightEye.Z * 100 - singleton.bottomRightEye.Z * 100).magnitude;

                _eye_Left_Ratio_LeftClosed = GetEyeLeftRatio_LeftClosed(_distanceOfCenterIrisandFrontEye_LeftClosed, _distanceOfEyeWidth_LeftClosed);
                _eye_Right_Ratio_LeftClosed = GetEyeRightRatio_LeftClosed(_distanceOfCenterIrisandFrontEye_LeftClosed, _distanceOfEyeWidth_LeftClosed);
                _eye_Up_Ratio_LeftClosed = GetEyeUpRatio_LeftClosed(_distanceOfCenterIrisandBottomEye_LeftClosed, _distanceOfEyeWidth_LeftClosed);

                // 오른쪽 눈을 감았을 때
                _distanceOfCenterIrisandFrontEye_RightClosed = new Vector3(singleton.rightLeftEye.X * 100 - LeftIrisPos.x, singleton.rightLeftEye.Y * 100 - LeftIrisPos.y, singleton.rightLeftEye.Z * 100 - LeftIrisPos.z).magnitude;
                _distanceOfCenterIrisandBottomEye_RightClosed = new Vector3(singleton.bottomLeftEye.X * 100 - LeftIrisPos.x, singleton.bottomLeftEye.Y * 100 - LeftIrisPos.y, singleton.bottomLeftEye.Z * 100 - LeftIrisPos.z).magnitude;
                _distanceOfEyeWidth_RightClosed = new Vector3(singleton.rightLeftEye.X * 100 - singleton.leftLeftEye.X * 100, singleton.rightLeftEye.Y * 100 - singleton.leftLeftEye.Y * 100, singleton.rightLeftEye.Z * 100 - singleton.leftLeftEye.Z * 100).magnitude;
                _distanceOfEyeHeight_RightClosed = new Vector3(singleton.topLeftEye.X * 100 - singleton.bottomLeftEye.X * 100, singleton.topLeftEye.Y * 100 - singleton.bottomLeftEye.Y * 100, singleton.topLeftEye.Z * 100 - singleton.bottomLeftEye.Z * 100).magnitude;

                _eye_Left_Ratio_RightClosed = GetEyeLeftRatio_RightClosed(_distanceOfCenterIrisandFrontEye_RightClosed, _distanceOfEyeWidth_RightClosed);
                _eye_Right_Ratio_RightClosed = GetEyeRightRatio_RightClosed(_distanceOfCenterIrisandFrontEye_RightClosed, _distanceOfEyeWidth_RightClosed);
                _eye_Up_Ratio_RightClosed = GetEyeUpRatio_RightClosed(_distanceOfCenterIrisandBottomEye_RightClosed, _distanceOfEyeWidth_RightClosed);

                // 두 눈을 뜨거나 감았을 때
                _distanceOfCenterIrisandFrontEye = new Vector3((singleton.rightLeftEye.X * 50 + singleton.rightRightEye.X * 50) - BothIrisPos.x, (singleton.rightLeftEye.Y * 50 + singleton.rightRightEye.Y * 50) - BothIrisPos.y, (singleton.rightLeftEye.Z * 50 + singleton.rightRightEye.Z * 50) - BothIrisPos.z).magnitude;
                _distanceOfCenterIrisandBottomEye = new Vector3((singleton.bottomLeftEye.X * 50 + singleton.bottomRightEye.X * 50) - BothIrisPos.x, (singleton.bottomLeftEye.Y * 50 + singleton.bottomRightEye.Y * 50) - BothIrisPos.y, (singleton.bottomLeftEye.Z * 50 + singleton.bottomRightEye.Z * 50) - BothIrisPos.z).magnitude;

                _eye_Left_Ratio = GetEyeLeftRatio(_distanceOfCenterIrisandFrontEye, (_distanceOfEyeWidth_LeftClosed + _distanceOfEyeWidth_RightClosed) / 2);
                _eye_Right_Ratio = GetEyeRightRatio(_distanceOfCenterIrisandFrontEye, (_distanceOfEyeWidth_LeftClosed + _distanceOfEyeWidth_RightClosed) / 2);
                _eye_Up_Ratio = GetEyeUpRatio(_distanceOfCenterIrisandBottomEye, (_distanceOfEyeWidth_LeftClosed + _distanceOfEyeWidth_RightClosed) / 2);
            }
        }

        // 두 눈이 떠있을 때
        private float GetEyeLeftRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.31f, 0.55f, ratio);
        }

        private float GetEyeRightRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.29f, 0.15f, ratio);
        }

        private float GetEyeUpRatio(float num1, float num2)
        {
            float ratio = (num1 / num2);
#if UNITY_EDITOR
            return Mathf.InverseLerp(0.34f, 0.37f, ratio);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return Mathf.InverseLerp(0.28f, 0.32f, ratio);
#endif
        }

        // 왼쪽 눈을 감았을 때
        private float GetEyeLeftRatio_LeftClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.37f, 0.55f, ratio);
        }

        private float GetEyeRightRatio_LeftClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.35f, 0.20f, ratio);
        }

        private float GetEyeUpRatio_LeftClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.30f, 0.35f, ratio);
        }

        // 오른쪽 눈을 감았을 때
        private float GetEyeLeftRatio_RightClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.27f, 0.60f, ratio);
        }

        private float GetEyeRightRatio_RightClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.25f, 0.20f, ratio);
        }

        private float GetEyeUpRatio_RightClosed(float num1, float num2)
        {
            float ratio = (num1 / num2);
            return Mathf.InverseLerp(0.38f, 0.33f, ratio);
        }

        // 눈동자 움직이는 Blendshape 값 조절
        private void UpdateEyeballAnimation()
        {
            _iris_Left = Mathf.Lerp(_iris_Left, _eye_Left_Ratio * _weight, _irisLeapT);
            _iris_Right = Mathf.Lerp(_iris_Right, _eye_Right_Ratio * _weight, _irisLeapT);
            _iris_Up = Mathf.Lerp(_iris_Up, _eye_Up_Ratio * _weight, _irisLeapT);

            _iris_Left_LeftClosed = Mathf.Lerp(_iris_Left_LeftClosed, _eye_Left_Ratio_LeftClosed * _closedWeight, _irisLeapT);
            _iris_Right_LeftClosed = Mathf.Lerp(_iris_Right_LeftClosed, _eye_Right_Ratio_LeftClosed * _closedWeight, _irisLeapT);
            _iris_Up_LeftClosed = Mathf.Lerp(_iris_Up_LeftClosed, _eye_Up_Ratio_LeftClosed * _weight, _irisLeapT);

            _iris_Left_RightClosed = Mathf.Lerp(_iris_Left_RightClosed, _eye_Left_Ratio_RightClosed * _closedWeight, _irisLeapT);
            _iris_Right_RightClosed = Mathf.Lerp(_iris_Right_RightClosed, _eye_Right_Ratio_RightClosed * _closedWeight, _irisLeapT);
            _iris_Up_RightClosed = Mathf.Lerp(_iris_Up_RightClosed, _eye_Up_Ratio_RightClosed * _weight, _irisLeapT);
        }

        // 눈동자 움직이는 Blendshape 실행
        private void LateUpdate()
        {

            UpdateEyeballAnimation();

            /// Kalman Filter 생성
            Mat observation = new Mat();
            observation = Mat.zeros(9, 1, CvType.CV_32FC1);
            observation.put(0, 0, _iris_Left);
            observation.put(1, 0, _iris_Right);
            observation.put(2, 0, _iris_Up);
            observation.put(3, 0, _iris_Left_LeftClosed);
            observation.put(4, 0, _iris_Right_LeftClosed);
            observation.put(5, 0, _iris_Up_LeftClosed);
            observation.put(6, 0, _iris_Left_RightClosed);
            observation.put(7, 0, _iris_Right_RightClosed);
            observation.put(8, 0, _iris_Up_RightClosed);

            for (int j = 0; j < 1; j++)
            {
                Filter_array[j].correct(observation);
                X_array[j] = Filter_array[j].predict();
            }
            double[] Iris_Left_Weight = X_array[0].get(0, 0);
            double[] Iris_Right_Weight = X_array[0].get(1, 0);
            double[] Iris_Up_Weight = X_array[0].get(2, 0);
            double[] Iris_Left_Weight_LeftClosed = X_array[0].get(3, 0);
            double[] Iris_Right_Weight_LeftClosed = X_array[0].get(4, 0);
            double[] Iris_Up_Weight_LeftClosed = X_array[0].get(5, 0);
            double[] Iris_Left_Weight_RightClosed = X_array[0].get(6, 0);
            double[] Iris_Right_Weight_RightClosed = X_array[0].get(7, 0);
            double[] Iris_Up_Weight_RightClosed = X_array[0].get(8, 0);


            if (_recordManager != null && _recordManager.IsPlaying) return;



            if (Head != null && Head.sharedMesh.blendShapeCount > 0)
            {
                // 경우 나누기
                if (_distanceOfEyeHeight_LeftClosed >= _eyeBlinkWeight && _distanceOfEyeHeight_RightClosed >= _eyeBlinkWeight)  // 만약 두 눈이 떠 있다면
                {
                    Head.SetBlendShapeWeight(15, (float)Iris_Up_Weight[0]);  // Eyeball_Up
                    Head.SetBlendShapeWeight(16, (float)Iris_Left_Weight[0]);  // Eyeball_Right
                    Head.SetBlendShapeWeight(17, (float)Iris_Right_Weight[0]);  // Eyeball_Left

                    if ((float)Iris_Right_Weight[0] > 10)  // (내 기준) 눈동자가 왼쪽으로 돌아갈 때
                    {
                        float Iris_Up_new_Weight = Mathf.Clamp((float)Iris_Up_Weight[0], 0, 100f - (float)Iris_Right_Weight[0]);  // 위쪽 Blendshape은 반비례한 값까지만 커질 수 있음
                        Head.SetBlendShapeWeight(15, Iris_Up_new_Weight);
                    }
                    else if ((float)Iris_Left_Weight[0] > 10)  // (내 기준) 눈동자가 오른쪽으로 돌아갈 때
                    {
                        float Iris_Up_new_Weight = Mathf.Clamp((float)Iris_Up_Weight[0], 0, 100f - (float)Iris_Left_Weight[0]);  // 위쪽 Blendshape은 반비례한 값까지만 커질 수 있음
                        Head.SetBlendShapeWeight(15, Iris_Up_new_Weight);
                    }
                }

                else if (_distanceOfEyeHeight_LeftClosed < _eyeBlinkWeight && _distanceOfEyeHeight_RightClosed >= _eyeBlinkWeight)  // 만약 왼쪽 눈을 감으면 오른쪽 눈만 tracking 하기
                {
                    Head.SetBlendShapeWeight(16, (float)Iris_Left_Weight_LeftClosed[0]);  // Eyeball_Right
                    Head.SetBlendShapeWeight(17, (float)Iris_Right_Weight_LeftClosed[0]);  // Eyeball_Left
                }

                else if (_distanceOfEyeHeight_LeftClosed >= _eyeBlinkWeight && _distanceOfEyeHeight_RightClosed < _eyeBlinkWeight)  // 만약 오른쪽 눈을 감으면 왼쪽 눈만 tracking 하기
                {
                    Head.SetBlendShapeWeight(16, (float)Iris_Left_Weight_RightClosed[0]);  // Eyeball_Right
                    Head.SetBlendShapeWeight(17, (float)Iris_Right_Weight_RightClosed[0]);  // Eyeball_Left
                }

                else  // 만약 두 눈을 감으면 눈동자는 제자리로 돌아오기
                {
                    Head.SetBlendShapeWeight(16, Mathf.Lerp(Head.GetBlendShapeWeight(14), 0, 0.5f));
                    Head.SetBlendShapeWeight(17, Mathf.Lerp(Head.GetBlendShapeWeight(15), 0, 0.5f));
                }
            }
        }
    }
}
