using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
        int ImageRes = 100;
        float planeHalfExtents = 5.0f; /// Half extents of the created primitive plane

        RayTracerSystem.RayResult lastResults;
        bool ExpectingResults;

        private void OnDisable()
        {
            if (ExpectingResults)
            {
                lastResults.PixelData.Dispose();
                ExpectingResults = false;
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

            // For 2019.1: // blasterTexture = new Texture2D(ImageRes, ImageRes, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_UInt , UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            blasterTexture = new Texture2D(ImageRes, ImageRes);
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

        // Update is called once per frame
        private void Update()
        {
            Vector3 imageCenter = transform.TransformPoint(new Vector3(0, 0, -ImagePlane));

            if (ExpectingResults)
            {
                BlockStream.Reader reader = lastResults.PixelData;
                for (int i = 0; i < lastResults.PixelData.ForEachCount; i++)
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
                lastResults.PixelData.Dispose();
                ExpectingResults = false;
            }

            if (Entities.World.Active == null)
            {
                return;
            }

            RayTracerSystem rbs = Entities.World.Active.GetExistingSystem<RayTracerSystem>();
            if (rbs == null || !rbs.IsEnabled)
            {
                return;
            }

            Vector3 lightDir = new Vector3(0, 0, -1);
            GameObject sceneLight = GameObject.Find("Directional Light");
            if (sceneLight != null)
            {
                lightDir = sceneLight.transform.rotation * lightDir;
            }

            Vector3 up = transform.rotation * new Vector3(0, 1, 0);
            Vector3 right = transform.rotation * new Vector3(1, 0, 0);

            lastResults = rbs.AddRequest(new RayTracerSystem.RayRequest
            {
                PinHole = transform.position,
                ImageCenter = imageCenter,
                Up = up,
                Right = right,
                LightDir = lightDir,
                RayLength = RayLength,
                PlaneHalfExtents = planeHalfExtents,
                AmbientLight = AmbientLight,
                ImageResolution = ImageRes,
                AlternateKeys = AlternateKeys,
                CastSphere = CastSphere,
                Shadows = Shadows,
                CollisionFilter = CollisionFilter.Default
            });
            ExpectingResults = true;
        }
    }
}
