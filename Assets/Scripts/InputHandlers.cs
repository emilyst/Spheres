using System;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using Cursor = UnityEngine.Cursor;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(Camera))]
public class InputHandlers : MonoBehaviour
{
    public Simulation Simulation;

    public bool GrabCursor;

    public Vector2 ScrollInput;
    public float ScrollVelocityX;
    public float ScrollVelocityY;
    public float ScrollSpeed = 3f;
    public float ScrollSmoothTime = 3f;

    public Vector2 LookInput;
    public float LookVelocityX;
    public float LookVelocityY;
    public float LookSpeed = 3f;
    public float LookSmoothTime = 3f;

    public Vector2 MoveInput;
    public float MoveVelocityX;
    public float MoveVelocityY;
    public float MoveSpeed = 33f;
    public float MoveSmoothTime = 3f;

    public float ThrustInput;
    public float ThrustVelocity;
    public float ThrustSpeed = 33f;
    public float ThrustSmoothTime = 3f;

    void OnEnable()
    {
        if (GrabCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    void LateUpdate()
    {
        Move();
        Look();
        Thrust();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        ScrollInput = context.ReadValue<Vector2>();
    }

    public void OnThrust(InputAction.CallbackContext context)
    {
        ThrustInput = context.ReadValue<float>();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Simulation.TogglePaused();
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

    void Move()
    {
        transform.Translate(
            Mathf.SmoothDamp(MoveVelocityX, MoveInput.x * MoveSpeed, ref MoveVelocityX, MoveSmoothTime),
            Mathf.SmoothDamp(MoveVelocityY, MoveInput.y * MoveSpeed, ref MoveVelocityY, MoveSmoothTime),
            0,
            Space.Self
        );
    }

    void Thrust()
    {
        transform.Translate(
            0,
            0,
            Mathf.SmoothDamp(ThrustVelocity, ThrustInput * ThrustSpeed, ref ThrustVelocity, ThrustSmoothTime),
            Space.Self
        );
    }

    void Look()
    {
        transform.Rotate(
            Vector3.up
            * Mathf.SmoothDamp(LookVelocityX, LookInput.x * LookSpeed, ref LookVelocityX, LookSmoothTime)
        );
        transform.Rotate(
            Vector3.left
            * Mathf.SmoothDamp(LookVelocityY, LookInput.y * LookSpeed, ref LookVelocityY, LookSmoothTime)
        );

        transform.Rotate(
            Vector3.up
            * Mathf.SmoothDamp(ScrollVelocityX, ScrollInput.x * ScrollSpeed, ref ScrollVelocityX, ScrollSmoothTime)
        );
        transform.Rotate(
            Vector3.left
            * Mathf.SmoothDamp(ScrollVelocityY, ScrollInput.y * ScrollSpeed, ref ScrollVelocityY, ScrollSmoothTime)
        );
    }
}
