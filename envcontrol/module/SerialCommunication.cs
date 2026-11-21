
using System;
using System.IO.Ports;

namespace envcontrol.module
{
    public class SerialCommunication : IDisposable
    {
        private SerialPort? serialPort;

        public bool IsConnected => serialPort != null && serialPort.IsOpen;

        public event Action<string>? DataReceived;
        public event Action<string>? ErrorOccurred;

        public void Connect(string portName, int baudRate = 115200)
        {
            Disconnect();

            try
            {
                serialPort = new SerialPort(portName, baudRate)
                {
                    NewLine = "\n",
                    DtrEnable = true,
                    RtsEnable = false,
                    ReadTimeout = 1000
                };

                serialPort.DataReceived += OnDataReceived;
                serialPort.Open();
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            if (serialPort == null)
                return;

            serialPort.DataReceived -= OnDataReceived;

            if (serialPort.IsOpen)
                serialPort.Close();

            serialPort.Dispose();
            serialPort = null;
        }

        public void Send(string message)
        {
            if (!IsConnected)
                throw new InvalidOperationException(
                    "Não existe uma conexão série ativa.");

            serialPort!.WriteLine(message);
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (serialPort != null &&
                       serialPort.IsOpen &&
                       serialPort.BytesToRead > 0)
                {
                    string message = serialPort.ReadLine().Trim();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        DataReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
