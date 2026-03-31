using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class ControllerCubeFollower : MonoBehaviour
{
    [Header("Which controller to follow")]
    public InputDeviceCharacteristics controllerHand = InputDeviceCharacteristics.Right;

    private InputDevice controller;

    void Start()
    {
        TryInitializeController();
    }

    void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerHand | InputDeviceCharacteristics.Controller, devices);

        if (devices.Count > 0)
        {
            controller = devices[0];
            Debug.Log($"Linked to controller: {controller.name}");
        }
        else
        {
            Debug.LogWarning("No controller found yet...");
        }
    }

    void Update()
    {
        // If controller got lost (unplug, reconnect), try again
        if (!controller.isValid)
        {
            TryInitializeController();
            return;
        }

        // Get position and rotation
        if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            transform.localPosition = pos;   // follow position

        if (controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            transform.localRotation = rot;   // follow rotation
    }
}
