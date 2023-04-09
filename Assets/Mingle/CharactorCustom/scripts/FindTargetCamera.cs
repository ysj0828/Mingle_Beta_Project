
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mingle;

public class FindTargetCamera : MonoBehaviour
{
    public CustomCameraManager CameraManager = null;

    private void Awake()
    {
        CameraManager.targetFind(gameObject);
    }
}


