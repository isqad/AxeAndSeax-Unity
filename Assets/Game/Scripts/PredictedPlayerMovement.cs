using PurrNet.Prediction;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts
{
    public class PredictedPlayerMovement : PredictedIdentity<PredictedPlayerMovement.Input, PredictedPlayerMovement.State>
    {
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private GameObject cinemachineCameraTarget;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float cameraMinPitch = -60f;
        [SerializeField] private float cameraMaxPitch = 60f;
        [SerializeField] private float maxSpeed = 4f;
        [SerializeField] private float backwardSpeedMul = 0.6f;
        [SerializeField] private float strafeSpeedMul = 0.8f;
        [SerializeField] private float gravity = -16f;
        [SerializeField] private float groundedPullDown = -2f;
        
        private CharacterController cc;
        private InputActionMap playerActionMap;
        private InputAction lookAction;
        private InputAction moveAction;

        public struct Input : IPredictedData
        {
            public Vector2 move;
            public Vector2 lookDelta;
            public void Dispose() {}
        }

        public struct State : IPredictedData<State>
        {
            public float yaw;
            public float pitch;
            public float verticalVelocity;
            public bool isGrounded;
            public void Dispose() {}
        }

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        protected override void LateAwake()
        {
            if (!isOwner)
            {
                cinemachineCamera.enabled = false;
                return;
            }

            cinemachineCamera.Priority = 1;
            cinemachineCamera.enabled = true;
            
            playerActionMap = InputSystem.actions.FindActionMap("Player");
            lookAction = playerActionMap.FindAction("Look");
            moveAction = playerActionMap.FindAction("Move");
            playerActionMap.Enable();
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void Destroyed()
        {
            if (isOwner)
                playerActionMap?.Disable();
        }

        protected override State GetInitialState()
        {
            return new State
            {
                yaw = transform.eulerAngles.y,
                pitch = 0f,
                verticalVelocity = 0f,
                isGrounded = true
            };
        }

        protected override void UpdateInput(ref Input input)
        {
            if (!isOwner) return;
            var look = lookAction.ReadValue<Vector2>();
            input.lookDelta += look;
        }

        protected override void GetFinalInput(ref Input input)
        {
            if (!isOwner) return;
            input.move = moveAction.ReadValue<Vector2>();
        }

        protected override void SanitizeInput(ref Input input)
        {
            input.move = Vector2.ClampMagnitude(input.move, 1f);
            input.lookDelta = new Vector2(
                Mathf.Clamp(input.lookDelta.x, -100f, 100f),
                Mathf.Clamp(input.lookDelta.y, -100f, 100f));
        }

        protected override void Simulate(Input input, ref State state, float delta)
        {
            state.yaw += input.lookDelta.x * mouseSensitivity;
            state.pitch = Mathf.Clamp(
                state.pitch - input.lookDelta.y * mouseSensitivity,
                cameraMinPitch, cameraMaxPitch);

            var yawRot = Quaternion.Euler(0f, state.yaw, 0f);
            var forward = yawRot * Vector3.forward;
            var right = yawRot * Vector3.right;

            float speedMul = 1f;
            if (input.move.y < -0.01f)
                speedMul = backwardSpeedMul;
            else if (Mathf.Abs(input.move.x) > 0.01f && Mathf.Abs(input.move.y) < 0.01f)
                speedMul = strafeSpeedMul;

            var moveDir = forward * input.move.y + right * input.move.x;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            if (state.isGrounded && state.verticalVelocity < 0f)
                state.verticalVelocity = groundedPullDown;
            else
                state.verticalVelocity += gravity * delta;

            var totalMove = moveDir * (maxSpeed * speedMul * delta)
                          + Vector3.up * (state.verticalVelocity * delta);

            cc.Move(totalMove);
            state.isGrounded = IsGrounded();

            transform.rotation = Quaternion.Euler(0f, state.yaw, 0f);
        }

        protected override void LateUpdateView(State viewState, State? verified)
        {
            if (!isOwner) return;
            
            if (cinemachineCameraTarget)
                cinemachineCameraTarget.transform.localRotation =
                    Quaternion.Euler(viewState.pitch, 0f, 0f);
        }

        protected override void ModifyExtrapolatedInput(ref Input input)
        {
            input.move *= 0.6f;
            input.lookDelta = Vector2.zero;
            if (Mathf.Abs(input.move.x) < 0.15f) input.move.x = 0f;
            if (Mathf.Abs(input.move.y) < 0.15f) input.move.y = 0f;
        }
        
        private bool IsGrounded()
        {
            // controller.isGrounded is weird, dont use it
            return Physics.SphereCast(
                transform.position - Vector3.up * (cc.height / 2 - 0.1f),
                cc.radius - 0.05f,
                Vector3.down,
                out _,
                0.2f
            );
        }
    }
}
