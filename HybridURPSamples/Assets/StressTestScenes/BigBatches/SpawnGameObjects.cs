using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SpawnGameObjects : MonoBehaviour
{
    public GameObject Prefab;
    public int CountX;
    public int CountY;

    // Start is called before the first frame update
    void Start()
    {
        var localToWorld = transform.localToWorldMatrix;

        for (int x = 0; x < CountX; ++x)
        {
            for (int y = 0; y < CountY; ++y)
            {
                var pos = new float4(
                    x, 
                    0.0f,
                    y,
                    1);
                pos = math.mul(localToWorld, pos);
                
                var gameObj = Instantiate(Prefab, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);
                var gameObjectAnimationSystem = gameObj.GetComponent<GameObjectAnimationSystem>();
                if (gameObjectAnimationSystem)
                {
                    gameObjectAnimationSystem.spawnIndex = (y * CountX) + x;
                    gameObjectAnimationSystem.height = CountY;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
