using System;
using System.IO.Ports;
using System.Threading.Tasks;

string comPort = "COM4"; // CHANGE if needed
int baudRate = 9600;

Console.WriteLine("Ports found: " + string.Join(", ", SerialPort.GetPortNames()));
Console.WriteLine($"Opening {comPort} @ {baudRate}");

using var port = new SerialPort(comPort, baudRate)
{
    NewLine = "\n",
    ReadTimeout = 2000,
    DtrEnable = true,
    RtsEnable = true
};

try
{
    port.Open();
    Console.WriteLine("Serial port opened OK.");
    port.DiscardInBuffer();
}
catch (Exception ex)
{
    Console.WriteLine("ERROR opening serial port: " + ex.Message);
    Console.WriteLine("Close Serial Monitor and verify COM port.");
    Console.ReadLine();
    return;
}

// The board may reset when the port opens; wait a moment
await Task.Delay(2500);

while (true)
{
    try
    {
        string line = port.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line))
            continue;

        Console.WriteLine("RAW: " + line);
    }
    catch (TimeoutException)
    {
        Console.WriteLine("No serial data yet...");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Read error: " + ex.Message);
        await Task.Delay(1000);
    }
}