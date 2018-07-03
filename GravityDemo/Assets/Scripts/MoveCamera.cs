using UnityEngine;

public class MoveCamera : MonoBehaviour
{
	private Vector2 _mouseAbsolute;
	private Vector2 _targetDirection;
    
	void Start()
	{
		// Camera's initial orientation.
		_targetDirection = transform.localRotation.eulerAngles;
	}

	void FixedUpdate()
	{
		if (Input.GetMouseButton(1))
		{
			var targetOrientation = Quaternion.Euler(_targetDirection);

			var mouseDelta = 5.0f * new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

			// Find the absolute mouse movement value from point zero.
			_mouseAbsolute += mouseDelta;
			_mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -90, 90);

			transform.localRotation =
				Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right) * targetOrientation;

			var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
			transform.localRotation *= yRotation;

			transform.Translate(
				new Vector3(Input.GetAxis("Horizontal") * Time.deltaTime * 250, 0,
					Input.GetAxis("Vertical") * Time.deltaTime * 250), Space.Self);
		}
	}
}