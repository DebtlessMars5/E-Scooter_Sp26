using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI; // If you plan to display data in the UI

public class ArduinoReader : MonoBehaviour
{
    private SerialPort stream;
    public string portName = "COM8"; // !!! CHANGE THIS to your Arduino's port name !!!
    public int baudRate = 115200; // Must match the rate set in the Arduino code

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
                    string value = stream.ReadLine(); // Read the line of data
                    Debug.Log("Data received from Arduino: " + value);
                    // You can now parse the 'value' and use it to control objects in Unity
                }
            }
            catch (System.TimeoutException) { } // Handle timeout if no data is available
            catch (System.Exception e)
            {
                Debug.LogError("Error reading from serial port: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (stream != null && stream.IsOpen)
        {
            stream.Close(); // Close the port when the Unity application quits
            Debug.Log("Serial port closed.");
        }
    }
}
