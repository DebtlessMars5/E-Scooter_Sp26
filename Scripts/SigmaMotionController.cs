using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;

/// <summary>
/// Sigma Motion System Controller for Electric Scooter
/// Attach this to the ElectricScooter GameObject
/// </summary>
public class SigmaMotionController : MonoBehaviour
{
    [Header("Connection Settings")]
    [Tooltip("IP address of Sigma system")]
    public string ipAddress = "127.0.0.1";

    [Tooltip("UDP port for Sigma system")]
    public int port = 10001;

    [Tooltip("Update rate in Hz (60-300 recommended)")]
    public int updateRate = 120;

    [Header("Motion Settings")]
    [Tooltip("Current pitch angle to send to motion platform")]
    [Range(-2f, 2f)]
    public float currentPitch = 0f;

    [Tooltip("Current roll angle to send to motion platform")]
    [Range(-2f, 2f)]
    public float currentRoll = 0f;

    [Header("Rumble Settings")]
    [Tooltip("Master rumble enable/disable")]
    public bool rumbleEnabled = true;

    [Tooltip("Low rumble frequency (Hz)")]
    public float lowRumbleFrequency = 10f;

    [Tooltip("Low rumble intensity (0-1)")]
    [Range(0f, 1f)]
    public float lowRumbleIntensity = 0.5f;

    [Tooltip("High rumble frequency (Hz)")]
    public float highRumbleFrequency = 30f;

    [Tooltip("High rumble intensity (0-1)")]
    [Range(0f, 1f)]
    public float highRumbleIntensity = 0.8f;

    [Header("Collision Settings")]
    [Tooltip("Collision impact strength")]
    public float collisionImpactStrength = 8f;

    [Tooltip("Collision impact duration (seconds)")]
    public float collisionImpactDuration = 0.3f;

    [Header("Performance")]
    [Tooltip("Send data immediately on significant changes (reduces latency)")]
    public bool immediateMode = true;

    [Tooltip("Minimum change in degrees to trigger immediate send")]
    public float immediateThreshold = 0.1f;

    [Tooltip("Predict future position to compensate for platform lag (seconds)")]
    [Range(0f, 0.2f)]
    public float predictionTime = 0.05f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Private variables
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private Rigidbody rb;
    private float updateInterval;
    private float lastUpdateTime;

    // Motion state
    private float currentSpeed = 0f;
    private bool rumbleLowActive = false;
    private bool rumbleHighActive = false;

    // Previous state for immediate mode
    private float lastPitch = 0f;
    private float lastRoll = 0f;

    // Angular velocity for prediction
    private float pitchVelocity = 0f;
    private float rollVelocity = 0f;

    // Rumble state
    private float lowRumblePhase = 0f;
    private float highRumblePhase = 0f;

    // Collision state
    private bool isColliding = false;
    private float collisionTimer = 0f;
    private float collisionForce = 0f;

