using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(Camera))]
public class InputBehaviors : MonoBehaviour
{
    public Simulation simulation;

    public bool grabCursor;

    public Vector2 scrollInput;
    public float scrollVelocityX;
    public float scrollVelocityY;
    public float scrollSpeed = 2f;
    public float scrollSmoothTime = 2f;

    public Vector2 lookInput;
    public float lookVelocityX;
    public float lookVelocityY;
    public float lookSpeed = 2f;
    public float lookSmoothTime = 2f;

    public Vector2 moveInput;
    public float moveVelocityX;
    public float moveVelocityY;
    public float moveSpeed = 222;
    public float moveSmoothTime = 2f;

    public float thrustInput;
    public float thrustVelocity;
    public float thrustSpeed = 222f;
    public float thrustSmoothTime = 2f;

    private void OnEnable()
    {
        if (grabCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void LateUpdate()
    {
        Move();
        Look();
        Thrust();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        scrollInput = context.ReadValue<Vector2>();
    }

    public void OnThrust(InputAction.CallbackContext context)
    {
        thrustInput = context.ReadValue<float>();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            simulation.TogglePaused();
        }
    }

    public void OnQuit(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void Move()
    {
        transform.Translate(
            Mathf.SmoothDamp(
                moveVelocityX,
                moveInput.x * moveSpeed,
                ref moveVelocityX,
                moveSmoothTime
            ),
            Mathf.SmoothDamp(
                moveVelocityY,
                moveInput.y * moveSpeed,
                ref moveVelocityY,
                moveSmoothTime
            ),
            0,
            Space.Self
        );
    }

    private void Thrust()
    {
        transform.Translate(
            0,
            0,
            Mathf.SmoothDamp(
                thrustVelocity,
                thrustInput * thrustSpeed,
                ref thrustVelocity,
                thrustSmoothTime
            ),
            Space.Self
        );
    }

    private void Look()
    {
        transform.Rotate(
            Vector3.up *
            Mathf.SmoothDamp(
                lookVelocityX,
                lookInput.x * lookSpeed,
                ref lookVelocityX,
                lookSmoothTime
            )
        );
        transform.Rotate(
            Vector3.left *
            Mathf.SmoothDamp(
                lookVelocityY,
                lookInput.y * lookSpeed,
                ref lookVelocityY,
                lookSmoothTime
            )
        );

        transform.Rotate(
            Vector3.up *
            Mathf.SmoothDamp(
                scrollVelocityX,
                scrollInput.x * scrollSpeed,
                ref scrollVelocityX,
                scrollSmoothTime
            )
        );
        transform.Rotate(
            Vector3.left *
            Mathf.SmoothDamp(
                scrollVelocityY,
                scrollInput.y * scrollSpeed,
                ref scrollVelocityY,
                scrollSmoothTime
            )
        );
    }
}
