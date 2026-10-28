using UnityEngine;

namespace KoiPond
{
    /// <summary>
    /// Spawns natural, expanding circular ripples and subtle bite disturbances
    /// on the pond water surface synchronized with koi feeding bites.
    /// </summary>
    public class KoiFeedingRippleEffect : MonoBehaviour
    {
        [Header("Particle Systems")]
        [Tooltip("Particle system responsible for expanding circular ripples")]
        [SerializeField] private ParticleSystem rippleParticles;

        [Tooltip("Particle system responsible for subtle initial bite disturbance / splash")]
        [SerializeField] private ParticleSystem splashParticles;

        [Header("Ripple Settings")]
        [Tooltip("Lifetime of each expanding ripple ring in seconds")]
        [SerializeField] private float rippleLifetime = 2.2f;

        [Tooltip("Starting size of the ripple ring in meters")]
        [SerializeField] private float rippleStartSize = 0.18f;

        [Tooltip("Maximum expanded size of ripple ring in meters")]
        [SerializeField] private float rippleMaxSize = 1.15f;

        [Tooltip("Maximum alpha opacity for ripple rings")]
        [Range(0.1f, 1f)]
        [SerializeField] private float rippleOpacity = 0.65f;

        [Header("Materials")]
        [SerializeField] private Material rippleMaterial;
        [SerializeField] private Material splashMaterial;

        private void Awake()
        {
            EnsureParticleSystems();
        }

        private void Start()
        {
            // Ensure particle systems are playing so Emit() works reliably
            if (rippleParticles != null && !rippleParticles.isPlaying)
            {
                rippleParticles.Play();
            }
            if (splashParticles != null && !splashParticles.isPlaying)
            {
                splashParticles.Play();
            }
        }

        private void OnValidate()
        {
            if (rippleParticles != null) ConfigureRippleParticleSystem(rippleParticles);
            if (splashParticles != null) ConfigureSplashParticleSystem(splashParticles);
        }

        /// <summary>
        /// Automatically constructs and configures the particle systems if missing.
        /// </summary>
        public void EnsureParticleSystems()
        {
            if (rippleParticles == null)
            {
                var rippleObj = transform.Find("RippleParticles");
                if (rippleObj == null)
                {
                    rippleObj = new GameObject("RippleParticles").transform;
                    rippleObj.SetParent(transform, false);
                }
                rippleParticles = rippleObj.GetComponent<ParticleSystem>();
                if (rippleParticles == null)
                {
                    rippleParticles = rippleObj.gameObject.AddComponent<ParticleSystem>();
                }
            }

            ConfigureRippleParticleSystem(rippleParticles);

            if (splashParticles == null)
            {
                var splashObj = transform.Find("SplashParticles");
                if (splashObj == null)
                {
                    splashObj = new GameObject("SplashParticles").transform;
                    splashObj.SetParent(transform, false);
                }
                splashParticles = splashObj.GetComponent<ParticleSystem>();
                if (splashParticles == null)
                {
                    splashParticles = splashObj.gameObject.AddComponent<ParticleSystem>();
                }
            }

            ConfigureSplashParticleSystem(splashParticles);
        }

        private void ConfigureRippleParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = rippleLifetime;
            main.startSpeed = 0f;
            main.startSize = rippleStartSize;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startColor = new Color(0.92f, 0.98f, 1f, rippleOpacity);

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            // Size over lifetime: expands smoothly from start size to max size
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(new Keyframe(0f, 1f, 1.6f, 1.6f));
            sizeCurve.AddKey(new Keyframe(1f, rippleMaxSize / Mathf.Max(0.01f, rippleStartSize), 0.15f, 0f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: soft fade out
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.85f, 0.95f, 0.98f), 0.5f),
                    new GradientColorKey(new Color(0.80f, 0.92f, 0.95f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(rippleOpacity, 0.12f),
                    new GradientAlphaKey(rippleOpacity * 0.75f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                rend.alignment = ParticleSystemRenderSpace.View;
                rend.sortingFudge = -50;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

#if UNITY_EDITOR
                if (rippleMaterial == null)
                {
                    rippleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KoiFeedingRipple_MAT.mat");
                }
#endif
                if (rippleMaterial != null)
                {
                    rend.sharedMaterial = rippleMaterial;
                }
            }
        }

        private void ConfigureSplashParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.38f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.6f;
            main.maxParticles = 50;
            main.startColor = new Color(0.95f, 1f, 1f, 0.75f);

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.03f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.9f, 0.98f, 1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                rend.sortingFudge = -50;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

#if UNITY_EDITOR
                if (splashMaterial == null)
                {
                    splashMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KoiFeedingSplash_MAT.mat");
                }
#endif
                if (splashMaterial != null)
                {
                    rend.sharedMaterial = splashMaterial;
                }
            }
        }

        /// <summary>
        /// Spawns a bite ripple at the exact point where the fish mouth touches the surface.
        /// Can include a subtle initial splash disturbance.
        /// </summary>
        public void SpawnBiteRipple(Vector3 mouthPos, float waterY, bool includeSplash = false)
        {
            if (rippleParticles != null)
            {
                var rend = rippleParticles.GetComponent<ParticleSystemRenderer>();
                if (rend != null && rend.sharedMaterial == null && rippleMaterial != null)
                {
                    rend.sharedMaterial = rippleMaterial;
                }

                if (!rippleParticles.isPlaying) rippleParticles.Play();

                Vector3 surfacePos = new Vector3(mouthPos.x, waterY + 0.005f, mouthPos.z);
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    position = surfacePos,
                    startLifetime = rippleLifetime,
                    startSize = rippleStartSize + Random.Range(-0.02f, 0.02f),
                    startColor = new Color(0.92f, 0.98f, 1f, rippleOpacity + Random.Range(-0.05f, 0.05f)),
                    applyShapeToPosition = false
                };
                rippleParticles.Emit(emitParams, 1);
            }

            if (includeSplash && splashParticles != null)
            {
                var rend = splashParticles.GetComponent<ParticleSystemRenderer>();
                if (rend != null && rend.sharedMaterial == null && splashMaterial != null)
                {
                    rend.sharedMaterial = splashMaterial;
                }

                if (!splashParticles.isPlaying) splashParticles.Play();

                Vector3 surfacePos = new Vector3(mouthPos.x, waterY + 0.006f, mouthPos.z);
                int dropletCount = Random.Range(4, 7);
                for (int i = 0; i < dropletCount; i++)
                {
                    Vector2 randDir = Random.insideUnitCircle.normalized * Random.Range(0.12f, 0.28f);
                    Vector3 velocity = new Vector3(randDir.x, Random.Range(0.25f, 0.50f), randDir.y);

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        position = surfacePos + new Vector3(Random.Range(-0.015f, 0.015f), 0.005f, Random.Range(-0.015f, 0.015f)),
                        velocity = velocity,
                        startLifetime = Random.Range(0.25f, 0.38f),
                        startSize = Random.Range(0.018f, 0.032f),
                        startColor = new Color(0.95f, 1f, 1f, 0.70f),
                        applyShapeToPosition = false
                    };

                    splashParticles.Emit(emitParams, 1);
                }
            }
        }
    }
}

