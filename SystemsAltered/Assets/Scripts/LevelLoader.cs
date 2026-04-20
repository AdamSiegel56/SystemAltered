using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    private bool hasDied;
    public InputActionReference inputActionAsset;

    public HUDController hudController;
    
    
    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied += ShowEndScreen;
        inputActionAsset.action.Enable();
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDied -= ShowEndScreen;
        inputActionAsset.action.Disable();
    }

    public void Start()
    {
        
    }

    private void Update()
    {
        if (inputActionAsset.action.triggered && hasDied)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ShowEndScreen()
    {
        
        transition.SetTrigger("EndLevel");
        hasDied = true;
    }
}
