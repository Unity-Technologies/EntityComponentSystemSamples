using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformMatrix))]
public class StarInputSystem : ComponentSystem
{
	private float d;
	private bool dragging;
	private GameObject star;
	
	private float3 pressedMouse, pressedStar;
	
	protected override void OnUpdate()
	{
		if (Input.GetMouseButton(0))
		{
			MoveStar();
		}
		else
		{
			dragging = false;
		}
	}

	private void MoveStar()
	{
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			d = hit.distance;
			star = hit.collider.gameObject;
		}
		else
		{
			if (!dragging)
			{
				star = null;
				return;
			}
		}
		
		if (!dragging)
		{
			pressedStar = star.transform.position;
			pressedMouse = ray.GetPoint(d);
			dragging = true;
		}

		star.transform.position = Vector3.Lerp(star.transform.position, pressedStar - (pressedMouse - new float3(ray.GetPoint(d))), Time.deltaTime * 15);
	}
	
	
}
