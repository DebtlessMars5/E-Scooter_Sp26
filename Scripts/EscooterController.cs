using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EscooterController : MonoBehaviour
{
    [Header("Scooter Controls")]
    [Range(-45, 45)] public float Scooter_Yaw = 0f;   // steering input (deg)
    public float Scooter_Speed = 0f;                  // m/s forward speed
    public float Scooter_Roll = 0f;                   // (reserved for lean later)

    [Header("Scene References")]
    public Transform scooterBody;       // ES_Body (has collider, part of this RB)
    public Transform steerPivot;        // ES_T_Pivot (has hinge + front RB)
    public HingeJoint steerHinge;       // assign hinge joint here
    public Transform visualRoot;  // assign ScooterVisualRoot

    [Header("Steering Physics")]
    public float wheelbase = 0.8f;
    public float steeringResponse = 1.0f; // overall turn sensitivity multiplier

    private Rigidbody rb;
    private ArduinoReader arduinoReader;

    void Start()
    {
        arduinoReader = GameObject.Find("ArduinoReader").GetComponent<ArduinoReader>();
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            // Prevent tipping or physics roll
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationZ;
        }

        if (!steerHinge && steerPivot)
            steerHinge = steerPivot.GetComponent<HingeJoint>();

        if (steerHinge)
        {
            steerHinge.useLimits = true;
            JointLimits lim = steerHinge.limits;
            lim.min = -45f;
            lim.max = 45f;
            steerHinge.limits = lim;
        }
    }

    void FixedUpdate()
    {
        Scooter_Speed = arduinoReader.speed / 2.237f;
        //Scooter_Speed = arduinoReader.speed;
        UpdateSteering();
        ApplyForwardMotion();
        ApplyRoll();
    }

    // --- STEERING via hinge joint ---------------------------------
    void UpdateSteering()
    {
        if (!steerHinge) return;

        // Clamp desired yaw
        Scooter_Yaw = Mathf.Clamp(Scooter_Yaw, -45f, 45f);

        // Set hinge target angle
        JointSpring spring = steerHinge.spring;
        spring.spring = 800f;         // stiffness
        spring.damper = 50f;          // resistance
        spring.targetPosition = Scooter_Yaw;
        steerHinge.spring = spring;
        steerHinge.useSpring = true;
    }

    // --- FORWARD MOVEMENT -----------------------------------------
    void ApplyForwardMotion()
    {
        if (!rb) return;

        // --- Forward direction (front of scooter) ---
        // Forward direction (+X for this model)
        Vector3 fwd = scooterBody.transform.right;


        // --- Compute turning curvature from handlebar yaw ---
        // Convert yaw (deg) to radians for trig
        float yawRad = Mathf.Deg2Rad * Scooter_Yaw;

        // Approximate wheelbase (distance between front and rear wheel)
        float wheelbase = 0.8f; // meters – tune to your scooter model

        // Turning radius formula: R = wheelbase / tan(steeringAngle)
        float turnRadius = (Mathf.Abs(Scooter_Yaw) < 1e-3f) ? Mathf.Infinity : wheelbase / Mathf.Tan(yawRad);

        // Angular velocity around Y (radians/sec)
        float angularVel = (Scooter_Speed / turnRadius) * steeringResponse * Mathf.Rad2Deg * Time.fixedDeltaTime;

        // Apply correct rotation direction for X-forward model
        float headingChange = angularVel;

        // Apply gradual body rotation
        Quaternion dRot = Quaternion.Euler(0f, headingChange, 0f);
        rb.MoveRotation(rb.rotation * dRot);

        // --- Forward velocity update ---
        Vector3 targetVel = fwd * Scooter_Speed;
        Vector3 velChange = targetVel - rb.linearVelocity;
        velChange.y = 0f;
        rb.AddForce(velChange, ForceMode.VelocityChange);
    }

    void ApplyRoll()
    {
        if (!visualRoot) return;

        // Clamp roll input
        Scooter_Roll = Mathf.Clamp(Scooter_Roll, -30f, 30f);

        // Desired local rotation only around X axis
        Quaternion targetRot = Quaternion.Euler(Scooter_Roll, 0f, 0f);

        // Directly set or smoothly interpolate (visual only)
        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            targetRot,
            1f - Mathf.Exp(-10f * Time.fixedDeltaTime)
        );
    }



}
