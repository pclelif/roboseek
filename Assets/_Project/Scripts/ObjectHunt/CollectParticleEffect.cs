using System.Collections;
using UnityEngine;

namespace Robot.ObjectHunt
{
    /// <summary>
    /// Spawns vibrant star-shaped sparkle feedback when objects are collected or introduced.
    /// Uses 4-point star Mesh geometry + Star Texture, exactly 7 particles burst, small scale, and gold/yellow color palette.
    /// </summary>
    public sealed class CollectParticleEffect : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private ParticleSystem customParticlePrefab;
        // Warm palette gold-yellow
        [SerializeField] private Color primaryColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color secondaryColor = new Color(1f, 0.95f, 0.35f, 1f);
        [SerializeField] private float duration = 1.0f;

        private static Mesh starMesh;
        private static Texture2D starTexture;
        private static Material starMaterial;

        /// <summary>
        /// Generates a physical 4-pointed star 2D Mesh geometry so particles can NEVER render as square quads.
        /// </summary>
        private static Mesh GetOrCreateStarMesh()
        {
            if (starMesh != null) return starMesh;

            starMesh = new Mesh();
            starMesh.name = "ProceduralStarParticleMesh";

            // 4-point star polygon (9 vertices: center + 4 outer tips + 4 inner corners)
            float R = 0.5f;   // Outer tip radius
            float r = 0.14f;  // Inner corner radius

            Vector3[] vertices = new Vector3[9];
            Vector2[] uvs = new Vector2[9];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 4; i++)
            {
                // Outer tip at 0, 90, 180, 270 degrees
                float outerAngle = i * 90f * Mathf.Deg2Rad;
                Vector3 outerPos = new Vector3(Mathf.Cos(outerAngle) * R, Mathf.Sin(outerAngle) * R, 0f);
                vertices[1 + i * 2] = outerPos;
                uvs[1 + i * 2] = new Vector2(0.5f + Mathf.Cos(outerAngle) * 0.5f, 0.5f + Mathf.Sin(outerAngle) * 0.5f);

                // Inner corner at 45, 135, 225, 315 degrees
                float innerAngle = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 innerPos = new Vector3(Mathf.Cos(innerAngle) * r, Mathf.Sin(innerAngle) * r, 0f);
                vertices[2 + i * 2] = innerPos;
                uvs[2 + i * 2] = new Vector2(0.5f + Mathf.Cos(innerAngle) * 0.2f, 0.5f + Mathf.Sin(innerAngle) * 0.2f);
            }

            int[] triangles = new int[8 * 3]; // 8 triangles around center
            for (int i = 0; i < 8; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = 1 + i;
                triangles[i * 3 + 2] = 1 + ((i + 1) % 8);
            }

            starMesh.vertices = vertices;
            starMesh.uv = uvs;
            starMesh.triangles = triangles;
            starMesh.RecalculateNormals();
            starMesh.RecalculateBounds();

            return starMesh;
        }

        private static Texture2D GetOrCreateStarTexture()
        {
            if (starTexture != null) return starTexture;
            int size = 64;
            starTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            starTexture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size * 0.44f;
            float innerRadius = size * 0.12f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float dist = p.magnitude;

                    // 4-Point Star formula with smooth inner-outer pinch
                    float starRadius = innerRadius + (outerRadius - innerRadius) * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2f)), 3.5f);
                    float alpha = Mathf.Clamp01((starRadius - dist) / 1.2f);
                    starTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            starTexture.Apply();
            return starTexture;
        }

        private static Material GetOrCreateStarMaterial()
        {
            if (starMaterial != null && starMaterial.shader != null) return starMaterial;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            starMaterial = new Material(shader);
            Texture2D tex = GetOrCreateStarTexture();
            
            if (starMaterial.HasProperty("_MainTex")) starMaterial.SetTexture("_MainTex", tex);
            if (starMaterial.HasProperty("_BaseMap")) starMaterial.SetTexture("_BaseMap", tex);
            
            if (starMaterial.HasProperty("_Surface")) starMaterial.SetFloat("_Surface", 1f); // Transparent in URP
            if (starMaterial.HasProperty("_Blend")) starMaterial.SetFloat("_Blend", 0f); // Alpha blend
            if (starMaterial.HasProperty("_SrcBlend")) starMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (starMaterial.HasProperty("_DstBlend")) starMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (starMaterial.HasProperty("_ZWrite")) starMaterial.SetInt("_ZWrite", 0);

            return starMaterial;
        }

        /// <summary>
        /// Spawns a collection particle burst at the given position.
        /// </summary>
        public static CollectParticleEffect Spawn(Vector3 position, Color? colorOverride = null)
        {
            GameObject container = new GameObject("CollectParticleEffect");
            container.transform.position = position;
            var effect = container.AddComponent<CollectParticleEffect>();
            if (colorOverride.HasValue)
            {
                effect.primaryColor = colorOverride.Value;
            }
            effect.Play();
            return effect;
        }

        public void Play()
        {
            if (customParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(customParticlePrefab, transform.position, Quaternion.identity, transform);
                ps.Play();
                Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                StartCoroutine(GenerateAndPlayDynamicParticles());
            }
        }

        private IEnumerator GenerateAndPlayDynamicParticles()
        {
            ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer psRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
            
            // Assign physical star mesh and sprite material
            psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            psRenderer.mesh = GetOrCreateStarMesh();
            psRenderer.alignment = ParticleSystemRenderSpace.View; // Camera facing
            psRenderer.material = GetOrCreateStarMaterial();

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f); // Gentle speed
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f); // Star mesh size
            main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, secondaryColor);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            // Exactly 7 star particles
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 7)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f; // Small radius density

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(primaryColor, 0f), new GradientColorKey(secondaryColor, 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Play();

            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }

        /// <summary>
        /// Spawns a subtle intro sparkle glow at target location during round setup.
        /// </summary>
        public static void SpawnSparkleIntro(Vector3 position)
        {
            GameObject container = new GameObject("SparkleIntroEffect");
            container.transform.position = position;
            var effect = container.AddComponent<CollectParticleEffect>();
            effect.primaryColor = new Color(1f, 0.88f, 0.2f, 0.9f);
            effect.secondaryColor = new Color(1f, 0.96f, 0.4f, 0.9f);
            effect.duration = 1.0f;
            effect.Play();
        }
    }
}
