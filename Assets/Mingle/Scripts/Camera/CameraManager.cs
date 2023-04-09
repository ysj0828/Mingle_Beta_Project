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
    public class CameraManager : MonoBehaviour
    {
        public GameObject VirtualCamFraming = null;

        public CinemachineFramingTransposer VcamFraming = null;
        public CinemachineVirtualCamera VcamFramingVC = null;
        private CinemachinePOV vcamFramingPOV = null;

        public CinemachineVirtualCamera FirstPersonStationaryCamera = null;
        private CinemachinePOV firstPersonPOV = null;
        public CinemachineVirtualCamera FirstPersonDynamicCamera = null;
        // public CinemachineVirtualCamera FirstPerson = null;

        public Transform RefTarget
        {
            get => _refTarget;
            set
            {
                _refTarget = value;
                _actionManager = value.GetComponent<PlayerActionManager>();
            }
        }
        private Transform _refTarget;

        private PlayerActionManager _actionManager;

        [SerializeField] private Transform camPos;

        public bool IsCameraFirstPerson = false;
        [HideInInspector] public bool GoBackToFreeLook = false;

        public bool SwitchCams = true;
        public bool lockTwoTouch = false;
        private bool _enableCamMove = true;
        public bool EnableRotation = true;

        public float CamYOffset = 0;

        private float _combinedX, _combinedY;
        private float _diffTracker = 0;
        // private float _firstPersonMagnitude;
        private float _initialCamDistance;
        private float _touchTime;
        [SerializeField] private float _horizentalSensitivity = 1.0f;
        [SerializeField] private float _verticalSensitivity = 1.0f;
        [SerializeField] private float _twoTouchSensitivity = 1.0f;
        [SerializeField] private float zoomMax = 23;
        [SerializeField] private float zoomMin = 0.7f;
        [SerializeField] private float zoomInSensitivity = 1.0f;
        [SerializeField] private float zoomOutSensitivity = 1.0f;
        [SerializeField] private float _lowestMax = -0.5f;
        [SerializeField] private float _highestMax = 0.4f;
        private float _lowest = 0;
        private float _highest = 0;

        private Vector2 _firstPersonInitialOne;
        private Vector2 _firstPersonInitialTwo;
        private Vector2 _inputTwoInitialOne;
        private Vector2 _inputTwoInitialTwo;
        private Vector2 _initPos;

        private float _deltaSum;
        private float _touchStartTime;
        private bool _initBool, _endBool;

        private RaycastHit _initHit, _endHit;
        private bool _isLongPress;

        private int _avatarDoubleTapCounter, _groundDoubleTapCounter;

        public event Action<bool, int, Transform> TappedAvatar = delegate { };
        // public event Action<int, Vector3> TappedGround = delegate { };
        public event Action<Vector3> TappedGround = delegate { };
        public event Action<Vector3, Quaternion, Vector3, string, int> TappedObject = delegate { };
        public event Action TappedNull = delegate { };

        public AvatarState CurrentState;

        private Camera _mainCam;

        /// <summary>
        /// 카메라가 아바타 포커스 하도록 변수 설정
        /// </summary>
        public void SetAvatar(GameObject avatar)
        {
            if (!avatar) return;
            VcamFramingVC.LookAt = avatar.transform;
            VcamFramingVC.Follow = avatar.transform;
            FirstPersonStationaryCamera = avatar.transform.Find("FirstPersonCam").GetComponent<CinemachineVirtualCamera>();
            FirstPersonDynamicCamera = avatar.transform.Find("Root").Find("Bip001").Find("Bip001 Pelvis").Find("Bip001 Spine").Find("Bip001 Spine1").Find("Bip001 Neck").Find("Bip001 Head").Find("FirstPersonCam (1)").GetComponent<CinemachineVirtualCamera>();
            firstPersonPOV = FirstPersonStationaryCamera.GetCinemachineComponent<CinemachinePOV>();
            firstPersonPOV.m_HorizontalAxis.m_InputAxisName = "";
            firstPersonPOV.m_VerticalAxis.m_InputAxisName = "";
            FirstPersonStationaryCamera.Priority = 0;
            FirstPersonDynamicCamera.Priority = 0;
            RefTarget = avatar.transform;
            avatar.GetComponent<PlayerActionManager>().SubscriptEvents();
        }

        void Awake()
        {
            _mainCam = Camera.main;

            VcamFramingVC = VirtualCamFraming.transform.GetComponent<CinemachineVirtualCamera>();
            VcamFraming = VcamFramingVC.GetCinemachineComponent<CinemachineFramingTransposer>();
            vcamFramingPOV = VcamFramingVC.GetCinemachineComponent<CinemachinePOV>();

            // Cinemachine 자체 인풋 제거.
            vcamFramingPOV.m_HorizontalAxis.m_InputAxisName = "";
            vcamFramingPOV.m_VerticalAxis.m_InputAxisName = "";
        }

        /// <summary>
        /// 카메라 포커스 타겟 설정
        /// </summary>
        public void targetFind(GameObject obj)
        {
            // 테스트용 카메라락 해제
            //_lockAngle = true;
            RefTarget = obj.transform;
            VcamFramingVC.LookAt = obj.transform;
            VcamFramingVC.Follow = obj.transform;

            // FirstPerson = obj.GetComponent<CinemachineVirtualCamera>();
            // FirstPerson.Follow = obj.transform;
        }

        private void Update()
        {
            CameraMovement();
        }

        private void LateUpdate()
        {
            if (Time.time > _touchStartTime + 0.5f)
            {
                _avatarDoubleTapCounter = 0;
                _groundDoubleTapCounter = 0;
            }
        }

        /// <summary>
        /// 카메라 관련 터치 인터랙션
        /// </summary>
        /// <remarks>
        /// <para>터치 0 : 관련 변수 리셋</para>
        /// <para>터치 1 : 카메라 회전</para>
        /// <para>터치 2 : 카메라 위치 변경(상하좌우)</para>
        /// </remarks>
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

                if (!SwitchCams) SwitchCams = true;

                if (IsCameraFirstPerson)
                {
                    if (GoBackToFreeLook)
                    {
                        FirstPersonStationaryCamera.Priority = 0;
                        FirstPersonDynamicCamera.Priority = 0;
                        //   FirstPerson.transform.localEulerAngles = Vector3.zero;
                        GoBackToFreeLook = false;
                        IsCameraFirstPerson = false;
                    }
                }
            }

            if (Input.touchCount == 1 && SwitchCams && EnableRotation)
            {
                Touch touch = Input.touches[0];

                if (VcamFramingVC.LookAt == null)
                {
                    VcamFramingVC.LookAt = RefTarget;
                }
                if (touch.phase == TouchPhase.Began)
                {
                    _initPos = touch.position;
                    _deltaSum = 0;
                    _touchStartTime = Time.time;
                    _initBool = Physics.Raycast(_mainCam.ScreenPointToRay(_initPos), out _initHit);
                    if (isPointerOverUI(touch)) return;
                }

                _deltaSum += touch.deltaPosition.magnitude;

                if (_deltaSum < 100 && !_isLongPress && _initBool)
                {
                    switch (_initHit.transform.tag)
                    {
                        case "Walkable":
                            if (Time.time <= _touchStartTime + 0.3f)
                            {

                            }

                            else if (Time.time > _touchStartTime + 0.3f)
                            {
                                //바닥 롱프레스 이벤트
                                // var json = new JObject();
                                // json["cmd"] = "OnLongPressEmptySpace";
                                // RNMessanger.SendtoRN(JsonConvert.SerializeObject(json, Formatting.None));
                                _isLongPress = true;
                            }
                            break;

                        case "PlayerAvatar":
                            if (Time.time <= _touchStartTime + 0.3f)
                            {

                            }

                            else if (Time.time > _touchStartTime + 0.3f)
                            {
                                if (_initHit.transform.GetComponent<PhotonView>().IsMine)
                                {
                                    //first person switch
                                    changeToFirstPerson();
                                    return;
                                }

                                else if (!_initHit.transform.GetComponent<PhotonView>().IsMine)
                                {
                                    //상대방 아바타 롱프레스 이벤트
                                    // var json = new JObject();
                                    // json["cmd"] = "OnLongPressOtherAvatar";
                                    // RNMessanger.SendtoRN(JsonConvert.SerializeObject(json, Formatting.None));
                                    // return;
                                }
                                _isLongPress = true;
                            }
                            break;
                    }
                }

                if (_deltaSum > 100)
                {
                    if (SceneManager.GetActiveScene().name == "CharacterCustom")
                    {
                        // Debug.Log("CharacterCustom One");
                        if (VcamFraming.m_ScreenX != 0.5f || VcamFraming.m_ScreenY != 0.5f)
                        {
                            VcamFraming.m_ScreenX = 0.5f;
                            VcamFraming.m_ScreenY = 0.5f + CamYOffset;
                        }
                        vcamFramingPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                        vcamFramingPOV.m_VerticalAxis.Value -= touch.deltaPosition.y * 0.04f * _verticalSensitivity;
                    }
                    else if (SceneManager.GetActiveScene().name == "CharacterPreview")
                    {
                        if (VcamFraming.m_ScreenX != 0.5f || VcamFraming.m_ScreenY != 0.5f)
                        {
                            VcamFraming.m_ScreenX = 0.5f;
                            VcamFraming.m_ScreenY = 0.5f + CamYOffset;
                        }
                        vcamFramingPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                        //vcamFramingPOV.m_VerticalAxis.Value -= touch.deltaPosition.y * 0.04f * _verticalSensitivity;
                    }
                    else if (!IsCameraFirstPerson)
                    {
                        if (VcamFraming.m_ScreenX != 0.5f || VcamFraming.m_ScreenY != 0.5f)
                        {
                            VcamFraming.m_ScreenX = 0.5f;
                            VcamFraming.m_ScreenY = 0.5f + CamYOffset;
                        }
                        vcamFramingPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                        vcamFramingPOV.m_VerticalAxis.Value -= touch.deltaPosition.y * 0.04f * _verticalSensitivity;
                    }

                    else if (IsCameraFirstPerson && CurrentState == AvatarState.Idle)
                    {
                        // firstPersonPOV.m_HorizontalAxis.Value += touch.deltaPosition.x * 0.05f * _horizentalSensitivity;
                        RefTarget.eulerAngles += new Vector3(0, touch.deltaPosition.x * 0.06f * _horizentalSensitivity, 0);
                        firstPersonPOV.m_VerticalAxis.Value -= touch.deltaPosition.y * 0.06f * _verticalSensitivity;
                    }
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _isLongPress = false;
                    if (_deltaSum <= 100)
                    {
                        _endBool = Physics.Raycast(_mainCam.ScreenPointToRay(touch.position), out _endHit);
                        if (_endBool && _actionManager?.CurrentState != AvatarState.Jumping)
                        {
                            switch (_endHit.transform.tag)
                            {
                                case "PlayerAvatar":
                                    _avatarDoubleTapCounter++;
                                    TappedAvatar?.Invoke(_endHit.transform.GetComponent<PhotonView>().IsMine, _avatarDoubleTapCounter, _endHit.transform);
                                    if (_avatarDoubleTapCounter == 2) _avatarDoubleTapCounter = 0;
                                    break;

                                case "Walkable":
                                    _groundDoubleTapCounter++;
                                    TappedGround?.Invoke(_endHit.point);
                                    // TappedGround?.Invoke(_groundDoubleTapCounter, _endHit.point);
                                    if (_groundDoubleTapCounter == 2) _groundDoubleTapCounter = 0;
                                    break;

                                case "ObjectInteractable":
                                    Vector3 closestPoint = _endHit.transform.GetComponent<BoxCollider>().ClosestPoint(RefTarget.position);
                                    closestPoint = new Vector3(closestPoint.x, RefTarget.position.y, closestPoint.z);
                                    TappedObject?.Invoke(_endHit.transform.position, _endHit.transform.rotation, closestPoint, _endHit.transform.name, _endHit.transform.GetComponent<PhotonView>().ViewID);
                                    break;
                            }
                        }

                        else if (!_endBool)
                        {
                            if (isPointerOverUI(touch)) return;
                            TappedNull?.Invoke();
                        }
                    }
                }
            }

            if (RefTarget != null && !lockTwoTouch)
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

                    if (SwitchCams)
                    {
                        SwitchCams = false;
                    }

                    if (VcamFramingVC.LookAt != null)
                    {
                        VcamFramingVC.LookAt = null;
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
                            if (IsCameraFirstPerson)
                            {
                                if (_firstPersonInitialOne == new Vector2(999, 999) || _firstPersonInitialTwo == new Vector2(999, 999))
                                {
                                    _firstPersonInitialOne = touches1.position;
                                    _firstPersonInitialTwo = touches2.position;
                                }

                                // float prevFirstPersonMagnitude = (_firstPersonInitialOne - _firstPersonInitialTwo).magnitude;
                                // float currentFirstPersonMagnitude = (touches1.position - touches2.position).magnitude;

                                // _firstPersonMagnitude = currentFirstPersonMagnitude - prevFirstPersonMagnitude;

                                if (_diffTracker <= -500)
                                {
                                    GoBackToFreeLook = true;

                                    if (VcamFraming.m_CameraDistance < 3)
                                    {
                                        VcamFraming.m_CameraDistance = 5;
                                    }
                                }
                            }
                            else
                            {
                                VcamFraming.m_CameraDistance -= diff * 0.01f * zoomOutSensitivity;
                                VcamFraming.m_CameraDistance = Mathf.Clamp(VcamFraming.m_CameraDistance, zoomMin, zoomMax);

                                if (SceneManager.GetActiveScene().name == "CharacterCustom")
                                {
                                    Debug.Log("zoom out");

                                    _lowest += VcamFraming.m_CameraDistance * 0.05f;
                                    _highest -= (VcamFraming.m_CameraDistance - 1.5f) * 0.05f;

                                    _lowest = Mathf.Clamp(_lowest, _lowestMax, 0);
                                    _highest = Mathf.Clamp(_highest, 0, _highestMax);

                                    Debug.Log("m_CameraDistance" + VcamFraming.m_CameraDistance);
                                    Debug.Log("_lowest" + _lowest);
                                    Debug.Log("_highest" + _highest);
                                }
                            }
                        }
                        else if (diff > 0)
                        {
                            //zoom in
                            VcamFraming.m_CameraDistance -= diff * 0.01f * zoomInSensitivity;
                            VcamFraming.m_CameraDistance = Mathf.Clamp(VcamFraming.m_CameraDistance, zoomMin, zoomMax);

                            if (SceneManager.GetActiveScene().name == "CharacterCustom")
                            {
                                Debug.Log("zoom in");

                                _lowest -= VcamFraming.m_CameraDistance * 0.05f;
                                _highest += (VcamFraming.m_CameraDistance - 1.5f) * 0.05f;

                                _lowest = Mathf.Clamp(_lowest, _lowestMax, 0);
                                _highest = Mathf.Clamp(_highest, 0, _highestMax);

                                Debug.Log("m_CameraDistance" + VcamFraming.m_CameraDistance);
                                Debug.Log("_lowest" + _lowest);
                                Debug.Log("_highest" + _highest);
                            }

                            if (_initialCamDistance <= 0.75)
                            {
                                _touchTime += Time.deltaTime;
                                if (_touchTime >= 2)
                                {
                                    changeToFirstPerson();
                                }
                            }
                        }
                    }

                    if (dotProduct > 0 && _enableCamMove)
                    {
                        //move
                        //Two Touch Up Down Left Right
                        if (SceneManager.GetActiveScene().name == "CharacterCustom")
                        {
                            //Debug.Log("CharacterCustom Two");

                            VcamFraming.m_TrackedObjectOffset += new Vector3(0, (-_combinedY) * Time.deltaTime * _twoTouchSensitivity, 0);
                            VcamFraming.m_TrackedObjectOffset = new Vector3(0, Mathf.Clamp(VcamFraming.m_TrackedObjectOffset.y, _lowest, _highest), 0);

                        }
                        else
                        {
                            VcamFraming.m_ScreenX += _combinedX * Time.deltaTime * 0.01f * _twoTouchSensitivity;
                            VcamFraming.m_ScreenY += (-_combinedY) * Time.deltaTime * 0.01f * _twoTouchSensitivity;

                            VcamFraming.m_ScreenX = Mathf.Clamp(VcamFraming.m_ScreenX, -minScreenX, maxScreenX);
                            VcamFraming.m_ScreenY = Mathf.Clamp(VcamFraming.m_ScreenY, minScreenY, maxScreenY);
                        }
                    }
                }
            }
        }

        [SerializeField] private float minScreenX = 4.0f;
        [SerializeField] private float minScreenY = 0.42f;
        [SerializeField] private float maxScreenX = 4.0f;
        [SerializeField] private float maxScreenY = 4.0f;

        ///<summary>
        ///카메라 Axis 값 초기화
        ///</summary>
        public void recenterCameraAxis()
        {
            vcamFramingPOV.m_HorizontalAxis.Value = 150f;
            vcamFramingPOV.m_VerticalAxis.Value = 25;
        }

        bool isPointerOverUI(Touch touch)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);

            eventData.position = new Vector2(touch.position.x, touch.position.y);

            List<RaycastResult> results = new List<RaycastResult>();

            if (!EventSystem.current) return false;

            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        /// <summary>
        /// 1인칭으로 전환
        /// </summary>
        /// <remarks>
        /// <para>오브젝트 인터랙션 : 아바타 머리 움직임에 따라 움직이는 1인칭 카메라</para>
        /// <para>그 외 : 아바타 머리 움직임에 상관없이 정적인 1인칭 카메라</para>
        /// </remarks>
        public void changeToFirstPerson()
        {
            //   FirstPerson.Priority = 30;
            if (RefTarget.GetComponent<PlayerActionManager>().CurrentState == AvatarState.ObjectInteraction)
            {
                FirstPersonDynamicCamera.Priority = 20;
            }

            else
            {
                FirstPersonStationaryCamera.Priority = 20;
            }
            IsCameraFirstPerson = true;
        }

        /// <summary>
        /// 카메라 포커스 리셋
        /// </summary>
        public void recenterCameraOnMove()
        {
            if (VcamFraming.m_ScreenX != 0.5f || VcamFraming.m_ScreenY != 0.5f)
            {
                VcamFraming.m_ScreenX = 0.5f;
                VcamFraming.m_ScreenY = 0.5f;
            }
        }

        //카메라 줌 범위 초기화(FEED 기능 기준)
        public void recenterCameraDistance()
        {
            VcamFraming.m_CameraDistance = 25f;
        }
    }
}