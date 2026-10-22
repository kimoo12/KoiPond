using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KoiPond
{
    /// <summary>
    /// Controls the Koi fish animations, swimming dynamics, and feeding behaviors.
    /// Supports both autonomous pond wandering and interactive player testing.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class KoiFishController : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator animator;

        [Header("Movement Settings")]
        [Tooltip("Swimming forward speed in meters per second")]
        [SerializeField] private float swimSpeed = 0.65f;

        [Tooltip("Turning rate in degrees per second")]
        [SerializeField] private float turnSpeed = 45f;

        [Tooltip("Banking (roll) angle when turning")]
        [SerializeField] private float bankingAngle = 12f;

        [Header("Autonomous Behavior")]
        [Tooltip("If true, fish naturally wanders around the pond")]
        [SerializeField] private bool autonomousWandering = true;

        [Tooltip("Center of the wandering pond area")]
        [SerializeField] private Vector3 wanderCenter = Vector3.zero;

        [Tooltip("Wander area radius in X and Z")]
        [SerializeField] private float wanderRadius = 4f;

        [Tooltip("Minimum and maximum water depth (Y range)")]
        [SerializeField] private float minY = -1.2f;
        [SerializeField] private float maxY = -0.1f;

        [Tooltip("Minimum time spent in one state (idle or swimming)")]
        [SerializeField] private float minStateDuration = 4f;

        [Tooltip("Maximum time spent in one state")]
        [SerializeField] private float maxStateDuration = 10f;

        [Header("Debug UI")]
        [Tooltip("Show on-screen buttons to test Eat and Swim/Idle")]
        [SerializeField] private bool showOnScreenControls = true;

        private Vector3 currentTargetPoint;
        private float stateTimer;
        private bool isCurrentlySwimming = true;
        private float currentSpeed = 0.75f;
        private float speedVelocity;
        private float currentRoll = 0f;
        private float rollVelocity;
        private float eatPauseTimer = 0f;

        private static readonly int ParamIsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamEat = Animator.StringToHash("Eat");

        private void Reset()
        {
            EnsureAnimator();
        }

        private void OnValidate()
        {
            EnsureAnimator();
        }

        private void Awake()
        {
            EnsureAnimator();
        }

        private void EnsureAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            EnsureAnimator();
            PickNewWanderTarget();
            isCurrentlySwimming = true;
            currentSpeed = swimSpeed;
            stateTimer = Random.Range(minStateDuration, maxStateDuration);

            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, true);
                animator.SetFloat(ParamSpeed, swimSpeed);
                animator.Play("Swim", 0, 0f);
            }
        }

        private void Update()
        {
            HandleInputs();

            if (eatPauseTimer > 0f)
            {
                eatPauseTimer -= Time.deltaTime;
                // Graceful deceleration during eating
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, 0.45f);
                if (currentSpeed > 0.005f)
                {
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }
            }
            else if (autonomousWandering)
            {
                UpdateAutonomousWandering();
            }

            // Keep animator parameters in sync
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, isCurrentlySwimming);
                animator.SetFloat(ParamSpeed, currentSpeed);
            }
        }

        private void HandleInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                {
                    Eat();
                }

                if (keyboard.tKey.wasPressedThisFrame)
                {
                    SetSwimming(!isCurrentlySwimming);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Eat();
                }
            }
#endif
        }

        private void UpdateAutonomousWandering()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                // Smooth state alternation: 75% swimming, 25% idle
                bool nextState = Random.value > 0.25f;
                SetSwimming(nextState);
                if (nextState)
                {
                    PickNewWanderTarget();
                }
                stateTimer = Random.Range(minStateDuration, maxStateDuration);
            }

            if (isCurrentlySwimming)
            {
                // Smooth acceleration into forward glide
                currentSpeed = Mathf.SmoothDamp(currentSpeed, swimSpeed, ref speedVelocity, 0.5f);

                // Steer towards target point
                Vector3 toTarget = currentTargetPoint - transform.position;
                if (toTarget.sqrMagnitude < 0.4f)
                {
                    PickNewWanderTarget();
                    toTarget = currentTargetPoint - transform.position;
                }

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

                    // Organic, smooth banking into curves
                    float yawDiff = Mathf.DeltaAngle(transform.eulerAngles.y, targetRot.eulerAngles.y);
                    float targetRoll = -Mathf.Clamp(yawDiff / 35f, -1f, 1f) * bankingAngle;
                    currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, 0.35f);

                    // Slerp rotation for fluid turn arcs
                    Quaternion smoothYawPitch = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                    Vector3 euler = smoothYawPitch.eulerAngles;
                    transform.rotation = Quaternion.Euler(euler.x, euler.y, currentRoll);

                    // Move forward
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Gentle drift down to idle floating hover
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, 0.7f);
                currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.5f);

                Vector3 euler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(euler.x, euler.y, currentRoll);

                if (currentSpeed > 0.005f)
                {
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }
            }
        }

        public void PickNewWanderTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            float randomY = Random.Range(minY, maxY);
            currentTargetPoint = wanderCenter + new Vector3(randomCircle.x, randomY, randomCircle.y);
        }

        /// <summary>
        /// Trigger the feeding / eating animation.
        /// </summary>
        [ContextMenu("Trigger Eat Animation")]
        public void Eat()
        {
            EnsureAnimator();
            if (animator != null)
            {
                animator.ResetTrigger(ParamEat);
                animator.SetTrigger(ParamEat);
                animator.CrossFadeInFixedTime("Eat", 0.25f, 0, 0f);
            }
            eatPauseTimer = 2.2f; // Pause forward wandering during the surface eating cycle
        }

        /// <summary>
        /// Transition fish to swimming state.
        /// </summary>
        [ContextMenu("Set State: Swimming")]
        public void SetSwimState()
        {
            SetSwimming(true);
        }

        /// <summary>
        /// Transition fish to idle hovering state.
        /// </summary>
        [ContextMenu("Set State: Idle")]
        public void SetIdleState()
        {
            SetSwimming(false);
        }

        public void SetSwimming(bool swim)
        {
            isCurrentlySwimming = swim;
            EnsureAnimator();
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, isCurrentlySwimming);
                if (isCurrentlySwimming)
                {
                    animator.CrossFadeInFixedTime("Swim", 0.35f);
                }
                else
                {
                    animator.CrossFadeInFixedTime("Idle", 0.35f);
                }
            }
        }

        public bool IsSwimming => isCurrentlySwimming;

        private void OnGUI()
        {
            if (!showOnScreenControls) return;

            GUILayout.BeginArea(new Rect(15, 15, 230, 160), GUI.skin.box);
            GUILayout.Label("<b>Koi Fish Controls</b>", GUILayout.Height(22));
            GUILayout.Label("Current: " + (isCurrentlySwimming ? "<color=green>Swimming</color>" : "<color=yellow>Idle</color>"));
            
            if (GUILayout.Button("Feed Koi [E / Space / Click]", GUILayout.Height(32)))
            {
                Eat();
            }

            if (GUILayout.Button(isCurrentlySwimming ? "Switch to Idle [T]" : "Switch to Swim [T]", GUILayout.Height(32)))
            {
                SetSwimming(!isCurrentlySwimming);
            }

            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
            Gizmos.DrawWireSphere(wanderCenter, wanderRadius);

            if (isCurrentlySwimming)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetPoint);
                Gizmos.DrawWireSphere(currentTargetPoint, 0.2f);
            }
        }
    }
}