    private void Start()
    {
        // Get Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Initialize UDP connection
        try
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            updateInterval = 1f / updateRate;

            Debug.Log($"[Sigma] Connected to {ipAddress}:{port} at {updateRate} Hz");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Sigma] Failed to initialize UDP: {e.Message}");
        }
    }

    private void FixedUpdate()
    {
        // Update motion data in FixedUpdate for better physics sync
        UpdateMotionData();

        // Check for immediate send on significant changes
        if (immediateMode)
        {
            float pitchChange = Mathf.Abs(currentPitch - lastPitch);
            float rollChange = Mathf.Abs(currentRoll - lastRoll);

            if (pitchChange > immediateThreshold || rollChange > immediateThreshold)
            {
                SendMotionData();
                lastPitch = currentPitch;
                lastRoll = currentRoll;
                return; // Don't send again in Update
            }
        }
    }

    private void Update()
    {
        // Send at regular intervals (even if no change)
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            SendMotionData();
            lastUpdateTime = Time.time;
            lastPitch = currentPitch;
            lastRoll = currentRoll;
        }

        // Update rumble effects
        if (rumbleLowActive)
        {
            lowRumblePhase += Time.deltaTime * lowRumbleFrequency * 2f * Mathf.PI;
        }

        if (rumbleHighActive)
        {
            highRumblePhase += Time.deltaTime * highRumbleFrequency * 2f * Mathf.PI;
        }

        // Update collision timer
        if (isColliding)
        {
            collisionTimer -= Time.deltaTime;
            if (collisionTimer <= 0f)
            {
                isColliding = false;
                collisionForce = 0f;
            }
        }
    }

    private void UpdateMotionData()
    {
        // Calculate angular velocity (for prediction)
        pitchVelocity = (currentPitch - lastPitch) / Time.fixedDeltaTime;
        rollVelocity = (currentRoll - lastRoll) / Time.fixedDeltaTime;

        // Update speed using linearVelocity (Unity 2023.1+)
        if (rb != null)
        {
            currentSpeed = rb.linearVelocity.magnitude;
        }
    }

    public void SendMotionData()
    {
        if (udpClient == null || endPoint == null) return;

        try
        {
            // Apply prediction to pitch/roll if enabled
            float sendPitch = currentPitch;
            float sendRoll = currentRoll;

            if (predictionTime > 0f)
            {
                sendPitch = Mathf.Clamp(currentPitch + pitchVelocity * predictionTime, -2f, 2f);
                sendRoll = Mathf.Clamp(currentRoll + rollVelocity * predictionTime, -2f, 2f);
            }

            // Calculate effects
            float heaveAccel = CalculateHeave();
            float surgeAccel = CalculateSurge();

            // Build packet according to Sigma API specification
            byte[] packet = new byte[36];
            int offset = 0;

            // Byte 0-3: Motion data indicator (0)
            WriteUInt32(packet, ref offset, 0);

            // Byte 4-7: Vehicle speed (m/s)
            WriteFloat(packet, ref offset, currentSpeed);

            // Byte 8-11: Surge acceleration (m/s²)
            WriteFloat(packet, ref offset, surgeAccel);

            // Byte 12-15: Sway acceleration (m/s²)
            WriteFloat(packet, ref offset, 0f);

            // Byte 16-19: Heave acceleration (m/s²) - used for rumble
            WriteFloat(packet, ref offset, heaveAccel);

            // Byte 20-23: Pitch position (degrees)
            WriteFloat(packet, ref offset, sendPitch);

            // Byte 24-27: Roll position (degrees)
            WriteFloat(packet, ref offset, sendRoll);

            // Byte 28-31: Gear (not used for scooter)
            WriteUInt32(packet, ref offset, 0);

            // Byte 32-35: Engine RPM (not used for scooter)
            WriteFloat(packet, ref offset, 0f);

            // Send packet
            udpClient.Send(packet, packet.Length, endPoint);

            // Debug output
            if (showDebugInfo && Time.frameCount % updateRate == 0)
            {
                Debug.Log($"[Sigma] Pitch: {sendPitch:F2}° | Roll: {sendRoll:F2}° | " +
                          $"Heave: {heaveAccel:F2} | Surge: {surgeAccel:F2} | " +
                          $"Rumble: {(rumbleEnabled ? "ON" : "OFF")} | " +
                          $"L: {rumbleLowActive} | H: {rumbleHighActive}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Sigma] Send error: {e.Message}");
        }
    }

    private float CalculateHeave()
    {
        float heave = 0f;

        // Only apply rumble if master switch is enabled
        if (rumbleEnabled)
        {
            // Add low rumble (slower, larger amplitude)
            if (rumbleLowActive)
            {
                heave += Mathf.Sin(lowRumblePhase) * lowRumbleIntensity * 3f;
            }

            // Add high rumble (faster, smaller amplitude)
            if (rumbleHighActive)
            {
                heave += Mathf.Sin(highRumblePhase) * highRumbleIntensity * 2f;
            }
        }

        return heave;
    }

    private float CalculateSurge()
    {
        float surge = 0f;

        // Add collision impact
        if (isColliding)
        {
            // Decay over time
            float t = 1f - (collisionTimer / collisionImpactDuration);
            surge = collisionForce * (1f - t * t); // Quadratic decay
        }

        return surge;
    }

    // Write methods for little-endian encoding
    private void WriteFloat(byte[] buffer, ref int offset, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        Array.Copy(bytes, 0, buffer, offset, 4);
        offset += 4;
    }

    private void WriteUInt32(byte[] buffer, ref int offset, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        Array.Copy(bytes, 0, buffer, offset, 4);
        offset += 4;
    }

    // ====== PUBLIC API ======

    /// <summary>
    /// Enable or disable all rumble effects
    /// </summary>
    public void SetRumbleEnabled(bool enabled)
    {
        rumbleEnabled = enabled;

        if (!enabled)
        {
            // Stop rumble phases when disabled
            lowRumblePhase = 0f;
            highRumblePhase = 0f;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[Sigma] Rumble Master Switch: {(enabled ? "ON" : "OFF")}");
        }
    }

    /// <summary>
    /// Enable or disable low frequency rumble
    /// </summary>
    public void SetRumbleLow(bool active)
    {
        rumbleLowActive = active;
        if (!active)
        {
            lowRumblePhase = 0f;
        }

        // Send immediately for instant feedback
        if (immediateMode)
        {
            SendMotionData();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[Sigma] Low Rumble: {(active ? "ON" : "OFF")}");
        }
    }

    /// <summary>
    /// Enable or disable high frequency rumble
    /// </summary>
    public void SetRumbleHigh(bool active)
    {
        rumbleHighActive = active;
        if (!active)
        {
            highRumblePhase = 0f;
        }

        // Send immediately for instant feedback
        if (immediateMode)
        {
            SendMotionData();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[Sigma] High Rumble: {(active ? "ON" : "OFF")}");
        }
    }

    /// <summary>
    /// Trigger a collision impact effect
    /// </summary>
    public void TriggerCollision()
    {
        TriggerCollision(collisionImpactStrength);
    }

    /// <summary>
    /// Trigger a collision impact with custom strength
    /// </summary>
    public void TriggerCollision(float strength)
    {
        isColliding = true;
        collisionTimer = collisionImpactDuration;
        collisionForce = strength;

        // Send immediately for instant impact
        SendMotionData();

        if (showDebugInfo)
        {
            Debug.Log($"[Sigma] Collision triggered! Strength: {strength:F1}");
        }
    }

    /// <summary>
    /// Set pitch angle (motion platform angle, not scooter GameObject rotation)
    /// </summary>
    public void SetPitch(float angle)
    {
        currentPitch = Mathf.Clamp(angle, -2f, 2f);
    }

    /// <summary>
    /// Set roll angle (motion platform angle, not scooter GameObject rotation)
    /// </summary>
    public void SetRoll(float angle)
    {
        currentRoll = Mathf.Clamp(angle, -2f, 2f);
    }

    /// <summary>
    /// Get current pitch angle being sent
    /// </summary>
    public float GetCurrentPitch()
    {
        return currentPitch;
    }

    /// <summary>
    /// Get current roll angle being sent
    /// </summary>
    public float GetCurrentRoll()
    {
        return currentRoll;
    }

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            Debug.Log("[Sigma] Connection closed");
        }
    }

    // Optional: Detect collisions automatically
    private void OnCollisionEnter(Collision collision)
    {
        // Automatically trigger collision effect on impact
        float impactMagnitude = collision.relativeVelocity.magnitude;
        if (impactMagnitude > 2f) // Threshold for significant collision
        {
            float strength = Mathf.Clamp(impactMagnitude * 2f, 0f, 15f);
            TriggerCollision(strength);
        }
    }
}