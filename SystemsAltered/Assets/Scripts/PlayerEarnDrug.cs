using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerEarnDrug : MonoBehaviour
{
    public TextMeshProUGUI drugEarnedText;

    public HUDController hudController;
    public InputActionReference inputActionAsset;
    public float maxJuice;
    public float currentJuice;
    public DrugStateData newDrug;
    public DrugStateData[] drugArray;
    public DrugStateController drugStateController;
    private void OnEnable()
    {
        EnemyAI.EnemyKilled += IncreaseJuice;
        inputActionAsset.action.Enable();
    }

    private void OnDisable()
    {
        EnemyAI.EnemyKilled -= IncreaseJuice;
        inputActionAsset.action.Disable();
    }

    private void Update()
    {
        if (inputActionAsset.action.triggered)
        {
            UseDrug();
            Debug.Log("HELPME");
        }
    }

    private void IncreaseJuice()
    {
        if (currentJuice >= maxJuice)
        {
            return;
        }
        
        currentJuice += 1;
        
        if (currentJuice >= maxJuice)
        {
            EarnNewDrug();
            currentJuice = maxJuice;
        }
        
        hudController.UpdateDrugEarnBar(currentJuice, maxJuice);
    }

    private void EarnNewDrug()
    {
        
        int value = Random.Range(0, 4);
        newDrug = drugArray[value];
        canUseDrug = true;
        hudController.nextDrugText.text = "Use Z To Use " +newDrug.stateType.ToString() ;

    }

    private bool canUseDrug;
    private void UseDrug()
    {
        if (canUseDrug)
        {
            drugStateController.SetState(newDrug);
            canUseDrug = false;
            hudController.nextDrugText.text = "";
            currentJuice = 0;
            hudController.UpdateDrugEarnBar(currentJuice, maxJuice);
        }
    }
        
    
}
