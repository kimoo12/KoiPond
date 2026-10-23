using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KoiPond
{
    /// <summary>
    /// Controls the Koi fish animations, swimming dynamics, banking, drifting, feeding,
    /// smooth depth envelope management, and pond boundary containment.
    /// Ensures natural, organic fish locomotion while respecting the water surface,
    /// depth boundaries, and pond bed geometry.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class KoiFishController : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator animator;

        [Header("Movement & Swimming Dynamics")]
        [Tooltip("Base swimming cruising speed in meters per second")]
        [SerializeField] private float swimSpeed = 0.65f;

        [Tooltip("Max speed burst during energetic swimming")]
        [SerializeField] private float burstSpeed = 0.95f;

        [Tooltip("Turning rate in degrees per second")]
        [SerializeField] private float turnSpeed = 40f;

        [Tooltip("Banking (roll) angle when turning")]
        [SerializeField] private float bankingAngle = 18f;

        [Tooltip("Pitch angle limits when ascending or descending")]
        [SerializeField] private float maxPitchAngle = 20f;

        [Header("Natural Organic Drifting & Undulation")]
        [Tooltip("Frequency of natural subtle sinusoidal vertical bobbing")]
        [SerializeField] private float driftBobFrequency = 0.9f;

        [Tooltip("Amplitude of natural subtle vertical bobbing")]
        [SerializeField] private float driftBobAmplitude = 0.035f;

        [Tooltip("Frequency of subtle lateral fish fin sway")]
        [SerializeField] private float lateralSwayFrequency = 1.4f;

        [Tooltip("Amplitude of subtle lateral fish fin sway")]
        [SerializeField] private float lateralSwayAmplitude = 0.02f;

        [Header("Water & Depth Limits")]
        [Tooltip("Reference to the water plane transform. Fish will never swim above this transform's Y position.")]
        [SerializeField] private Transform waterPlane;

        [Tooltip("Reference to the pond bed collider. Fish will not swim outside the pond bed.")]
        [SerializeField] private Collider pondBedCollider;

        [Tooltip("Minimum depth below the water plane (shallowest level fish wanders to).")]
        [SerializeField] private float minDepth = 0.12f;

        [Tooltip("Maximum depth below the water plane (deepest level fish wanders to).")]
        [SerializeField] private float maxDepth = 0.95f;

        [Tooltip("Clearance maintained above the bottom of the pond bed.")]
        [SerializeField] private float bedClearance = 0.10f;

        [Tooltip("Minimum water depth required near shores to keep fish away from dry banks.")]
        [SerializeField] private float shoreMargin = 0.22f;

        [Tooltip("Distance ahead to probe for pond bed banks to steer away smoothly.")]
        [SerializeField] private float obstacleAvoidanceDistance = 1.4f;

        [Header("Autonomous Behavior")]
        [Tooltip("If true, fish naturally wanders around the pond")]
        [SerializeField] private bool autonomousWandering = true;

        [Tooltip("Center of the wandering pond area (fallback if pond bed is unassigned)")]
        [SerializeField] private Vector3 wanderCenter = new Vector3(-0.39f, 0f, -0.50f);

        [Tooltip("Wander area radius in X and Z")]
        [SerializeField] private float wanderRadius = 4.2f;

        [Tooltip("Minimum time spent in one state (idle or swimming)")]
        [SerializeField] private float minStateDuration = 4f;

        [Tooltip("Maximum time spent in one state")]
        [SerializeField] private float maxStateDuration = 9f;

        [Header("Debug UI")]
        [Tooltip("Show on-screen buttons to test Eat and Swim/Idle")]
        [SerializeField] private bool showOnScreenControls = true;

        // Runtime dynamic state
        private Vector3 currentTargetPoint;
        private float stateTimer;
        private bool isCurrentlySwimming = true;
        private float currentSpeed = 0.65f;
        private float targetSpeed = 0.65f;
        private float speedVelocity;
        private float currentRoll = 0f;
        private float rollVelocity;
        private float currentPitch = 0f;
        private float pitchVelocity;
        private float eatTimer = 0f;
        private float eatDuration = 2.5f;
        private Vector3 eatSurfaceTarget;
        private float timeAlive = 0f;
        private float surfaceEatHeightOffset = 0.04f;

        private static readonly int ParamIsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamEat = Animator.StringToHash("Eat");

        /// <summary>
        /// World Y coordinate of the water surface.
        /// </summary>
        public float WaterSurfaceY => waterPlane != null ? waterPlane.position.y : 0f;

        /// <summary>
        /// Shallowest standard wandering world Y coordinate.
        /// </summary>
        public float ShallowestY
        {
            get
            {
                float surfaceY = WaterSurfaceY;
                float d1 = Mathf.Abs(minDepth);
                float d2 = Mathf.Abs(maxDepth);
                float shallow = Mathf.Min(d1, d2);
                return Mathf.Min(surfaceY, surfaceY - shallow);
            }
        }

        /// <summary>
        /// Deepest standard wandering world Y coordinate.
        /// </summary>
        public float DeepestY
        {
            get
            {
                float surfaceY = WaterSurfaceY;
                float d1 = Mathf.Abs(minDepth);
                float d2 = Mathf.Abs(maxDepth);
                float deep = Mathf.Max(d1, d2);
                return surfaceY - deep;
            }
        }

        public Transform WaterPlane
        {
            get => waterPlane;
            set => waterPlane = value;
        }

        public Collider PondBedCollider
        {
            get => pondBedCollider;
            set => pondBedCollider = value;
        }

        public float MinDepth
        {
            get => minDepth;
            set => minDepth = Mathf.Max(0f, value);
        }

        public float MaxDepth
        {
            get => maxDepth;
            set => maxDepth = Mathf.Max(0f, value);
        }

        public bool IsSwimming => isCurrentlySwimming;
        public Vector3 CurrentTargetPoint => currentTargetPoint;

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
            if (minDepth < 0f) minDepth = 0f;
            if (maxDepth < 0f) maxDepth = 0f;
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void EnsureReferences()
        {
            EnsureAnimator();

            if (waterPlane == null)
            {
                var pond1 = GameObject.Find("Pond_1");
                if (pond1 != null)
                {
                    waterPlane = pond1.transform;
                }
                else
                {
                    var waterObj = GameObject.Find("WaterPlane") ?? GameObject.Find("Water");
                    if (waterObj != null)
                    {
                        waterPlane = waterObj.transform;
                    }
                }
            }

            if (pondBedCollider == null)
            {
                var pondBed = GameObject.Find("Pond_Bed");
                if (pondBed != null)
                {
                    pondBedCollider = pondBed.GetComponent<Collider>();
                }
            }
        }

        private void Start()
        {
            EnsureReferences();
            PickNewWanderTarget();
            isCurrentlySwimming = true;
            targetSpeed = swimSpeed;
            currentSpeed = swimSpeed;
            stateTimer = Random.Range(minStateDuration, maxStateDuration);

            // Eased initial soft containment
            EnforcePondConstraints(Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, true);
                animator.SetFloat(ParamSpeed, swimSpeed);
                animator.Play("Swim", 0, 0f);
            }
        }

        private void Update()
        {
            timeAlive += Time.deltaTime;
            HandleInputs();

            if (eatTimer > 0f)
            {
                UpdateEating(Time.deltaTime);
            }
            else if (autonomousWandering)
            {
                UpdateAutonomousWandering(Time.deltaTime);
            }

            // Smooth boundary enforcement
            EnforcePondConstraints(Time.deltaTime);

            // Keep animator parameters in sync with natural speed
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, isCurrentlySwimming);
                animator.SetFloat(ParamSpeed, currentSpeed);
            }
        }

        private void LateUpdate()
        {
            // Safety check: ensure water surface plane is never breached
            float waterY = WaterSurfaceY;
            if (transform.position.y > waterY)
            {
                Vector3 p = transform.position;
                p.y = waterY;
                transform.position = p;
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

        /// <summary>
        /// Updates the autonomous wandering locomotion, turning arcs, smooth banking, and gentle idle drifts.
        /// </summary>
        private void UpdateAutonomousWandering(float dt)
        {
            stateTimer -= dt;
            if (stateTimer <= 0f)
            {
                // Alternation: 70% active swimming, 30% calm idle gliding/hovering
                bool nextState = Random.value > 0.30f;
                SetSwimming(nextState);
                if (nextState)
                {
                    // Vary speed naturally between cruising and occasional gentle burst
                    bool isBurst = Random.value < 0.25f;
                    targetSpeed = isBurst ? Random.Range(swimSpeed * 1.15f, burstSpeed) : Random.Range(swimSpeed * 0.85f, swimSpeed * 1.1f);
                    PickNewWanderTarget();
                }
                else
                {
                    targetSpeed = 0f;
                }
                stateTimer = Random.Range(minStateDuration, maxStateDuration);
            }

            if (isCurrentlySwimming)
            {
                // Smooth acceleration into target speed
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, 0.7f);

                Vector3 toTarget = currentTargetPoint - transform.position;
                if (toTarget.sqrMagnitude < 0.5f)
                {
                    PickNewWanderTarget();
                    toTarget = currentTargetPoint - transform.position;
                }

                Vector3 desiredDirection = toTarget.normalized;

                // Smooth organic bank avoidance
                Vector3 avoidance = CalculatePondBedAvoidance();
                if (avoidance.sqrMagnitude > 0.001f)
                {
                    desiredDirection = (desiredDirection + avoidance * 2.2f).normalized;
                }

                // Smoothly steer toward desired heading
                if (desiredDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(desiredDirection, Vector3.up);
                    Vector3 lookEuler = lookRot.eulerAngles;

                    // Organic pitch calculation with smooth clamp
                    float rawPitch = Mathf.DeltaAngle(0f, lookEuler.x);
                    float targetPitchAngle = Mathf.Clamp(rawPitch, -maxPitchAngle, maxPitchAngle);

                    // Organic banking roll from yaw turn rate
                    float yawDiff = Mathf.DeltaAngle(transform.eulerAngles.y, lookEuler.y);
                    float targetRoll = -Mathf.Clamp(yawDiff / 32f, -1f, 1f) * bankingAngle;

                    currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, 0.38f);
                    currentPitch = Mathf.SmoothDamp(currentPitch, targetPitchAngle, ref pitchVelocity, 0.45f);

                    // Slerp horizontal yaw for smooth graceful turn arcs
                    Quaternion currentYawRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                    Quaternion targetYawRot = Quaternion.Euler(0f, lookEuler.y, 0f);
                    Quaternion newYawRot = Quaternion.RotateTowards(currentYawRot, targetYawRot, turnSpeed * dt);

                    transform.rotation = Quaternion.Euler(currentPitch, newYawRot.eulerAngles.y, currentRoll);

                    // Move forward with gentle natural undulation
                    Vector3 moveDelta = transform.forward * (currentSpeed * dt);

                    // Add subtle natural lateral fin sway
                    float sway = Mathf.Cos(timeAlive * lateralSwayFrequency * (currentSpeed / swimSpeed)) * (lateralSwayAmplitude * currentSpeed * dt);
                    moveDelta += transform.right * sway;

                    MoveFish(moveDelta);
                }
            }
            else
            {
                // Gentle idle drift and floating hover
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, 1.1f);
                currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.8f);
                currentPitch = Mathf.SmoothDamp(currentPitch, 0f, ref pitchVelocity, 0.8f);

                // Natural subtle floating bob and drift while hovering
                float bobVelocity = Mathf.Sin(timeAlive * driftBobFrequency) * driftBobAmplitude;
                float driftSway = Mathf.Sin(timeAlive * 0.7f) * 0.012f;

                Vector3 currentEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(currentPitch, currentEuler.y, currentRoll);

                Vector3 driftVector = (transform.forward * (currentSpeed + 0.02f) + Vector3.up * bobVelocity + transform.right * driftSway) * dt;
                MoveFish(driftVector);
            }
        }

        /// <summary>
        /// Natural eating behavior: fish angles smoothly upward toward the water surface,
        /// performs surface feeding with deceleration, and smoothly glides back down.
        /// </summary>
        private void UpdateEating(float dt)
        {
            eatTimer -= dt;
            float progress = 1f - (eatTimer / eatDuration); // 0 (start) to 1 (end)

            float waterY = WaterSurfaceY;
            float surfaceMouthY = waterY - surfaceEatHeightOffset;

            if (progress < 0.35f)
            {
                // Phase 1: Glide gently up toward surface feeding target
                Vector3 toSurface = eatSurfaceTarget - transform.position;
                if (toSurface.sqrMagnitude > 0.01f)
                {
                    Quaternion targetLook = Quaternion.LookRotation(toSurface.normalized, Vector3.up);
                    // Gentle upward pitch
                    Vector3 euler = targetLook.eulerAngles;
                    float pitchClamped = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -16f, 16f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitchClamped, euler.y, 0f), 4f * dt);
                }

                currentSpeed = Mathf.SmoothDamp(currentSpeed, swimSpeed * 0.4f, ref speedVelocity, 0.4f);
                MoveFish(transform.forward * (currentSpeed * dt));
            }
            else if (progress < 0.75f)
            {
                // Phase 2: Nibbling at the surface, hovering and decelerating naturally
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0.03f, ref speedVelocity, 0.5f);
                currentRoll = Mathf.SmoothDamp(currentRoll, Mathf.Sin(timeAlive * 3f) * 3f, ref rollVelocity, 0.3f);

                Vector3 rot = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(Mathf.SmoothDamp(Mathf.DeltaAngle(0f, rot.x), -4f, ref pitchVelocity, 0.3f), rot.y, currentRoll);

                // Small gentle mouth bobbing at surface
                float nibbleBob = Mathf.Sin(timeAlive * 5f) * 0.008f;
                Vector3 pos = transform.position;
                pos.y = Mathf.MoveTowards(pos.y, surfaceMouthY + nibbleBob, 0.15f * dt);
                transform.position = pos;

                MoveFish(transform.forward * (currentSpeed * dt));
            }
            else
            {
                // Phase 3: Finishing bite, leveling off and gently descending back into water
                currentSpeed = Mathf.SmoothDamp(currentSpeed, swimSpeed * 0.5f, ref speedVelocity, 0.6f);
                currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.5f);

                Vector3 rot = transform.rotation.eulerAngles;
                float targetPitchDown = 8f; // Gentle pitch down to re-submerge smoothly
                transform.rotation = Quaternion.Euler(Mathf.SmoothDamp(Mathf.DeltaAngle(0f, rot.x), targetPitchDown, ref pitchVelocity, 0.5f), rot.y, currentRoll);

                MoveFish(transform.forward * (currentSpeed * dt));

                if (eatTimer <= 0f)
                {
                    PickNewWanderTarget();
                    targetSpeed = swimSpeed;
                }
            }
        }

        /// <summary>
        /// Moves the fish with soft collision deflection against the pond bed.
        /// </summary>
        private void MoveFish(Vector3 moveVector)
        {
            if (pondBedCollider != null && moveVector.sqrMagnitude > 0.000001f)
            {
                Ray moveRay = new Ray(transform.position, moveVector.normalized);
                if (pondBedCollider.Raycast(moveRay, out RaycastHit hit, moveVector.magnitude + 0.12f))
                {
                    // Deflect smoothly along the pond bed contour
                    moveVector = Vector3.ProjectOnPlane(moveVector, hit.normal);
                    PickNewWanderTarget();
                }
            }

            transform.position += moveVector;
        }

        /// <summary>
        /// Probes ahead and diagonally to detect pond bed banks, returning a gentle steering force
        /// that keeps the fish smoothly turning inside deeper water.
        /// </summary>
        private Vector3 CalculatePondBedAvoidance()
        {
            if (pondBedCollider == null) return Vector3.zero;

            Vector3 avoidForce = Vector3.zero;
            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // Forward probe
            if (CheckProbe(pos, forward, obstacleAvoidanceDistance, out RaycastHit fwdHit))
            {
                Vector3 away = fwdHit.normal;
                away.y = 0f;
                float weight = 1f - (fwdHit.distance / obstacleAvoidanceDistance);
                avoidForce += away.normalized * (weight * 2.2f);
            }

            // Forward-Right probe
            Vector3 fwdRight = (forward + right * 0.6f).normalized;
            if (CheckProbe(pos, fwdRight, obstacleAvoidanceDistance * 0.85f, out RaycastHit rHit))
            {
                Vector3 away = rHit.normal;
                away.y = 0f;
                float weight = 1f - (rHit.distance / (obstacleAvoidanceDistance * 0.85f));
                avoidForce += away.normalized * weight;
            }

            // Forward-Left probe
            Vector3 fwdLeft = (forward - right * 0.6f).normalized;
            if (CheckProbe(pos, fwdLeft, obstacleAvoidanceDistance * 0.85f, out RaycastHit lHit))
            {
                Vector3 away = lHit.normal;
                away.y = 0f;
                float weight = 1f - (lHit.distance / (obstacleAvoidanceDistance * 0.85f));
                avoidForce += away.normalized * weight;
            }

            // Soft shore depth check ahead
            Vector3 aheadPos = pos + forward * 0.9f;
            if (TryGetBedHeight(aheadPos.x, aheadPos.z, out float aheadBedY, out _))
            {
                float aheadWaterDepth = WaterSurfaceY - aheadBedY;
                if (aheadWaterDepth < shoreMargin)
                {
                    Vector3 pondCenter = GetPondCenter();
                    Vector3 toCenter = new Vector3(pondCenter.x - pos.x, 0f, pondCenter.z - pos.z);
                    if (toCenter.sqrMagnitude > 0.01f)
                    {
                        float pushStrength = (shoreMargin - aheadWaterDepth) / shoreMargin;
                        avoidForce += toCenter.normalized * (pushStrength * 2.5f);
                    }
                }
            }

            return avoidForce;
        }

        private bool CheckProbe(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
        {
            Ray ray = new Ray(origin, direction);
            return pondBedCollider.Raycast(ray, out hit, distance);
        }

        /// <summary>
        /// Attempts to get the pond bed surface height at a given (X, Z) coordinate.
        /// </summary>
        public bool TryGetBedHeight(float x, float z, out float bedY, out Vector3 bedNormal)
        {
            bedY = DeepestY;
            bedNormal = Vector3.up;

            if (pondBedCollider == null) return false;

            float searchStartY = WaterSurfaceY + 5f;
            Ray downRay = new Ray(new Vector3(x, searchStartY, z), Vector3.down);
            if (pondBedCollider.Raycast(downRay, out RaycastHit hit, 20f))
            {
                bedY = hit.point.y;
                bedNormal = hit.normal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Selects a new wander target inside the submerged pond basin within the depth range.
        /// </summary>
        public void PickNewWanderTarget()
        {
            Vector3 center = GetPondCenter();
            bool foundValidTarget = false;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (TryGetBedHeight(candidate.x, candidate.z, out float bedY, out _))
                {
                    float waterDepth = WaterSurfaceY - bedY;
                    if (waterDepth >= shoreMargin + 0.08f)
                    {
                        float localMinY = Mathf.Max(DeepestY, bedY + bedClearance);
                        float localMaxY = ShallowestY;

                        if (localMinY <= localMaxY)
                        {
                            float targetY = Random.Range(localMinY, localMaxY);
                            targetY = Mathf.Min(targetY, WaterSurfaceY);

                            currentTargetPoint = new Vector3(candidate.x, targetY, candidate.z);
                            foundValidTarget = true;
                            break;
                        }
                    }
                }
            }

            if (!foundValidTarget)
            {
                float safeY = (ShallowestY + DeepestY) * 0.5f;
                safeY = Mathf.Min(safeY, WaterSurfaceY);
                currentTargetPoint = new Vector3(center.x, safeY, center.z);
            }
        }

        /// <summary>
        /// Returns the horizontal center of the pond bed.
        /// </summary>
        public Vector3 GetPondCenter()
        {
            if (pondBedCollider != null)
            {
                Vector3 boundsCenter = pondBedCollider.bounds.center;
                return new Vector3(boundsCenter.x, WaterSurfaceY, boundsCenter.z);
            }

            if (waterPlane != null)
            {
                return new Vector3(waterPlane.position.x, WaterSurfaceY, waterPlane.position.z);
            }

            return wanderCenter;
        }

        /// <summary>
        /// Eased pond containment: smoothly pushes fish away from shores and soft-clamps depth,
        /// with an absolute hard ceiling at the water plane transform position.
        /// </summary>
        public void EnforcePondConstraints(float dt)
        {
            Vector3 pos = transform.position;
            float waterY = WaterSurfaceY;
            float maxAllowedY = (eatTimer > 0f) ? waterY : ShallowestY;
            float minAllowedY = DeepestY;

            // 1. Smooth horizontal containment inside pond bed
            if (pondBedCollider != null)
            {
                Vector3 center = GetPondCenter();

                if (TryGetBedHeight(pos.x, pos.z, out float bedY, out _))
                {
                    float waterDepth = waterY - bedY;
                    if (waterDepth < shoreMargin)
                    {
                        // Eased soft push back toward pond center
                        Vector3 toCenter = new Vector3(center.x - pos.x, 0f, center.z - pos.z);
                        if (toCenter.sqrMagnitude > 0.001f)
                        {
                            float pushFactor = Mathf.Clamp01((shoreMargin - waterDepth) / shoreMargin);
                            pos += toCenter.normalized * (swimSpeed * pushFactor * 2.5f * dt);
                        }
                    }

                    // Keep fish above the bed floor with soft ease
                    minAllowedY = Mathf.Max(minAllowedY, bedY + bedClearance);
                }
                else
                {
                    // Outside collider perimeter: guide smoothly back toward center
                    Vector3 toCenter = new Vector3(center.x - pos.x, 0f, center.z - pos.z);
                    if (toCenter.sqrMagnitude > 0.001f)
                    {
                        pos += toCenter.normalized * (swimSpeed * 3f * dt);
                    }
                    PickNewWanderTarget();
                }
            }

            // 2. Smooth vertical depth clamping
            if (minAllowedY > maxAllowedY)
            {
                minAllowedY = maxAllowedY;
            }

            // Soft clamp between minAllowedY and maxAllowedY
            if (pos.y < minAllowedY)
            {
                pos.y = Mathf.MoveTowards(pos.y, minAllowedY, 1.5f * dt);
                if (pos.y < minAllowedY - 0.1f) pos.y = minAllowedY;
            }
            else if (pos.y > maxAllowedY)
            {
                pos.y = Mathf.MoveTowards(pos.y, maxAllowedY, 1.5f * dt);
                if (pos.y > maxAllowedY + 0.05f) pos.y = maxAllowedY;
            }

            // 3. Absolute hard ceiling: never swim above the water plane transform position
            if (pos.y > waterY)
            {
                pos.y = waterY;
            }

            transform.position = pos;
        }

        /// <summary>
        /// Trigger the feeding / eating animation naturally with surface approach.
        /// </summary>
        [ContextMenu("Trigger Eat Animation")]
        public void Eat()
        {
            EnsureAnimator();
            if (animator != null)
            {
                animator.ResetTrigger(ParamEat);
                animator.SetTrigger(ParamEat);
                animator.CrossFadeInFixedTime("Eat", 0.3f, 0, 0f);
            }

            eatDuration = 2.5f;
            eatTimer = eatDuration;

            // Pick a feeding point at the surface slightly forward from current position
            Vector3 fwdFlat = transform.forward;
            fwdFlat.y = 0f;
            if (fwdFlat.sqrMagnitude < 0.01f) fwdFlat = Vector3.forward;

            Vector3 candidateSurface = transform.position + fwdFlat.normalized * 0.4f;
            candidateSurface.y = WaterSurfaceY - surfaceEatHeightOffset;

            // Verify feeding spot is inside pond bed
            if (TryGetBedHeight(candidateSurface.x, candidateSurface.z, out float bedY, out _))
            {
                if (WaterSurfaceY - bedY >= shoreMargin)
                {
                    eatSurfaceTarget = candidateSurface;
                }
                else
                {
                    Vector3 center = GetPondCenter();
                    eatSurfaceTarget = new Vector3(center.x, WaterSurfaceY - surfaceEatHeightOffset, center.z);
                }
            }
            else
            {
                Vector3 center = GetPondCenter();
                eatSurfaceTarget = new Vector3(center.x, WaterSurfaceY - surfaceEatHeightOffset, center.z);
            }
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
            targetSpeed = swim ? swimSpeed : 0f;
            EnsureAnimator();
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, isCurrentlySwimming);
                if (isCurrentlySwimming)
                {
                    animator.CrossFadeInFixedTime("Swim", 0.4f);
                }
                else
                {
                    animator.CrossFadeInFixedTime("Idle", 0.5f);
                }
            }
        }

        /// <summary>
        /// Advances autonomous wandering and constraint enforcement by a given delta time (useful for testing).
        /// </summary>
        public void SimulateStep(float dt)
        {
            timeAlive += dt;
            if (eatTimer > 0f)
            {
                UpdateEating(dt);
            }
            else if (autonomousWandering)
            {
                UpdateAutonomousWandering(dt);
            }

            EnforcePondConstraints(dt);

            if (transform.position.y > WaterSurfaceY)
            {
                Vector3 p = transform.position;
                p.y = WaterSurfaceY;
                transform.position = p;
            }
        }

        private void OnGUI()
        {
            if (!showOnScreenControls) return;

            GUILayout.BeginArea(new Rect(15, 15, 230, 165), GUI.skin.box);
            GUILayout.Label("<b>Koi Fish Controls</b>", GUILayout.Height(22));
            GUILayout.Label("Current: " + (isCurrentlySwimming ? "<color=green>Swimming</color>" : "<color=yellow>Idle</color>"));
            GUILayout.Label(string.Format("Depth: {0:F2}m (Water: {1:F2}m)", WaterSurfaceY - transform.position.y, WaterSurfaceY));

            if (GUILayout.Button("Feed Koi [E / Space / Click]", GUILayout.Height(30)))
            {
                Eat();
            }

            if (GUILayout.Button(isCurrentlySwimming ? "Switch to Idle [T]" : "Switch to Swim [T]", GUILayout.Height(30)))
            {
                SetSwimming(!isCurrentlySwimming);
            }

            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetPondCenter();

            // Water plane level disk
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(new Vector3(center.x, WaterSurfaceY, center.z), wanderRadius);

            // Shallowest depth limit plane
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(new Vector3(center.x, ShallowestY, center.z), wanderRadius);

            // Deepest depth limit plane
            Gizmos.color = new Color(0f, 0.3f, 0.8f, 0.25f);
            Gizmos.DrawWireSphere(new Vector3(center.x, DeepestY, center.z), wanderRadius);

            // Target visualization
            if (isCurrentlySwimming)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetPoint);
                Gizmos.DrawWireSphere(currentTargetPoint, 0.2f);
            }

            // Avoidance probes
            if (pondBedCollider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, transform.forward * obstacleAvoidanceDistance);
            }
        }
    }
}




