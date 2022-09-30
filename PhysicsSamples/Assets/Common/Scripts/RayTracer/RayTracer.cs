using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;

namespace Unity.Physics.Extensions
{
    public class RayTracer : MonoBehaviour
    {
        GameObject imageDisplay;
        UnityEngine.Material blasterMaterial;
        Texture2D blasterTexture;

        public bool AlternateKeys = false;
        public bool CastSphere = false;
        public bool Shadows = false;
        public float ImagePlane = 10.0f;
        public float RayLength = 100.0f;
        public float AmbientLight = 0.2f;
        public GameObject DisplayTarget;

        int imageRes = 100;
        float planeHalfExtents = 5.0f; /// Half extents of the created primitive plane

        const int kInvalidRequestId = -1;
        int requestId = kInvalidRequestId;

        private void OnDisable()
        {
            RayTracerSystem rbs;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && null != (rbs = world.GetExistingSystemManaged<RayTracerSystem>()))
            {
                rbs.DisposeRequest(requestId);
                requestId = kInvalidRequestId;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Creates a y-up plane
            imageDisplay = GameObject.CreatePrimitive(PrimitiveType.Plane);

            if (DisplayTarget != null)
            {
                imageDisplay.transform.parent = DisplayTarget.transform;
            }
            else
            {
                imageDisplay.transform.parent = gameObject.transform;
            }

            imageDisplay.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            blasterTexture = new Texture2D(imageRes, imageRes);
            blasterTexture.filterMode = FilterMode.Point;

            blasterMaterial = new UnityEngine.Material(imageDisplay.GetComponent<MeshRenderer>().materials[0]);
            blasterMaterial.shader = Shader.Find("Unlit/Texture");
            blasterMaterial.SetTexture("_MainTex", blasterTexture);
            imageDisplay.GetComponent<MeshRenderer>().materials = new[] { blasterMaterial };

            // Orient our plane so we cast along +Z:
            imageDisplay.transform.localRotation = Quaternion.AngleAxis(-90.0f, new Vector3(1, 0, 0));
            imageDisplay.transform.localPosition = Vector3.zero;
            imageDisplay.transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;

            RayTracerSystem rbs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RayTracerSystem>();
            if (rbs == null || !rbs.IsEnabled) return;

            if (requestId != kInvalidRequestId && rbs.TryGetResults(requestId, out var reader))
            {
                for (int i = 0; i < reader.ForEachCount; i++)
                {
                    reader.BeginForEachIndex(i);
                    while (reader.RemainingItemCount > 0)
                    {
                        int x = reader.Read<int>();
                        int y = reader.Read<int>();
                        Color c = reader.Read<Color>();
                        blasterTexture.SetPixel(x, y, c);
                    }
                    reader.EndForEachIndex();
                }

                blasterTexture.Apply();

                if (rbs.DisposeRequest(requestId))
                {
                    requestId = kInvalidRequestId;
                }
            }
            if (requestId == kInvalidRequestId)
            {
                Vector3 imageCenter = transform.TransformPoint(new Vector3(0, 0, -ImagePlane));

                Vector3 lightDir = new Vector3(0, 0, -1);
                GameObject sceneLight = GameObject.Find("Directional Light");
                if (sceneLight != null)
                {
                    lightDir = sceneLight.transform.rotation * lightDir;
                }

                Vector3 up = transform.rotation * new Vector3(0, 1, 0);
                Vector3 right = transform.rotation * new Vector3(1, 0, 0);

                requestId = rbs.AddRequest(new RayTracerSystem.RayRequest
                {
                    PinHole = transform.position,
                    ImageCenter = imageCenter,
                    Up = up,
                    Right = right,
                    LightDir = lightDir,
                    RayLength = RayLength,
                    PlaneHalfExtents = planeHalfExtents,
                    AmbientLight = AmbientLight,
                    ImageResolution = imageRes,
                    AlternateKeys = AlternateKeys,
                    CastSphere = CastSphere,
                    Shadows = Shadows,
                    CollisionFilter = CollisionFilter.Default
                });
            }
        }
    }
}
