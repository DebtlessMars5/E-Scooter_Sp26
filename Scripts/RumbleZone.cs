using UnityEngine;

public class RumbleZone : MonoBehaviour
{
    public SigmaMotionController activeScooter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        var scooter = other.GetComponentInParent<SigmaMotionController>();

        if (scooter != null)
        {
            activeScooter = scooter;
            activeScooter.SendMotionData();
        }
    }


}