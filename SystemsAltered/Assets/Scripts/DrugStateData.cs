using UnityEngine;

public enum DrugState
{
    Sober,
    Cocaine,
    Steroids,
    THC,
    Meth,
    Crash
}

[CreateAssetMenu(fileName = "DrugState_", menuName = "DrugRush/Drug State")]
public class DrugStateData : ScriptableObject
{
    [Header("Info")]
    public DrugState stateType;
    public float duration = 10f;
    public bool hasCrash = true;
    public DrugStateData crashState;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 5f;
    public float gravityScale = 1f;

    [Header("Look")]
    public float lookSensitivity = 1f;
    public float fov = 60f;

    [Header("Rendering (URP Renderer Swap)")]
    [Tooltip("Index in the URP Renderer List")]
    public int rendererIndex = 0;

    [Header("Hallucination")]
    public bool spawnFakeEnemies;
    public float fakeEnemyChance;
    
    [Header("Combat")]
    public float fireRate = 5f;
    public float recoil = 1f;
    public float spread = 0.01f;
}