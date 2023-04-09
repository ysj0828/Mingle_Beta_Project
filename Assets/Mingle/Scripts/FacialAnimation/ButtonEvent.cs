using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mingle;
using UnityEngine.UI;

public class ButtonEvent : MonoBehaviour
{
    ChangeEyesMaterial _changeeyesmaterial;
    FacialRecordManager _recordManager;
    ButtonController _buttonController;

    public GameObject Face2D;
    public GameObject FaceLayer;
    public GameObject Face3D;

    public Button Face3DButton;
    public Button SurprisedButton;
    public Button AngryButton;
    public Button TouchedButton;
    public Button HappyButton;

    private void Awake()
    {
        FacialRecordManager[] recordManagers = FindObjectsOfType<FacialRecordManager>();
        if (recordManagers.Length > 0) _recordManager = FindObjectsOfType<FacialRecordManager>()[0];
    }

    private void Start()
    {
        _changeeyesmaterial = Face2D.GetComponent<ChangeEyesMaterial>();
        _buttonController = GetComponent<ButtonController>();
    }

    public void Face()
    {
        Face2D.SetActive(false);
        FaceLayer.SetActive(false);
        Face3D.SetActive(true);

        Face3DButton.interactable = false;
        SurprisedButton.interactable = true;
        AngryButton.interactable = true;
        TouchedButton.interactable = true;
        HappyButton.interactable = true;
    }

    public void Surprised()
    {
        _recordManager.EmoIndex = 0;
        _changeeyesmaterial.index = 0;

        Face2D.SetActive(true);
        FaceLayer.SetActive(true);
        Face3D.SetActive(false);

        Face3DButton.interactable = true;
        SurprisedButton.interactable = false;
        AngryButton.interactable = true;
        TouchedButton.interactable = true;
        HappyButton.interactable = true;
    }

    public void Angry()
    {
        _recordManager.EmoIndex = 1;
        _changeeyesmaterial.index = 1;

        Face2D.SetActive(true);
        FaceLayer.SetActive(true);
        Face3D.SetActive(false);

        Face3DButton.interactable = true;
        SurprisedButton.interactable = true;
        AngryButton.interactable = false;
        TouchedButton.interactable = true;
        HappyButton.interactable = true;
    }

    public void Touched()
    {
        _recordManager.EmoIndex = 2;
        _changeeyesmaterial.index = 2;

        Face2D.SetActive(true);
        FaceLayer.SetActive(true);
        Face3D.SetActive(false);

        Face3DButton.interactable = true;
        SurprisedButton.interactable = true;
        AngryButton.interactable = true;
        TouchedButton.interactable = false;
        HappyButton.interactable = true;
    }

    public void Happy()
    {
        _recordManager.EmoIndex = 3;
        _changeeyesmaterial.index = 3;

        Face2D.SetActive(true);
        FaceLayer.SetActive(true);
        Face3D.SetActive(false);

        Face3DButton.interactable = true;
        SurprisedButton.interactable = true;
        AngryButton.interactable = true;
        TouchedButton.interactable = true;
        HappyButton.interactable = false;
    }
}