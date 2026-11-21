using envcontrol.module;

namespace envcontrol
{
    public partial class Form1 : Form
    {
        private readonly SerialPortManager portManager;
        private readonly SerialCommunication serial;

        private bool ledStatus;

        public Form1()
        {
            InitializeComponent();

            portManager = new SerialPortManager();
            serial = new SerialCommunication();

            ledStatus = false;

            // Eventos da comunicação
            serial.DataReceived += Serial_DataReceived;
            serial.ErrorOccurred += Serial_ErrorOccurred;

            CarregarPortasCom();

            AtualizarBotaoLed(false);
        }

        private void CarregarPortasCom()
        {
            portList.Items.Clear();

            try
            {
                string[] ports = portManager.GetAvailablePorts();

                portList.Items.AddRange(ports);

                if (ports.Length == 0)
                    AppendLog("Nenhuma porta COM encontrada.");
            }
            catch (Exception ex)
            {
                AppendLog("Erro: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string? port = portList.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.Show("Seleciona uma porta COM.");
                return;
            }

            try
            {
                serial.Connect(port);

                AppendLog($"Conectado em {port}.");

                serial.Send("STATUS");

                btn_act.Enabled = true;
                button2.Enabled = true;
                btn_ping.Enabled = true;
            }
            catch (Exception ex)
            {
                AppendLog("Erro de conexão: " + ex.Message);

                MessageBox.Show(
                    ex.Message,
                    "Erro de conexão",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void Serial_DataReceived(string message)
        {
            message = message.Trim();

            string[] parts = message.Split(':', 2);

            string type = parts[0];
            string content = parts.Length > 1 ? parts[1] : "";

            if (type == "INFO")
            {
                if (content == "LED ON")
                    AtualizarBotaoLed(true);

                else if (content == "LED OFF")
                    AtualizarBotaoLed(false);
            }

            AppendLog("ESP32: " + message);

            ProcessarEstadoLed(message);
        }

        private void Serial_ErrorOccurred(string error)
        {
            AppendLog("Erro na comunicação: " + error);
        }

        private void ProcessarEstadoLed(string message)
        {
            string upper = message.ToUpperInvariant();

            if (upper.Contains("STATUS:ON") ||
                upper == "LED ON" ||
                upper.EndsWith("LED: ON"))
            {
                AtualizarBotaoLed(true);
            }
            else if (upper.Contains("STATUS:OFF") ||
                     upper == "LED OFF" ||
                     upper.EndsWith("LED: OFF"))
            {
                AtualizarBotaoLed(false);
            }
        }

        private void AtualizarBotaoLed(bool ligado)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                    AtualizarBotaoLed(ligado)));
                return;
            }

            ledStatus = ligado;

            btn_act.Text = ligado ? "OFF" : "ON";

            btn_act.BackColor = ligado
                ? Color.Red
                : Color.Green;
        }

        private void AppendLog(string texto)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.BeginInvoke(new Action(() =>
                    AppendLog(texto)));
                return;
            }

            txtLogs.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] {texto}{Environment.NewLine}"
            );
        }

        private void btn_act_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serial.IsConnected)
                {
                    MessageBox.Show("Conecta-te primeiro.");
                    return;
                }

                if (!ledStatus)
                    serial.Send("ON");
                else
                    serial.Send("OFF");

                // O estado será atualizado quando chegar a resposta do ESP32.
            }
            catch (Exception ex)
            {
                AppendLog("Erro: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                serial.Send("PING");
            }
            catch (Exception ex)
            {
                AppendLog("Erro: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            serial.Dispose();

            base.OnFormClosing(e);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            BluetoothConfig dialog = new BluetoothConfig(serial);
            dialog.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WifiConfig dialog = new WifiConfig(serial);
            dialog.ShowDialog();
        }
    }
}
