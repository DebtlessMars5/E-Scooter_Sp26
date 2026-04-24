using UnityEngine;

public class AccelerationLimiter : MonoBehaviour
{
    public float max_acceleration = 10f ; // m/s^2

    private float last_speed = 0f;
    private float current_speed = 0f;

    private float desired_acceleration = 0f;
    private float acceleration;

    public float output_speed = 0f;

    private ArduinoReader arduinoReader;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        arduinoReader = GameObject.Find("ArduinoReader").GetComponent<ArduinoReader>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        acceleration = max_acceleration * Time.fixedDeltaTime;
        current_speed = arduinoReader.speed;

        desired_acceleration = current_speed - last_speed;

        if (Mathf.Abs(desired_acceleration) > acceleration)
        {
            Debug.Log("Acceleration Limited");
            output_speed = Mathf.Sign(desired_acceleration)*acceleration+last_speed;
        }
        else
        {
            output_speed = current_speed;
        }
        
        last_speed = output_speed;
    }
}
