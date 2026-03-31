using UnityEngine;
using UnityEngine.XR;

public class ControllerInputs : MonoBehaviour
{
    [Header("References")]
    public EscooterController scooter;    // Drag your scooter controller here
    public InputDeviceCharacteristics controllerHand =
        InputDeviceCharacteristics.Right; // or Left, pick in inspector

    [Header("Settings")]
    public float yawSensitivity = 1f;
    public bool invertYaw = false;
    public float maxSpeed = 5f;           // ← adjustable in Inspector
    public float speedSmoothing = 3f;     // ← lower = slower response
    public float joystickDeadZone = 0.1f; // ← ignore tiny noise

    private InputDevice controller;
    private Quaternion initialControllerRot;
    private float targetSpeed = 0f;

    void Start()
    {
        TryInitializeController();
    }

    void TryInitializeController()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerHand, devices);

        if (devices.Count > 0)
        {
            controller = devices[0];
            controller.TryGetFeatureValue(CommonUsages.deviceRotation, out initialControllerRot);
            Debug.Log($"Linked to {controller.name}");
        }
        else
        {
            Debug.LogWarning("No XR controller found for yaw input.");
        }
    }

    void Update()
    {
        if (!controller.isValid)
        {
            TryInitializeController();
            return;
        }

        if (scooter == null) return;

        // ---- Handle Yaw ----
        if (controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentRot))
        {
            Quaternion delta = currentRot * Quaternion.Inverse(initialControllerRot);
            Vector3 euler = delta.eulerAngles;
            if (euler.y > 180f) euler.y -= 360f;

            float yaw = (invertYaw ? -euler.y : euler.y) * yawSensitivity;
            scooter.Scooter_Yaw = Mathf.Clamp(yaw, -45f, 45f);
        }

        // ---- Handle Speed ----
        if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
        {
            float inputY = Mathf.Abs(stick.y) < joystickDeadZone ? 0f : stick.y;
            targetSpeed = inputY * maxSpeed;

            // Smooth blend toward target
            scooter.Scooter_Speed = Mathf.Lerp(
                scooter.Scooter_Speed,
                targetSpeed,
                Time.deltaTime * speedSmoothing
            );
        }
    }
}
