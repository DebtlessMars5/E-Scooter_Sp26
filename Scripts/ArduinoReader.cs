using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI; // If you plan to display data in the UI

public class ArduinoReader : MonoBehaviour
{
    private SerialPort stream;
    public string portName = "COM3"; // !!! CHANGE THIS to your Arduino's port name !!!
    public int baudRate = 115200; // Must match the rate set in the Arduino code

    public float speed = 0f;

    // Use this for initialization
    void Start()
    {
        stream = new SerialPort(portName, baudRate);
        stream.ReadTimeout = 50; // Set a read timeout
        try
        {
            stream.Open(); // Open the serial port connection
            Debug.Log("Serial port opened successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error opening serial port: " + e.Message);
        }
    }

    // Update is called once per frame
    void Update()
    {


        if (stream != null && stream.IsOpen)
        {
            try
            {
                // Check if there is data available to read
                if (stream.BytesToRead > 0)
                {
                    // Read the line of data

                    // You can now parse the 'value' and use it to control objects in Unity
                    string value = stream.ReadLine();
                    speed = ParseSpeed(value);
                    //Debug.Log("Data received from Arduino: " + value);
                }
            }
            catch (System.TimeoutException) { } // Handle timeout if no data is available
            catch (System.Exception e)
            {
                Debug.LogError("Error reading from serial port: " + e.Message);
            }
        }

    }

    float ParseSpeed(string data)
    {
        // Split by comma to get each key=value pair
        string[] pairs = data.Split(',');

        foreach (string pair in pairs)
        {
            string trimmed = pair.Trim();

            // Look for the Speed(m/s) key
            if (trimmed.StartsWith("Speed(m/s)="))
            {
                string valueStr = trimmed.Substring("Speed(m/s)=".Length);
                if (float.TryParse(valueStr, out float result))
                    return result;
            }
        }
        return 0f;
    }

    void OnApplicationQuit()
    {
        if (stream != null && stream.IsOpen)
        {
            stream.Close(); // Close the port when the Unity application quits
            Debug.Log("Serial port closed.");
        }
    }

    public float GetSpeed() => speed;
}

