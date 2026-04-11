using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DrugStateDatabase", menuName = "DrugRush/State Database")]
public class DrugStateDatabase : ScriptableObject
{
    public List<DrugStateData> states;

    private Dictionary<DrugState, DrugStateData> lookup;

    public void Init()
    {
        lookup = new Dictionary<DrugState, DrugStateData>();

        foreach (var s in states)
        {
            if (!lookup.ContainsKey(s.stateType))
                lookup.Add(s.stateType, s);
        }
    }

    public DrugStateData Get(DrugState state)
    {
        if (lookup == null) Init();
        return lookup[state];
    }
}