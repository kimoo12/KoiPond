using UnityEngine;

namespace KoiPond
{
    /// <summary>
    /// Spawns natural, expanding and overlapping circular ripples and subtle bite disturbances
    /// on the pond water surface synchronized with koi feeding behavior.
    /// </summary>
    public class KoiFeedingRippleEffect : MonoBehaviour
    {
        [Header("Particle Systems")]
        [Tooltip("Particle system responsible for expanding circular ripples")]
        [SerializeField] private ParticleSystem rippleParticles;

        [Tooltip("Particle system responsible for subtle initial bite disturbance / splash")]
        [SerializeField] private ParticleSystem splashParticles;

        [Header("Ripple Settings")]
        [Tooltip("Minimum time between ripple wave emissions during feeding")]
        [SerializeField] private float rippleInterval = 0.32f;

        [Tooltip("Lifetime of each expanding ripple ring in seconds")]
        [SerializeField] private float rippleLifetime = 2.0f;

        [Tooltip("Starting size of the ripple ring in meters")]
        [SerializeField] private float rippleStartSize = 0.20f;

        [Tooltip("Maximum expanded size of ripple ring in meters")]
        [SerializeField] private float rippleMaxSize = 1.25f;

        [Tooltip("Maximum alpha opacity for ripple rings")]
        [Range(0.1f, 1f)]
        [SerializeField] private float rippleOpacity = 0.55f;

        [Header("Materials (Optional Fallbacks)")]
        [SerializeField] private Material rippleMaterial;
        [SerializeField] private Material splashMaterial;

        private float rippleTimer;
        private bool isFeeding;
        private Vector3 currentMouthPosition;
        private float waterSurfaceY = 0f;

        public bool IsFeeding => isFeeding;

        private void Awake()
        {
            EnsureParticleSystems();
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
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = rippleLifetime;
            main.startSpeed = 0f;
            main.startSize = rippleStartSize;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startColor = new Color(0.9f, 0.98f, 1f, rippleOpacity);

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;

            // Size over lifetime: expands smoothly from start size to max size
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(new Keyframe(0f, 1f, 1.8f, 1.8f));
            sizeCurve.AddKey(new Keyframe(1f, rippleMaxSize / Mathf.Max(0.01f, rippleStartSize), 0.2f, 0f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: soft fade out
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.85f, 0.95f, 0.98f), 0.6f),
                    new GradientColorKey(new Color(0.75f, 0.92f, 0.95f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(rippleOpacity, 0.15f),
                    new GradientAlphaKey(rippleOpacity * 0.7f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                rend.alignment = ParticleSystemRenderSpace.World;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

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
            main.loop = false;
            main.playOnAwake = false;
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
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

                if (splashMaterial != null)
                {
                    rend.sharedMaterial = splashMaterial;
                }
            }
        }

        private void Update()
        {
            if (isFeeding)
            {
                rippleTimer -= Time.deltaTime;
                if (rippleTimer <= 0f)
                {
                    rippleTimer = rippleInterval;
                    EmitCircularRipple(currentMouthPosition);
                }
            }
        }

        /// <summary>
        /// Begins surface feeding ripple sequence: fires an initial small disturbance
        /// and starts rhythmic concentric ripple generation at the mouth water-contact point.
        /// </summary>
        public void StartFeeding(Vector3 mouthPos, float waterY)
        {
            waterSurfaceY = waterY;
            isFeeding = true;
            currentMouthPosition = new Vector3(mouthPos.x, waterSurfaceY + 0.003f, mouthPos.z);
            rippleTimer = 0f; // Emit first ripple immediately

            // Small subtle splash/disturbance at initial surface touch
            SpawnInitialDisturbance(currentMouthPosition);
        }

        /// <summary>
        /// Updates the current feeding water contact position as the koi moves slightly.
        /// </summary>
        public void UpdateFeedingPosition(Vector3 mouthPos)
        {
            currentMouthPosition = new Vector3(mouthPos.x, waterSurfaceY + 0.003f, mouthPos.z);
        }

        /// <summary>
        /// Stops spawning new feeding ripples when the fish completes feeding.
        /// Existing ripples continue their natural expansion and fading.
        /// </summary>
        public void StopFeeding()
        {
            isFeeding = false;
        }

        /// <summary>
        /// Emits an individual expanding circular ripple at the given surface point.
        /// </summary>
        public void EmitCircularRipple(Vector3 surfacePos)
        {
            if (rippleParticles == null) return;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = new Vector3(surfacePos.x, waterSurfaceY + 0.003f, surfacePos.z),
                startLifetime = rippleLifetime,
                startSize = rippleStartSize + Random.Range(-0.02f, 0.02f),
                startColor = new Color(0.92f, 0.98f, 1f, rippleOpacity + Random.Range(-0.05f, 0.05f)),
                applyShapeToPosition = false
            };

            rippleParticles.Emit(emitParams, 1);
        }

        /// <summary>
        /// Subtle initial bite disturbance: a small expanding ring and 4-6 micro droplets.
        /// </summary>
        private void SpawnInitialDisturbance(Vector3 surfacePos)
        {
            // Initial distinct ripple ring
            EmitCircularRipple(surfacePos);

            // Small, subtle droplet burst
            if (splashParticles != null)
            {
                int dropletCount = Random.Range(4, 7);
                for (int i = 0; i < dropletCount; i++)
                {
                    Vector2 randDir = Random.insideUnitCircle.normalized * Random.Range(0.15f, 0.35f);
                    Vector3 velocity = new Vector3(randDir.x, Random.Range(0.35f, 0.65f), randDir.y);

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        position = surfacePos + new Vector3(Random.Range(-0.02f, 0.02f), 0.01f, Random.Range(-0.02f, 0.02f)),
                        velocity = velocity,
                        startLifetime = Random.Range(0.28f, 0.42f),
                        startSize = Random.Range(0.02f, 0.038f),
                        startColor = new Color(0.95f, 1f, 1f, 0.75f),
                        applyShapeToPosition = false
                    };

                    splashParticles.Emit(emitParams, 1);
                }
            }
        }
    }
}
