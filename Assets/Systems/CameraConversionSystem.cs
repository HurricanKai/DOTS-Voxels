
using UnityEngine;
using UnityEngine.Rendering;

public class CameraConversionSystem : GameObjectConversionSystem
{
    private void Convert(Camera cam)
    {
        var entity = GetPrimaryEntity(cam);
        
        AddHybridComponent(cam);

        DstEntityManager.AddSharedComponentData(entity, new HybridCamera
        {
            Camera = cam
        });
    }
    
    protected override void OnUpdate()
    {
        Entities.ForEach((Camera cam) => Convert(cam));
    }
}