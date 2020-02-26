using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

[UpdateInGroup(typeof(InputSystemGroup))]
public class UpdateInputSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle a)
    {
        InputSystem.Update();
        return a;
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class InputSystemGroup : ComponentSystemGroup
{
    public override void SortSystemUpdateList()
    {
        // Extract list of systems to sort (excluding built-in systems that are inserted at fixed points)
        var toSort = new List<ComponentSystemBase>(m_systemsToUpdate.Count);
        UpdateInputSystem ius = null;
        foreach (var s in m_systemsToUpdate)
        {
            if (s is UpdateInputSystem system) {
                ius = system;
            } else {
                toSort.Add(s);
            }
        }
        m_systemsToUpdate = toSort;
        base.SortSystemUpdateList();
        // Re-insert built-in systems to construct the final list
        var finalSystemList = new List<ComponentSystemBase>(toSort.Count);
        if (ius != null)
            finalSystemList.Add(ius);
        foreach (var s in m_systemsToUpdate)
            finalSystemList.Add(s);
        
        m_systemsToUpdate = finalSystemList;
    }
}