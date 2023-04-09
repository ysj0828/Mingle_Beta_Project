using UnityEngine;
using Cinemachine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Photon.Pun;
using Unity.Burst.CompilerServices;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Mingle
{
    public class CustomCameraManager : MonoBehaviour
    {
        public GameObject VirtualCamFraming = null;

        public CinemachineFramingTransposer VcamFraming = null;
        public CinemachineVirtualCamera VcamFramingVC = null;
        private CinemachinePOV vcamFramingPOV = null;

        public Transform refTarget;

        public bool lockTwoTouch = false;
        private bool _enableCamMove = true;

        public float CamYOffset = 0;

        private float _combinedX, _combinedY;
        private float _diffTracker = 0;
        // private float _firstPersonMagnitude;
        private float _initialCamDistance;
        private float _touchTime;
        [SerializeField] private float _horizentalSensitivity = 1.0f;
        [SerializeField] private float _verticalSensitivity = 1.0f;
        [SerializeField] private float _twoTouchSensitivity = 1.0f;
        [SerializeField] private float zoomMax = 7;
        [SerializeField] private float zoomMin = 0.7f;
        [SerializeField] private float zoomInSensitivity = 1.0f;
        [SerializeField] private float zoomOutSensitivity = 1.0f;

        private Vector2 _inputTwoInitialOne;
        private Vector2 _inputTwoInitialTwo;
        private Vector2 _initPos;

        private float _deltaSum;
        private float _height = 0;
        private float _maxDistance = 2f;
        private float _currentDistance;

        private float _distance;

        [SerializeField] GameObject _cameraHeightBound ;
        // {"x":0.0,"y":1.2472962141036988,"z":-1.9696153402328492}


        public void SetAvatarAsMain()
        {
            VcamFramingVC.LookAt = refTarget;
            VcamFramingVC.Follow = refTarget;
            _enableCamMove = true;

            VcamFraming.m_DeadZoneHeight = 0;
            VcamFraming.m_DeadZoneWidth = 0;

            vcamFramingPOV.m_VerticalAxis.m_MinValue = 0;
        }

        void Awake()
        {
            VcamFramingVC = VirtualCamFraming.transform.GetComponent<CinemachineVirtualCamera>();
            VcamFraming = VcamFramingVC.GetCinemachineComponent<CinemachineFramingTransposer>();
            vcamFramingPOV = VcamFramingVC.GetCinemachineComponent<CinemachinePOV>();

            // Cinemachine 자체 인풋 제거.
            vcamFramingPOV.m_HorizontalAxis.m_InputAxisName = "";
            vcamFramingPOV.m_VerticalAxis.m_InputAxisName = "";
        }

        public void targetFind(GameObject obj)
        {
            // 테스트용 카메라락 해제
            refTarget = obj.transform;
            VcamFramingVC.LookAt = obj.transform;
            VcamFramingVC.Follow = obj.transform;
        }

        private void Update()
        {

            _distance = new Vector2(transform.position.x, transform.position.z).magnitude;

        if(SceneManager.GetActiveScene().name == "CharacterPreview"){
            if (_distance >= 1 && _distance < 2f)
            {

                _height = 1.7f - 1.1f * _distance + 0.5f;

                _cameraHeightBound.transform.localScale = new Vector3(0.1f, _height, 1f);

                _cameraHeightBound.transform.position = new Vector3(transform.position.x, 1.3167f, transform.position.z);
            }
        }
            //Debug.Log("distance : " + _distance);

            CameraMovement();

        }

        private void CameraMovement()
        {
            if (Input.touchCount == 0)
            {
                if (_diffTracker != 0) _diffTracker = 0;
                if (_touchTime != 0) _touchTime = 0;

                if (_initPos != new Vector2(999, 999) || _inputTwoInitialOne != new Vector2(999, 999) || _inputTwoInitialTwo != new Vector2(999, 999))
                {
                    _initPos = new Vector2(999, 999);

                    _inputTwoInitialOne = new Vector2(999, 999);
                    _inputTwoInitialTwo = new Vector2(999, 999);
                }
            }

            if (Input.touchCount == 1)
            {
                Touch touch = Input.touches[0];

                if (VcamFramingVC.LookAt == null)
                {
                    VcamFramingVC.LookAt = refTarget;
                }

                if (touch.phase == TouchPhase.Began)
                {
                    _initPos = touch.position;
                    _deltaSum = 0;
                }

                _deltaSum += touch.deltaPosition.magnitude;

                if (_deltaSum > 100)
                {
                        if (VcamFraming.m_ScreenX != 0.5f || VcamFraming.m_ScreenY != 0.5f)
                        {
                            VcamFraming.m_ScreenX = 0.5f;
                            VcamFraming.m_ScreenY = 0.5f + CamYOffset;
                        }
                    //원터치 상 하 
                    if (SceneManager.GetActiveScene().name == "CharacterCustom")
                    {


                        if (Mathf.Abs((touch.position.x - _initPos.x)) > Mathf.Abs((touch.position.y - _initPos.y)))
                        {
                            // vcamFramingPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                            refTarget.parent.eulerAngles -= new Vector3(0, touch.deltaPosition.x * 0.06f * _horizentalSensitivity, 0);
                        }

                        else if (Mathf.Abs((touch.position.x - _initPos.x)) <= Mathf.Abs((touch.position.y - _initPos.y)) && _distance < 1.965f)
                        {
                            VcamFraming.m_TrackedObjectOffset -= new Vector3(0, (touch.deltaPosition.y) * 0.05f * Time.deltaTime * _twoTouchSensitivity, 0);
                            VcamFraming.m_TrackedObjectOffset = new Vector3(0, Mathf.Clamp(VcamFraming.m_TrackedObjectOffset.y, -0.5f, 0.5f), 0);
                        }

                        //vcamFramingPOV.m_VerticalAxis.Value -= touch.deltaPosition.y * 0.04f * _verticalSensitivity;
                    }
                    else if (SceneManager.GetActiveScene().name == "CharacterPreview")
                    {
 
                        //vcamFramingPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                        
                        refTarget.parent.eulerAngles -= new Vector3(0, touch.deltaPosition.x * 0.06f * _horizentalSensitivity, 0);
                    }
                }
            }

            if (refTarget != null && !lockTwoTouch)
            {
                if (SceneManager.GetActiveScene().name == "CharacterPreview") return;

                if (Input.touchCount == 2)
                {

                    Touch touches1 = Input.GetTouch(0);
                    Touch touches2 = Input.GetTouch(1);

                    if (_inputTwoInitialOne == new Vector2(999, 999) || _inputTwoInitialTwo == new Vector2(999, 999))
                    {
                        _inputTwoInitialOne = touches1.position;
                        _inputTwoInitialTwo = touches2.position;
                        _initialCamDistance = VcamFraming.m_CameraDistance;
                    }

                    _combinedX = touches1.deltaPosition.x + touches2.deltaPosition.x;
                    _combinedY = touches1.deltaPosition.y + touches2.deltaPosition.y;
                    float dotProduct = Vector2.Dot(touches1.position - _inputTwoInitialOne, touches2.position - _inputTwoInitialTwo);

                    if (dotProduct < 0)
                    {
                        Vector2 touchZeroPos = touches1.position - touches1.deltaPosition;
                        Vector2 touchOnePos = touches2.position - touches2.deltaPosition;

                        float prevMagnitude = (touchZeroPos - touchOnePos).magnitude;
                        float currentMagnitude = (touches1.position - touches2.position).magnitude;
                        float diff = currentMagnitude - prevMagnitude;

                        _diffTracker += diff;

                        if (diff < 0)
                        {
                            //zoom out
                            VcamFraming.m_CameraDistance -= diff * 0.01f * zoomOutSensitivity;
                            VcamFraming.m_CameraDistance = Mathf.Clamp(VcamFraming.m_CameraDistance, zoomMin, zoomMax);

                            //초기값
                            VcamFraming.m_TrackedObjectOffset = Vector3.Lerp(VcamFraming.m_TrackedObjectOffset, Vector3.zero, 0.5f);

                        }
                        else if (diff > 0)
                        {
                            //zoom in
                            VcamFraming.m_CameraDistance -= diff * 0.01f * zoomInSensitivity;
                            VcamFraming.m_CameraDistance = Mathf.Clamp(VcamFraming.m_CameraDistance, zoomMin, zoomMax);


                        }
                    }
                }
            }
        }
    }
}