using System;
using System.IO.Ports;
using System.Text;

namespace envcontrol.module
{
    public class SerialCommunication : IDisposable
    {
        private SerialPort? serialPort;
        private readonly StringBuilder rxBuffer = new();
        private readonly object rxLock = new();

        public bool IsConnected => serialPort != null && serialPort.IsOpen;

        public event Action<string>? DataReceived;
        public event Action<string>? ErrorOccurred;

        public void Connect(string portName, int baudRate = 115200)
        {
            Disconnect();

            try
            {
                lock (rxLock) rxBuffer.Clear();

                serialPort = new SerialPort(portName, baudRate)
                {
                    NewLine = "\n",
                    DtrEnable = false,   // evita reset/bootloader do ESP32
                    RtsEnable = false,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                serialPort.DataReceived += OnDataReceived;
                serialPort.Open();
                serialPort.DiscardInBuffer();
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

            serialPort!.Write(message + "\n");
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var port = serialPort;
                if (port == null || !port.IsOpen)
                    return;

                string chunk = port.ReadExisting();

                lock (rxLock)
                {
                    rxBuffer.Append(chunk);

                    while (true)
                    {
                        string text = rxBuffer.ToString();
                        int idx = text.IndexOf('\n');
                        if (idx < 0) break;

                        string line = text.Substring(0, idx).Trim();
                        rxBuffer.Remove(0, idx + 1);

                        if (!string.IsNullOrWhiteSpace(line))
                            DataReceived?.Invoke(line);
                    }

                    // proteção contra lixo sem quebra de linha
                    if (rxBuffer.Length > 4096)
                        rxBuffer.Clear();
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