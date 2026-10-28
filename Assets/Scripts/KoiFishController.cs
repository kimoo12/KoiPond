using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KoiPond
{
    public enum KoiState
    {
        Swimming,
        Idle,
        ApproachingFood,
        Feeding,
        Submerging
    }

    /// <summary>
    /// Controls the Koi fish animations, swimming dynamics, banking, drifting, feeding,
    /// smooth depth envelope management, and pond boundary containment.
    /// Ensures natural, organic fish locomotion while strictly respecting the water surface,
    /// pond bed clearance, and solid geometry.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class KoiFishController : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator animator;

        [Header("Movement & Swimming Dynamics")]
        [Tooltip("Base swimming cruising speed in meters per second")]
        [SerializeField] private float swimSpeed = 0.60f;

        [Tooltip("Max speed burst during energetic swimming")]
        [SerializeField] private float burstSpeed = 0.85f;

        [Tooltip("Turning rate in degrees per second")]
        [SerializeField] private float turnSpeed = 28f;

        [Tooltip("Banking (roll) angle when turning")]
        [SerializeField] private float bankingAngle = 6f;

        [Tooltip("Pitch angle limits when ascending or descending")]
        [SerializeField] private float maxPitchAngle = 10f;

        [Header("Natural Organic Drifting & Undulation")]
        [Tooltip("Frequency of natural subtle sinusoidal vertical bobbing")]
        [SerializeField] private float driftBobFrequency = 0.6f;

        [Tooltip("Amplitude of natural subtle vertical bobbing (kept minimal to prevent wobble)")]
        [SerializeField] private float driftBobAmplitude = 0.008f;

        [Header("Water & Depth Limits")]
        [Tooltip("Reference to the water plane transform. Fish will never swim above this transform's Y position.")]
        [SerializeField] private Transform waterPlane;

        [Tooltip("Reference to the pond bed collider. Fish will not swim outside the pond bed.")]
        [SerializeField] private Collider pondBedCollider;

        [Tooltip("Reference to the fish mouth bone transform for precise surface contact.")]
        [SerializeField] private Transform mouthTransform;

        [Tooltip("Reference to the feeding ripple effect system.")]
        [SerializeField] private KoiFeedingRippleEffect feedingRippleEffect;

        [Tooltip("Clearance maintained between the lowest point of the fish and the pond floor.")]
        [SerializeField] private float minFloorClearance = 0.12f;

        [Tooltip("Clearance maintained between the highest dorsal point of the fish and the water surface.")]
        [SerializeField] private float minSurfaceClearance = 0.06f;

        [Tooltip("Minimum water depth required to swim without clipping.")]
        [SerializeField] private float minRequiredWaterDepth = 0.72f;

        [Tooltip("Distance ahead to probe for obstacles and shallow banks.")]
        [SerializeField] private float obstacleAvoidanceDistance = 1.6f;

        [Header("Autonomous Behavior")]
        [Tooltip("If true, fish naturally wanders around the pond")]
        [SerializeField] private bool autonomousWandering = true;

        [Tooltip("Center of the deep basin area")]
        [SerializeField] private Vector3 basinCenter = new Vector3(-0.39f, 0f, -0.50f);

        [Tooltip("Deep basin ellipse radii (X = minor axis, Z = major axis)")]
        [SerializeField] private Vector2 basinRadii = new Vector2(1.4f, 2.5f);

        [Tooltip("Minimum time spent in one state (idle or swimming)")]
        [SerializeField] private float minStateDuration = 5f;

        [Tooltip("Maximum time spent in one state")]
        [SerializeField] private float maxStateDuration = 10f;

        [Header("Debug UI")]
        [Tooltip("Show on-screen buttons to test Eat and Swim/Idle")]
        [SerializeField] private bool showOnScreenControls = true;

        // Bone references for procedural secondary animation
        private Transform pectoralFinL;
        private Transform pectoralFinR;
        private Transform spineBack;
        private Transform spineTail1;
        private Transform spineTail2;
        private Transform tailFin;

        // Current state
        private KoiState currentState = KoiState.Swimming;

        // Runtime dynamic state
        private Vector3 currentTargetPoint;
        private float stateTimer;
        private float currentSpeed = 0.60f;
        private float targetSpeed = 0.60f;
        private float speedVelocity;
        private float currentRoll = 0f;
        private float rollVelocity;
        private float currentPitch = 0f;
        private float pitchVelocity;
        private float timeAlive = 0f;
        private float yawTurnRate = 0f;
        private float previousYaw = 0f;

        // Feeding state variables
        private float feedTimer = 0f;
        private Vector3 targetBodyFeedingPos;
        private Vector3 feedPosVelocity;
        private Vector3 postFeedResumeHeading;
        private bool bite1Fired;
        private bool bite2Fired;
        private bool bite3Fired;
        private bool bite4Fired;

        private static readonly int ParamIsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamEat = Animator.StringToHash("Eat");

        public KoiState CurrentState => currentState;
        public float WaterSurfaceY => waterPlane != null ? waterPlane.position.y : 0f;

        public bool IsSwimming => currentState == KoiState.Swimming;
        public Vector3 CurrentTargetPoint => currentTargetPoint;

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
            if (minFloorClearance < 0.05f) minFloorClearance = 0.05f;
            if (minSurfaceClearance < 0.02f) minSurfaceClearance = 0.02f;
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (mouthTransform == null)
            {
                mouthTransform = transform.Find("Armature/Root/Spine_Front/Head/Mouth");
            }

            if (pectoralFinL == null)
            {
                pectoralFinL = transform.Find("Armature/Root/Spine_Front/PectoralFin_L");
            }

            if (pectoralFinR == null)
            {
                pectoralFinR = transform.Find("Armature/Root/Spine_Front/PectoralFin_R");
            }

            if (spineBack == null)
            {
                spineBack = transform.Find("Armature/Root/Spine_Back");
            }

            if (spineTail1 == null)
            {
                spineTail1 = transform.Find("Armature/Root/Spine_Back/Spine_Tail1");
            }

            if (spineTail2 == null)
            {
                spineTail2 = transform.Find("Armature/Root/Spine_Back/Spine_Tail1/Spine_Tail2");
            }

            if (tailFin == null)
            {
                tailFin = transform.Find("Armature/Root/Spine_Back/Spine_Tail1/Spine_Tail2/TailFin");
            }

            if (feedingRippleEffect == null)
            {
                feedingRippleEffect = Object.FindAnyObjectByType<KoiFeedingRippleEffect>();
            }

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
            currentState = KoiState.Swimming;
            targetSpeed = swimSpeed;
            currentSpeed = swimSpeed;
            stateTimer = Random.Range(minStateDuration, maxStateDuration);
            previousYaw = transform.eulerAngles.y;

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
            float dt = Time.deltaTime;
            timeAlive += dt;
            HandleInputs();

            // Track yaw turn rate for smooth fin response
            float currentYaw = transform.eulerAngles.y;
            yawTurnRate = Mathf.DeltaAngle(previousYaw, currentYaw) / Mathf.Max(0.001f, dt);
            previousYaw = currentYaw;

            switch (currentState)
            {
                case KoiState.ApproachingFood:
                    UpdateApproachingFood(dt);
                    break;
                case KoiState.Feeding:
                    UpdateFeeding(dt);
                    break;
                case KoiState.Submerging:
                    UpdateSubmerging(dt);
                    break;
                case KoiState.Swimming:
                case KoiState.Idle:
                    if (autonomousWandering)
                    {
                        UpdateAutonomousWandering(dt);
                    }
                    EnforcePondConstraints(dt);
                    break;
            }

            // Sync animator speed smoothly with swimming speed
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, currentState == KoiState.Swimming || currentState == KoiState.ApproachingFood || currentState == KoiState.Submerging);
                animator.SetFloat(ParamSpeed, currentSpeed);

                if (currentState == KoiState.Swimming)
                {
                    animator.speed = Mathf.Lerp(0.80f, 1.20f, currentSpeed / burstSpeed);
                }
                else if (currentState == KoiState.Idle)
                {
                    animator.speed = 0.75f;
                }
                else
                {
                    animator.speed = 1.0f;
                }
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float waterY = WaterSurfaceY;

            // 1. Soft ceiling: Mouth must never exceed the water surface
            if (mouthTransform != null && mouthTransform.position.y > waterY)
            {
                Vector3 p = transform.position;
                p.y -= (mouthTransform.position.y - waterY);
                transform.position = p;
            }

            // 2. Gentle Pectoral Fin Paddling & Steering (Subtle, no excessive flapping)
            if (currentState == KoiState.Swimming || currentState == KoiState.Idle)
            {
                float paddleFreq = Mathf.Lerp(1.2f, 2.0f, currentSpeed / swimSpeed);
                float paddleAngle = Mathf.Sin(timeAlive * paddleFreq) * 2.2f * Mathf.Clamp01(currentSpeed / swimSpeed + 0.2f);

                float steerYaw = Mathf.Clamp(yawTurnRate / 24f, -1f, 1f);
                float leftFinRoll = paddleAngle - steerYaw * 4f;
                float rightFinRoll = -paddleAngle - steerYaw * 4f;

                if (pectoralFinL != null)
                {
                    pectoralFinL.localRotation *= Quaternion.Euler(0f, leftFinRoll, -paddleAngle * 0.2f);
                }
                if (pectoralFinR != null)
                {
                    pectoralFinR.localRotation *= Quaternion.Euler(0f, rightFinRoll, paddleAngle * 0.2f);
                }
            }

            // 4. Subtle, Smooth Secondary Spine Undulation (Gentle tail propulsion)
            if (currentState == KoiState.Swimming)
            {
                float undulateFreq = Mathf.Lerp(1.2f, 2.0f, currentSpeed / swimSpeed);
                float undulateAngle = Mathf.Sin(timeAlive * undulateFreq) * (1.2f * (currentSpeed / swimSpeed));

                if (spineBack != null) spineBack.localRotation *= Quaternion.Euler(0f, undulateAngle * 0.25f, 0f);
                if (spineTail1 != null) spineTail1.localRotation *= Quaternion.Euler(0f, -undulateAngle * 0.4f, 0f);
                if (spineTail2 != null) spineTail2.localRotation *= Quaternion.Euler(0f, undulateAngle * 0.65f, 0f);
                if (tailFin != null) tailFin.localRotation *= Quaternion.Euler(0f, -undulateAngle * 0.85f, 0f);
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
                    SetSwimming(currentState != KoiState.Swimming);
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
        /// Updates autonomous wandering locomotion, smooth arcs, minimal banking, and zero body wobble.
        /// </summary>
        private void UpdateAutonomousWandering(float dt)
        {
            stateTimer -= dt;
            if (stateTimer <= 0f)
            {
                bool nextState = Random.value > 0.25f;
                SetSwimming(nextState);
                if (nextState)
                {
                    bool isBurst = Random.value < 0.20f;
                    targetSpeed = isBurst ? Random.Range(swimSpeed * 1.10f, burstSpeed) : Random.Range(swimSpeed * 0.90f, swimSpeed * 1.05f);
                    PickNewWanderTarget();
                }
                else
                {
                    targetSpeed = 0f;
                }
                stateTimer = Random.Range(minStateDuration, maxStateDuration);
            }

            if (currentState == KoiState.Swimming)
            {
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, 0.9f);

                Vector3 toTarget = currentTargetPoint - transform.position;
                if (toTarget.sqrMagnitude < 0.7f)
                {
                    PickNewWanderTarget();
                    toTarget = currentTargetPoint - transform.position;
                }

                Vector3 desiredDirection = toTarget.normalized;

                // Smooth obstacle avoidance
                Vector3 avoidance = CalculatePondBedAvoidance();
                if (avoidance.sqrMagnitude > 0.001f)
                {
                    desiredDirection = (desiredDirection + avoidance * 2.2f).normalized;
                }

                if (desiredDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(desiredDirection, Vector3.up);
                    Vector3 lookEuler = lookRot.eulerAngles;

                    // Clamped, subtle pitch & bank
                    float rawPitch = Mathf.DeltaAngle(0f, lookEuler.x);
                    float targetPitchAngle = Mathf.Clamp(rawPitch, -maxPitchAngle, maxPitchAngle);

                    float yawDiff = Mathf.DeltaAngle(transform.eulerAngles.y, lookEuler.y);
                    float targetRoll = -Mathf.Clamp(yawDiff / 32f, -1f, 1f) * bankingAngle;

                    currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, 0.6f);
                    currentPitch = Mathf.SmoothDamp(currentPitch, targetPitchAngle, ref pitchVelocity, 0.7f);

                    Quaternion currentYawRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                    Quaternion targetYawRot = Quaternion.Euler(0f, lookEuler.y, 0f);
                    Quaternion newYawRot = Quaternion.RotateTowards(currentYawRot, targetYawRot, turnSpeed * dt);

                    transform.rotation = Quaternion.Euler(currentPitch, newYawRot.eulerAngles.y, currentRoll);

                    // Forward locomotion (clean and stable forward motion)
                    Vector3 moveDelta = transform.forward * (currentSpeed * dt);
                    MoveFish(moveDelta);
                }
            }
            else
            {
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, 1.2f);
                currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 1.0f);
                currentPitch = Mathf.SmoothDamp(currentPitch, 0f, ref pitchVelocity, 1.0f);

                float bobVelocity = Mathf.Sin(timeAlive * driftBobFrequency) * driftBobAmplitude;

                Vector3 currentEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(currentPitch, currentEuler.y, currentRoll);

                Vector3 driftVector = (transform.forward * (currentSpeed + 0.01f) + Vector3.up * bobVelocity) * dt;
                MoveFish(driftVector);
            }
        }

        private void UpdateApproachingFood(float dt)
        {
            feedTimer += dt;

            transform.position = Vector3.SmoothDamp(transform.position, targetBodyFeedingPos, ref feedPosVelocity, 0.45f);

            float targetPitchAngle = -14f;
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitchAngle, ref pitchVelocity, 0.40f);
            currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.40f);
            transform.rotation = Quaternion.Euler(currentPitch, transform.eulerAngles.y, currentRoll);

            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0.03f, ref speedVelocity, 0.45f);

            if (feedTimer >= 0.65f)
            {
                currentState = KoiState.Feeding;
            }
        }

        private void UpdateFeeding(float dt)
        {
            feedTimer += dt;

            float hoverBob = Mathf.Sin(timeAlive * 4f) * 0.002f;
            Vector3 desiredPos = targetBodyFeedingPos + Vector3.up * hoverBob;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref feedPosVelocity, 0.30f);

            float targetPitchAngle = -14f;
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitchAngle, ref pitchVelocity, 0.30f);
            currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.40f);
            transform.rotation = Quaternion.Euler(currentPitch, transform.eulerAngles.y, currentRoll);

            Vector3 mouthPos = mouthTransform != null ? mouthTransform.position : transform.position + transform.forward * 0.35f;

            if (feedTimer >= 0.72f && !bite1Fired)
            {
                bite1Fired = true;
                if (feedingRippleEffect != null)
                {
                    feedingRippleEffect.SpawnBiteRipple(mouthPos, WaterSurfaceY, includeSplash: true);
                }
            }
            else if (feedTimer >= 1.03f && !bite2Fired)
            {
                bite2Fired = true;
                if (feedingRippleEffect != null)
                {
                    feedingRippleEffect.SpawnBiteRipple(mouthPos, WaterSurfaceY, includeSplash: false);
                }
            }
            else if (feedTimer >= 1.37f && !bite3Fired)
            {
                bite3Fired = true;
                if (feedingRippleEffect != null)
                {
                    feedingRippleEffect.SpawnBiteRipple(mouthPos, WaterSurfaceY, includeSplash: false);
                }
            }
            else if (feedTimer >= 1.68f && !bite4Fired)
            {
                bite4Fired = true;
                if (feedingRippleEffect != null)
                {
                    feedingRippleEffect.SpawnBiteRipple(mouthPos, WaterSurfaceY, includeSplash: false);
                }
            }

            if (feedTimer >= 1.95f)
            {
                currentState = KoiState.Submerging;
                // Seamless exit: set resume heading forward along current body heading
                postFeedResumeHeading = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                if (postFeedResumeHeading.sqrMagnitude < 0.01f) postFeedResumeHeading = Vector3.forward;
                currentTargetPoint = transform.position + postFeedResumeHeading * 1.8f;
                currentTargetPoint.y = WaterSurfaceY - 0.45f;
            }
        }

        private void UpdateSubmerging(float dt)
        {
            feedTimer += dt;

            // Smoothly pitch from -14 to +8, then back to 0 as it levels off
            float submergePhase = (feedTimer - 1.95f) / 1.15f; // 0.0 to 1.0
            float targetPitchAngle = Mathf.Sin(submergePhase * Mathf.PI) * 7f; // Peaks at +7 degrees midway, returns smoothly to 0

            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitchAngle, ref pitchVelocity, 0.45f);
            currentRoll = Mathf.SmoothDamp(currentRoll, 0f, ref rollVelocity, 0.45f);
            transform.rotation = Quaternion.Euler(currentPitch, transform.eulerAngles.y, currentRoll);

            // Smoothly accelerate forward along the existing heading without sudden speed bursts
            currentSpeed = Mathf.SmoothDamp(currentSpeed, swimSpeed * 0.85f, ref speedVelocity, 0.65f);
            Vector3 moveDelta = transform.forward * (currentSpeed * dt);
            MoveFish(moveDelta);

            // Smoothly descent Y position into normal swimming depth envelope
            float targetSubmergeY = WaterSurfaceY - 0.42f;
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, targetSubmergeY, 0.22f * dt);
            transform.position = p;

            if (feedTimer >= 3.10f)
            {
                PickNewWanderTarget();
                currentState = KoiState.Swimming;
                targetSpeed = swimSpeed;
                stateTimer = Random.Range(minStateDuration, maxStateDuration);
            }
        }

        private void MoveFish(Vector3 moveVector)
        {
            if (pondBedCollider != null && moveVector.sqrMagnitude > 0.000001f)
            {
                Ray moveRay = new Ray(transform.position, moveVector.normalized);
                if (pondBedCollider.Raycast(moveRay, out RaycastHit hit, moveVector.magnitude + 0.35f))
                {
                    moveVector = Vector3.ProjectOnPlane(moveVector, hit.normal);
                    PickNewWanderTarget();
                }
            }

            transform.position += moveVector;
        }

        private Vector3 CalculatePondBedAvoidance()
        {
            if (pondBedCollider == null) return Vector3.zero;

            Vector3 avoidForce = Vector3.zero;
            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            if (CheckProbe(pos, forward, obstacleAvoidanceDistance, out RaycastHit fwdHit))
            {
                Vector3 away = fwdHit.normal;
                away.y = 0f;
                float weight = 1f - (fwdHit.distance / obstacleAvoidanceDistance);
                avoidForce += away.normalized * (weight * 2.4f);
            }

            Vector3 fwdRight = (forward + right * 0.65f).normalized;
            if (CheckProbe(pos, fwdRight, obstacleAvoidanceDistance * 0.85f, out RaycastHit rHit))
            {
                Vector3 away = rHit.normal;
                away.y = 0f;
                float weight = 1f - (rHit.distance / (obstacleAvoidanceDistance * 0.85f));
                avoidForce += away.normalized * (weight * 1.4f);
            }

            Vector3 fwdLeft = (forward - right * 0.65f).normalized;
            if (CheckProbe(pos, fwdLeft, obstacleAvoidanceDistance * 0.85f, out RaycastHit lHit))
            {
                Vector3 away = lHit.normal;
                away.y = 0f;
                float weight = 1f - (lHit.distance / (obstacleAvoidanceDistance * 0.85f));
                avoidForce += away.normalized * (weight * 1.4f);
            }

            Vector3 aheadPos = pos + forward * 1.0f;
            if (TryGetBedHeight(aheadPos.x, aheadPos.z, out float aheadBedY, out _))
            {
                float aheadWaterDepth = WaterSurfaceY - aheadBedY;
                if (aheadWaterDepth < minRequiredWaterDepth)
                {
                    Vector3 center = GetPondCenter();
                    Vector3 toCenter = new Vector3(center.x - pos.x, 0f, center.z - pos.z);
                    if (toCenter.sqrMagnitude > 0.01f)
                    {
                        float pushStrength = (minRequiredWaterDepth - aheadWaterDepth) / minRequiredWaterDepth;
                        avoidForce += toCenter.normalized * (pushStrength * 2.8f);
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

        public float GetHighestBedUnderBody(Vector3 fishPos, Vector3 forward, Vector3 right)
        {
            float maxBedY = -999f;
            float scaleZ = transform.lossyScale.z;
            float scaleX = transform.lossyScale.x;

            Vector3[] samplePoints = new Vector3[] {
                fishPos,
                fishPos + forward * (0.35f * scaleZ),
                fishPos - forward * (0.35f * scaleZ),
                fishPos + right * (0.15f * scaleX),
                fishPos - right * (0.15f * scaleX)
            };

            for (int i = 0; i < samplePoints.Length; i++)
            {
                if (TryGetBedHeight(samplePoints[i].x, samplePoints[i].z, out float bY, out _))
                {
                    if (bY > maxBedY) maxBedY = bY;
                }
            }

            return maxBedY;
        }

        public bool TryGetBedHeight(float x, float z, out float bedY, out Vector3 bedNormal)
        {
            bedY = WaterSurfaceY - 1.10f;
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

        public void PickNewWanderTarget()
        {
            Vector3 center = GetPondCenter();
            bool foundValidTarget = false;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle;
                Vector3 candidate = center + new Vector3(randomCircle.x * basinRadii.x, 0f, randomCircle.y * basinRadii.y);

                if (TryGetBedHeight(candidate.x, candidate.z, out float bedY, out _))
                {
                    float waterDepth = WaterSurfaceY - bedY;
                    if (waterDepth >= minRequiredWaterDepth + 0.05f)
                    {
                        float scaleY = transform.lossyScale.y;
                        float bottomExtent = 0.28f * scaleY + minFloorClearance;
                        float topExtent = 0.26f * scaleY + minSurfaceClearance;

                        float localMinY = bedY + bottomExtent;
                        float localMaxY = WaterSurfaceY - topExtent;

                        if (localMinY <= localMaxY)
                        {
                            float targetY = Random.Range(localMinY, localMaxY);
                            currentTargetPoint = new Vector3(candidate.x, targetY, candidate.z);
                            foundValidTarget = true;
                            break;
                        }
                    }
                }
            }

            if (!foundValidTarget)
            {
                float safeY = WaterSurfaceY - 0.42f;
                currentTargetPoint = new Vector3(center.x, safeY, center.z);
            }
        }

        public Vector3 GetPondCenter()
        {
            if (pondBedCollider != null)
            {
                Vector3 boundsCenter = pondBedCollider.bounds.center;
                return new Vector3(boundsCenter.x, WaterSurfaceY, boundsCenter.z);
            }

            return basinCenter;
        }

        public void EnforcePondConstraints(float dt)
        {
            Vector3 pos = transform.position;
            float waterY = WaterSurfaceY;
            Vector3 center = GetPondCenter();

            float scaleY = transform.lossyScale.y;
            float scaleZ = transform.lossyScale.z;
            float fishHalfLength = 0.30f * scaleZ;

            float pitchRad = Mathf.Abs(currentPitch) * Mathf.Deg2Rad;
            float pitchDrop = Mathf.Sin(pitchRad) * (fishHalfLength * 0.7f);

            float highestBedUnderFish = GetHighestBedUnderBody(pos, transform.forward, transform.right);

            float bottomExtent = 0.28f * scaleY + pitchDrop + minFloorClearance;
            float minAllowedY = highestBedUnderFish + bottomExtent;

            float topExtent = 0.26f * scaleY + pitchDrop + minSurfaceClearance;
            float maxAllowedY = waterY - topExtent;

            float currentWaterDepth = waterY - highestBedUnderFish;
            if (currentWaterDepth < minRequiredWaterDepth)
            {
                Vector3 toCenter = new Vector3(center.x - pos.x, 0f, center.z - pos.z);
                if (toCenter.sqrMagnitude > 0.001f)
                {
                    float pushFactor = Mathf.Clamp01((minRequiredWaterDepth - currentWaterDepth) / minRequiredWaterDepth);
                    pos += toCenter.normalized * (swimSpeed * pushFactor * 3.0f * dt);
                }
            }

            if (minAllowedY > maxAllowedY)
            {
                maxAllowedY = minAllowedY + 0.02f;
            }

            // Smooth gentle dampening to eliminate vertical snapping or bobbing
            if (pos.y < minAllowedY)
            {
                pos.y = Mathf.MoveTowards(pos.y, minAllowedY, 1.2f * dt);
                if (pos.y < minAllowedY - 0.05f) pos.y = minAllowedY;
            }
            else if (pos.y > maxAllowedY)
            {
                pos.y = Mathf.MoveTowards(pos.y, maxAllowedY, 1.2f * dt);
                if (pos.y > maxAllowedY + 0.03f) pos.y = maxAllowedY;
            }

            transform.position = pos;
        }

        [ContextMenu("Trigger Eat Animation")]
        public void Eat()
        {
            if (currentState == KoiState.Feeding || currentState == KoiState.ApproachingFood || currentState == KoiState.Submerging) return;

            EnsureReferences();

            if (animator != null)
            {
                animator.ResetTrigger(ParamEat);
                animator.SetTrigger(ParamEat);
                animator.CrossFadeInFixedTime("Eat", 0.30f, 0, 0f);
            }

            currentState = KoiState.ApproachingFood;
            feedTimer = 0f;
            bite1Fired = false;
            bite2Fired = false;
            bite3Fired = false;
            bite4Fired = false;

            Vector3 fwdFlat = transform.forward;
            fwdFlat.y = 0f;
            if (fwdFlat.sqrMagnitude < 0.01f) fwdFlat = Vector3.forward;
            fwdFlat.Normalize();

            Vector3 surfaceTarget = transform.position + fwdFlat * 0.40f;
            surfaceTarget.y = WaterSurfaceY;

            targetBodyFeedingPos = new Vector3(
                surfaceTarget.x - fwdFlat.x * 0.35f,
                WaterSurfaceY - 0.23f,
                surfaceTarget.z - fwdFlat.z * 0.35f
            );

            // Keep within pond bounds
            if (TryGetBedHeight(targetBodyFeedingPos.x, targetBodyFeedingPos.z, out float bedY, out _))
            {
                if (WaterSurfaceY - bedY < 0.35f) // Near very shallow rim
                {
                    Vector3 center = GetPondCenter();
                    targetBodyFeedingPos = new Vector3(center.x, WaterSurfaceY - 0.23f, center.z);
                }
            }
        }

        [ContextMenu("Set State: Swimming")]
        public void SetSwimState()
        {
            SetSwimming(true);
        }

        [ContextMenu("Set State: Idle")]
        public void SetIdleState()
        {
            SetSwimming(false);
        }

        public void SetSwimming(bool swim)
        {
            if (currentState == KoiState.ApproachingFood || currentState == KoiState.Feeding || currentState == KoiState.Submerging) return;

            currentState = swim ? KoiState.Swimming : KoiState.Idle;
            targetSpeed = swim ? swimSpeed : 0f;
            EnsureReferences();
            if (animator != null)
            {
                animator.SetBool(ParamIsSwimming, swim);
                if (swim)
                {
                    animator.CrossFadeInFixedTime("Swim", 0.5f);
                }
                else
                {
                    animator.CrossFadeInFixedTime("Idle", 0.6f);
                }
            }
        }

        private void OnGUI()
        {
            if (!showOnScreenControls) return;

            GUILayout.BeginArea(new Rect(15, 15, 230, 165), GUI.skin.box);
            GUILayout.Label("<b>Koi Fish Controls</b>", GUILayout.Height(22));
            GUILayout.Label("State: <color=cyan>" + currentState.ToString() + "</color>");
            GUILayout.Label(string.Format("Depth: {0:F2}m (Water: {1:F2}m)", WaterSurfaceY - transform.position.y, WaterSurfaceY));

            if (GUILayout.Button("Feed Koi [E / Space / Click]", GUILayout.Height(30)))
            {
                Eat();
            }

            if (GUILayout.Button(currentState == KoiState.Swimming ? "Switch to Idle [T]" : "Switch to Swim [T]", GUILayout.Height(30)))
            {
                SetSwimming(currentState != KoiState.Swimming);
            }

            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetPondCenter();

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(new Vector3(center.x, WaterSurfaceY, center.z), basinRadii.y);

            if (currentState == KoiState.Swimming)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetPoint);
                Gizmos.DrawWireSphere(currentTargetPoint, 0.2f);
            }
        }
    }
}







