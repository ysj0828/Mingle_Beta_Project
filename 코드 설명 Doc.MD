Mingle_Beta_Project/Assets/Mingle/Scripts/Camera/CameraManager.cs

# Cinemachine 카메라에 정의된 이벤트

public event Action<bool, int, Transform> TappedAvatar = delegate { }; //아바타 탭 이벤트 \
public event Action<Vector3> TappedGround = delegate { }; //Ground 탭 이벤트 \
public event Action<Vector3, Quaternion, Vector3, string, int> TappedObject = delegate { }; // 상호작용 가능하 오브젝트 탭 이벤트 \
public event Action TappedNull = delegate { }; // 허공 탭 이벤트

