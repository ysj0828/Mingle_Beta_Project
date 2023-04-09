using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeEyesMaterial : MonoBehaviour
{
    [SerializeField] Material[] myMaterial;
    Renderer myRenderer;

    public int index;

    private void Start()
    {
        myRenderer = GetComponent<Renderer>();
        myRenderer.enabled = true;
        myRenderer.sharedMaterial = myMaterial[0];
    }

    void Update()
    {
        if (index == 0)
            myRenderer.sharedMaterial = myMaterial[0];
        else if (index == 1)
            myRenderer.sharedMaterial = myMaterial[1];
        else if (index == 2)
            myRenderer.sharedMaterial = myMaterial[2];
        else if (index == 3)
            myRenderer.sharedMaterial = myMaterial[3];
    }
}
