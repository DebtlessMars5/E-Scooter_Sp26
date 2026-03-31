using UnityEngine;

public class ScooterSlopeDetector : MonoBehaviour
{
    public Transform scooterBody;  // assign the T-body or root of the scooter
    public float rayLength = 5f;
    public LayerMask groundLayer;

    // Outputs
    public float slopeAngle;
    public float slopePitch;
    public float slopeRoll;

    void Update()
    {
        Vector3 rayOrigin = scooterBody.position + Vector3.up * 0.2f; // small offset so ray doesn't hit scooter

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            Vector3 groundNormal = hit.normal;

            // overall slope
            slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            // relative slope (pitch/roll)
            Vector3 forward = scooterBody.forward;
            Vector3 right = scooterBody.right;

            slopePitch = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(forward, Vector3.up),
                Vector3.ProjectOnPlane(forward, groundNormal),
                right
            );

            slopeRoll = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(right, Vector3.up),
                Vector3.ProjectOnPlane(right, groundNormal),
                forward
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (scooterBody == null) return;

        Gizmos.color = Color.red;
        Vector3 origin = scooterBody.position + Vector3.up * 0.2f;
        Gizmos.DrawRay(origin, Vector3.down * rayLength);
    }
}
