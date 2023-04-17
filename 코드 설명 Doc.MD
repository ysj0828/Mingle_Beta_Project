# Cinemachine 카메라에 정의된 이벤트
## Mingle_Beta_Project/Assets/Mingle/Scripts/Camera/CameraManager.cs

```c#
public event Action<bool, int, Transform> TappedAvatar = delegate { }; //아바타 탭 이벤트 
public event Action<Vector3> TappedGround = delegate { }; //Ground 탭 이벤트 
public event Action<Vector3, Quaternion, Vector3, string, int> TappedObject = delegate { }; // 상호작용 오브젝트 탭 이벤트 
public event Action TappedNull = delegate { }; // 허공 탭 이벤트
```


<details>
  <summary>Update() - 탭 / 탭한 뒤 홀드(롱 프레스)</summary>
  
  ### 코드
  ```c#
if (Input.touchCount == 1 && EnableRotation)
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
								if (Time.time > _touchStartTime + 0.3f)
								{
										//바닥 롱프레스 이벤트
										_isLongPress = true;
								}
								break;

						case "PlayerAvatar":
								if (Time.time > _touchStartTime + 0.3f)
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
										}
										_isLongPress = true;
								}
								break;
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
  ```
</details>
	
	
	
<details>
  <summary>Update() - 드래그</summary>
  
  ### 코드
  ```c#
		_deltaSum += touch.deltaPosition.magnitude;

		if (_deltaSum > 100)
		{
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
  ```
	
</details>
	
	
	
	
<details>
  <summary>Update() - 2 손가락</summary>
  
  ### 코드
  ```c#
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
  ```
</details>
