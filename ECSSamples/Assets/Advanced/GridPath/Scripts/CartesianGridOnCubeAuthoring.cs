using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid on Cube")]
[ConverterVersion("macton", 4)]
public class CartesianGridOnCubeAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [Range(2, 512)]
    public int RowCount;
    public GameObject[] FloorPrefab;
    public GameObject WallPrefab;

    // Specific wall probability, given PotentialWallProbability
    public float WallSProbability = 0.5f;
    public float WallWProbability = 0.5f;
    
    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(WallPrefab);
        referencedPrefabs.AddRange(FloorPrefab);
    }
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var floorPrefabCount = FloorPrefab.Length;
        if (floorPrefabCount == 0)
            return;

        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var blob = ref blobBuilder.ConstructRoot<CartesianGridOnCubeGeneratorBlob>();
        blob.RowCount = RowCount;
        blob.WallSProbability = WallSProbability;
        blob.WallWProbability = WallWProbability;
        blob.WallPrefab = conversionSystem.GetPrimaryEntity(WallPrefab);
        var floorPrefab = blobBuilder.Allocate(ref blob.FloorPrefab, FloorPrefab.Length);

        for (int i = 0; i < FloorPrefab.Length; i++)
        {
            floorPrefab[i] = conversionSystem.GetPrimaryEntity(FloorPrefab[i]);
        }
        
        dstManager.AddComponentData(entity, new CartesianGridOnCubeGenerator
        {
            Blob = blobBuilder.CreateBlobAssetReference<CartesianGridOnCubeGeneratorBlob>(Allocator.Persistent)
        });
        
        blobBuilder.Dispose();
    }
}
