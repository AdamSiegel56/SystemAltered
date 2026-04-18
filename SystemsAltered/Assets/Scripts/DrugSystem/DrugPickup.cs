using System;
using UnityEngine;

public class DrugPickup : MonoBehaviour
{
    public DrugStateData state;

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponent<DrugStateController>();

        if (controller != null)
        {
            controller.SetState(state);
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        gameObject.transform.Rotate(3,3,3);
    }
}