using UnityEngine;

public class DrugWorldSwap : MonoBehaviour
{
    
    
    
    [SerializeField] private string lightWorld = "LightWorld";
    private int lwIndex;
    [SerializeField] private string darkWorld = "DarkWorld";
    private int dwIndex;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lwIndex = LayerMask.NameToLayer(lightWorld);
        dwIndex = LayerMask.NameToLayer(darkWorld);
    }
    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += CameraSwap;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= CameraSwap;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TransferWorlds(bool isTogglingDark)
    {
        // Find all objects in the scene (Warning: FindObjectsByType is performance-heavy)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (isTogglingDark)
        {
            foreach (GameObject go in allObjects) {
                if (go.layer == lwIndex) {
                    go.SetActive(false);
                }
            }
            foreach (GameObject go in allObjects) {
                if (go.layer == dwIndex) {
                    go.SetActive(true);
                }
            }
        }
        else
        {
            foreach (GameObject go in allObjects) {
                if (go.layer == dwIndex) {
                    go.SetActive(false);
                }
            }
            foreach (GameObject go in allObjects) {
                if (go.layer == lwIndex) {
                    go.SetActive(true);
                }
            }
        }
        
    }

    public void CameraSwap(DrugStateData newState)
    {
        switch (newState.stateType)
        {
            case (DrugState.Sober):
                Camera.main.cullingMask = ~(1 << dwIndex);
                TransferWorlds(false);

                break;
            case(DrugState.Cocaine):
                Camera.main.cullingMask = ~(1 << lwIndex);
                TransferWorlds(true);
                break;
            case(DrugState.THC):
                Camera.main.cullingMask = ~(1 << lwIndex);
                TransferWorlds(true);
                Debug.Log("TESTINGLW");
                break;
            case(DrugState.Steroids):
                Camera.main.cullingMask = ~(1 << lwIndex);
                TransferWorlds(true);

                break;
            case(DrugState.Meth):
                Camera.main.cullingMask = ~(1 << lwIndex);
                TransferWorlds(true);

                break;
            case(DrugState.Crash):
                Camera.main.cullingMask = ~(1 << dwIndex);
                TransferWorlds(false);

                break;
        }
    }
    
}
