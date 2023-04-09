using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using Mediapipe;
using Mediapipe.Unity;
using UnityEngine.Android;
using Mingle;
//using UnityEngine.iOS;  // iOS 빌드 시 주석 제거해주기


namespace Mediapipe.Unity.Tutorial
{
    public class newRotation : MonoBehaviour
    {
        // 일직선 상의 랜드마크
        NormalizedLandmark p1;  // 왼쪽 귀 랜드마크
        NormalizedLandmark p2;  // 오른쪽 귀 랜드마크
        NormalizedLandmark p3;  // 코 랜드마크

        private Vector3 vhelp;
        private Vector3 vx_d;
        private Vector3 vy_d;

        private Vector3 vx;
        private Vector3 vy;
        private Vector3 vz;

        public Transform head;
        public Transform lookAtTarget;

        protected Vector3 headEulerAngles;
        protected Vector3 oldHeadEulerAngle;
        protected Vector3 relativePos;

        public List<KalmanFilter> Filter_array = new List<KalmanFilter>();
        public List<Mat> X_array = new List<Mat>();
        private Mat _tmpArray = new Mat();
        private Quaternion _tmp;

        private FacialRecordManager _recordManager = null;

        private void Awake()
        {
            FacialRecordManager[] recordManagers = FindObjectsOfType<FacialRecordManager>();
            if (recordManagers.Length > 0) _recordManager = FindObjectsOfType<FacialRecordManager>()[0];
        }


        private void Start()
        {

            if (head != null)
            {
                oldHeadEulerAngle = head.localEulerAngles;  // head의 오리지널 각도

                if (lookAtTarget != null)
                {
                    relativePos = lookAtTarget.position - head.position;  // head부터 target까지 기준 벡터
                }
                else
                {
                    Debug.LogError("lookAtTarget");
                }
            }
            else
            {
                Debug.LogError("There is no head");
            }


            KalmanFilter kal = new KalmanFilter(3, 3, 0, CvType.CV_32FC1);  // 지속적으로 변화하는 시스템에 이상적인 노이즈 제거 필터
            Mat measurement = new Mat(3, 3, CvType.CV_32FC1);
            Core.setIdentity(measurement);
            Mat transition = new Mat(3, 3, CvType.CV_32FC1);
            Core.setIdentity(transition);
            Mat processnoise = new Mat(3, 3, CvType.CV_32FC1);
            Core.setIdentity(processnoise);

#if UNITY_EDIOR || UNITY_IOS
            processnoise *= 0.03;  // 노이즈 제거하는 정도, 숫자 작을수록 노이즈 많이 제거함 (최대 1)
#elif UNITY_ANDROID && !UNITY_EDITOR
            processnoise *= 0.1;
#endif
            kal.set_measurementMatrix(measurement);
            kal.set_transitionMatrix(transition);
            kal.set_processNoiseCov(processnoise);

            _tmpArray = Mat.zeros(3, 1, CvType.CV_32FC1);
            Filter_array.Add(kal);
            X_array.Add(_tmpArray);
        }

        private void FixedUpdate()
        {
            // 기준이 되는 일직선 상의 랜드마크
            p1 = AllVariables.instance.LeftEar;
            p2 = AllVariables.instance.RightEar;
            p3 = AllVariables.instance.Nose;

            if (p1 == null || p2 == null || p3 == null)  // Before Mediapipe Loading
            {
                return;
            }
            else
            {
                vhelp = new Vector3(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);  // 왼쪽 귀에서 코까지 방향
                vx_d = new Vector3(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);  // 오른쪽 귀에서 왼쪽 귀까지 방향
                vy_d = Vector3.Cross(vhelp, vx_d);

                // 벡터 정규화
                vx = Vector3.Normalize(vx_d);
                vy = Vector3.Normalize(vy_d);
                vz = Vector3.Normalize(Vector3.Cross(vx, vy));  // 시선 벡터(얼굴의 법선벡터)

                headEulerAngles.x = -vy[0];
                headEulerAngles.y = -vz[0];
                headEulerAngles.z = -vx[0];
            }
        }

        private void LateUpdate()
        {
            if (_recordManager != null && _recordManager.IsPlaying) return;
            oldHeadEulerAngle = Vector3.Lerp(relativePos, headEulerAngles, 0.6f);  // 기준 벡터에서 시선 옮기기
            relativePos = oldHeadEulerAngle;

            if (relativePos == Vector3.zero)  // 예외처리
                return;
            else
            {
                Quaternion rotateAngle = Quaternion.LookRotation(relativePos);  // 시선 벡터 바라보기

                _tmp = Quaternion.Lerp(Quaternion.Euler(relativePos.x, relativePos.y, relativePos.z),
                                                 Quaternion.Euler(rotateAngle.x * 100, rotateAngle.y * 100, -vy[2] * 80),
                                                 0.2f);

                /// Kalman Filter 생성
                Mat observation = new Mat();
                observation = Mat.zeros(3, 1, CvType.CV_32FC1);
                observation.put(0, 0, _tmp.x);
                observation.put(1, 0, _tmp.y);
                observation.put(2, 0, _tmp.z);

                for (int j = 0; j < 1; j++)
                {
                    Filter_array[j].correct(observation);
                    X_array[j] = Filter_array[j].predict();
                }
                double[] array_x = X_array[0].get(0, 0);
                double[] array_y = X_array[0].get(1, 0);
                double[] array_z = X_array[0].get(2, 0);

                // Kalman Filter 예측 결과 적용 & Clamp 함수로 얼굴 회전 각도 limit 주기
                head.localRotation = Quaternion.Euler(Mathf.Clamp((float)array_x[0] * 500, -20f, 20f),
                                                    Mathf.Clamp((float)array_y[0] * 500, -20f, 20f),
                                                    Mathf.Clamp((float)array_z[0] * 500, -20f, 20f));
            }
        }
    }
}