using Unity.Entities;
using UnityEngine;

namespace Samples.Common
{
	public class RotationSpeedHybridECS : MonoBehaviour
	{
		public float speed;
	}

	public class RotatingSystemHybridECS : ComponentSystem
	{
		struct Group
		{
#pragma warning disable 649
		    public Transform 				transform;
			public RotationSpeedHybridECS   rotation;
#pragma warning restore 649
	    }

		protected override void OnUpdate()
		{
			float dt = Time.deltaTime;
			foreach(var e in GetEntities<Group>())
			{
				e.transform.rotation = e.transform.rotation * Quaternion.AngleAxis(dt * e.rotation.speed, Vector3.up);
			}
		}
	}
}

