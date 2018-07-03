using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class StarMoveSystem : ComponentSystem
{
	struct StarData
	{
	    public readonly int Length;
	    public GameObjectArray starsGO;
	    [ReadOnly] public ComponentDataArray<Star> stars;
	    public ComponentDataArray<Position> positions;
	}

	[Inject] private StarData starData;
    
	protected override void OnUpdate()
	{
		for (int i = 0; i < starData.Length; ++i)
		{
			var position = starData.positions[i];
			position.Value = new float3(starData.starsGO[i].transform.position);
			starData.positions[i] = position;
		}
	}
}