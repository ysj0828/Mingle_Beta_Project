using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundInfo : MonoBehaviour
{
    [SerializeField] Color color;

    private void Start() {
        SetCameraColor(Camera.main);
    }

    public void SetCameraColor(Camera cam)
    {
        cam.backgroundColor = color;
    }
}
