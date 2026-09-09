using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerRig : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] float moveSpeed = 2.6f;
        [SerializeField] float lookSensitivity = 0.08f;
        [SerializeField] float gravity = 12f;

        CharacterController controller;
        float pitch;
        float fallSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            pitch = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (Keyboard.current == null || Mouse.current == null)
            {
                return;
            }

            Look();
            Move();
        }

        void Look()
        {
            var delta = Mouse.current.delta.ReadValue() * lookSensitivity;

            transform.Rotate(Vector3.up, delta.x);

            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);

            if (head != null)
            {
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        void Move()
        {
            var keyboard = Keyboard.current;
            var input = Vector2.zero;

            if (keyboard[Key.W].isPressed) input.y += 1f;
            if (keyboard[Key.S].isPressed) input.y -= 1f;
            if (keyboard[Key.D].isPressed) input.x += 1f;
            if (keyboard[Key.A].isPressed) input.x -= 1f;

            var move = transform.right * input.x + transform.forward * input.y;

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            fallSpeed = controller.isGrounded ? -1f : fallSpeed - gravity * Time.deltaTime;

            controller.Move((move * moveSpeed + Vector3.up * fallSpeed) * Time.deltaTime);
        }
    }
}
