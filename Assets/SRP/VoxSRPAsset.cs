using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class VoxSRPAsset : RenderPipelineAsset
{
    [SerializeField]
    private Material voxMaterial;

    public int chunksPerBatch;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("VoxSRP/Create")]
    static void CreateBasicAssetPipeline()
    {
        var instance = ScriptableObject.CreateInstance<VoxSRPAsset>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/VoxSRP.asset");
    }
#endif
    

    protected override RenderPipeline CreatePipeline() => new VoxSRP();

    public override Material defaultMaterial => voxMaterial;
}