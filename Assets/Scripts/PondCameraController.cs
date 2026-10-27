using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KoiPond
{
    /// <summary>
    /// Smooth orbit camera that pivots around the pond center.
    /// Supports right-mouse drag, middle-mouse drag, A/D or Arrow keys, and mouse wheel zoom.
    /// </summary>
    public class PondCameraController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Center point to orbit around")]
        [SerializeField] private Vector3 targetCenter = new Vector3(-0.39f, 0.0f, -0.50f);

        [Tooltip("Transform target to orbit around if assigned (overrides targetCenter)")]
        [SerializeField] private Transform targetTransform;

        [Header("Orbit Controls")]
        [Tooltip("When true, camera position and angle remain fixed in place")]
        [SerializeField] private bool lockControls = false;

        [Tooltip("Mouse orbit sensitivity")]
        [SerializeField] private float mouseSensitivity = 3.5f;

        [Tooltip("Keyboard rotation speed in degrees per second")]
        [SerializeField] private float keyboardSpeed = 45f;

        [Tooltip("Smooth damping for orbit movement (0 = instantaneous, higher = smoother)")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Pitch Limits")]
        [Tooltip("Minimum pitch angle (looking near horizon)")]
        [SerializeField] private float minPitch = 15f;

        [Tooltip("Maximum pitch angle (looking almost straight down)")]
        [SerializeField] private float maxPitch = 80f;

        [Header("Distance / Zoom")]
        [Tooltip("Current distance to target")]
        [SerializeField] private float distance = 15.0f;

        [Tooltip("Minimum zoom distance")]
        [SerializeField] private float minDistance = 6.0f;

        [Tooltip("Maximum zoom distance")]
        [SerializeField] private float maxDistance = 25.0f;

        [Tooltip("Zoom sensitivity")]
        [SerializeField] private float zoomSpeed = 4.0f;

        [Header("UI Instructions")]
        [Tooltip("Show brief helper text on screen")]
        [SerializeField] private bool showControlsHint = true;

        private float currentYaw;
        private float currentPitch;
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;

        private float yawVelocity;
        private float pitchVelocity;
        private float distanceVelocity;

        private bool isDragging = false;

        private void Start()
        {
            Vector3 center = GetPivotPoint();

            // Calculate starting angles and distance from current position relative to target
            Vector3 offset = transform.position - center;
            targetDistance = offset.magnitude;
            if (targetDistance < 0.1f) targetDistance = distance;
            distance = targetDistance;

            // Extract initial yaw & pitch from current transform
            Vector3 angles = transform.eulerAngles;
            targetPitch = angles.x;
            targetYaw = angles.y;

            currentPitch = targetPitch;
            currentYaw = targetYaw;
        }

        private Vector3 GetPivotPoint()
        {
            if (targetTransform != null)
            {
                return targetTransform.position;
            }
            return targetCenter;
        }

        private void Update()
        {
            HandleInput();
        }

        private void LateUpdate()
        {
            if (lockControls) return;

            // Smoothly interpolate yaw, pitch, and zoom distance
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, smoothTime);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, smoothTime);

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 targetPos = GetPivotPoint() - (rotation * Vector3.forward * distance);

            transform.rotation = rotation;
            transform.position = targetPos;
        }

        private void HandleInput()
        {
            if (lockControls) return;

            float deltaYaw = 0f;
            float deltaPitch = 0f;
            float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null)
            {
                // Right click or middle click drag to orbit
                if (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
                {
                    isDragging = true;
                }
                if (mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame)
                {
                    isDragging = false;
                }

                if (isDragging)
                {
                    Vector2 mouseDelta = mouse.delta.ReadValue();
                    deltaYaw = mouseDelta.x * (mouseSensitivity * 0.1f);
                    deltaPitch = -mouseDelta.y * (mouseSensitivity * 0.1f);
                }

                // Scroll wheel to zoom
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    scroll = Mathf.Sign(scrollY) * zoomSpeed;
                }
            }

            if (keyboard != null)
            {
                // A/D or Left/Right arrows to rotate horizontally
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    deltaYaw -= keyboardSpeed * Time.deltaTime;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    deltaYaw += keyboardSpeed * Time.deltaTime;
                }

                // W/S or Up/Down arrows to rotate pitch
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    deltaPitch -= keyboardSpeed * 0.5f * Time.deltaTime;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    deltaPitch += keyboardSpeed * 0.5f * Time.deltaTime;
                }

                // Q/E to zoom
                if (keyboard.qKey.isPressed)
                {
                    targetDistance = Mathf.Clamp(targetDistance + zoomSpeed * Time.deltaTime * 3f, minDistance, maxDistance);
                }
                if (keyboard.eKey.isPressed)
                {
                    targetDistance = Mathf.Clamp(targetDistance - zoomSpeed * Time.deltaTime * 3f, minDistance, maxDistance);
                }
            }
#endif

            targetYaw += deltaYaw;
            targetPitch = Mathf.Clamp(targetPitch + deltaPitch, minPitch, maxPitch);

            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll, minDistance, maxDistance);
            }
        }

        private void OnGUI()
        {
            if (!showControlsHint) return;

            // Small discrete hint box in top right
            int width = 230;
            int height = 75;
            Rect rect = new Rect(Screen.width - width - 15, 15, width, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("<b>Camera Orbit Controls</b>");
            GUILayout.Label("<size=11>• Right Click + Drag to rotate\n• A / D keys to orbit pond\n• Mouse Scroll to zoom</size>");
            GUILayout.EndArea();
        }

        public void SetTargetCenter(Vector3 newCenter)
        {
            targetCenter = newCenter;
        }

        public bool LockControls
        {
            get => lockControls;
            set => lockControls = value;
        }
    }
}
