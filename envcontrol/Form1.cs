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

            DefinirControlesConectados(false);
            AtualizarBotaoLed(false);
            CarregarPortasCom();
        }

        private void DefinirControlesConectados(bool conectado)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DefinirControlesConectados(conectado)));
                return;
            }

            btn_act.Enabled = conectado;
            button2.Enabled = conectado;
            button3.Enabled = conectado;
            button4.Enabled = conectado;
            btn_ping.Enabled = conectado;
        }

        private void CarregarPortasCom()
        {
            portList.Items.Clear();

            try
            {
                string[] ports = portManager.GetAvailablePorts();

                if (ports.Length > 0)
                {
                    portList.Items.AddRange(ports);
                    portList.SelectedIndex = 0;
                }
                else
                {
                    AppendLog("Nenhuma porta COM encontrada.");
                }
            }
            catch (Exception ex)
            {
                AppendLog("Erro ao listar portas COM: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string? port = portList.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.Show(
                    "Seleciona uma porta COM.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                serial.Connect(port);

                if (!serial.IsConnected)
                {
                    DefinirControlesConectados(false);
                    AppendLog($"Falha ao conectar em {port}.");
                    return;
                }

                AppendLog($"Conectado em {port}.");
                DefinirControlesConectados(true);

                serial.Send("STATUS");
            }
            catch (Exception ex)
            {
                DefinirControlesConectados(false);
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
            if (string.IsNullOrWhiteSpace(message))
                return;

            string cleanMessage = message.Trim();
            string[] parts = cleanMessage.Split(':', 2);

            string type = parts[0].Trim().ToUpperInvariant();
            string content = parts.Length > 1 ? parts[1].Trim() : "";

            if (type == "INFO")
            {
                if (content.Equals("LED ON", StringComparison.OrdinalIgnoreCase))
                    AtualizarBotaoLed(true);
                else if (content.Equals("LED OFF", StringComparison.OrdinalIgnoreCase))
                    AtualizarBotaoLed(false);
            }

            AppendLog("ESP32: " + cleanMessage);
            ProcessarEstadoLed(cleanMessage);
        }

        private void Serial_ErrorOccurred(string error)
        {
            AppendLog("Erro na comunicação: " + error);

            if (!serial.IsConnected)
            {
                DefinirControlesConectados(false);
            }
        }

        private void ProcessarEstadoLed(string message)
        {
            string upper = message.ToUpperInvariant().Replace(" ", "");

            if (upper.Contains("STATUS:ON") ||
                upper.Contains("LED:ON") ||
                upper.Contains("LED_STATUS:ON") ||
                upper == "LEDON")
            {
                AtualizarBotaoLed(true);
            }
            else if (upper.Contains("STATUS:OFF") ||
                     upper.Contains("LED:OFF") ||
                     upper.Contains("LED_STATUS:OFF") ||
                     upper == "LEDOFF")
            {
                AtualizarBotaoLed(false);
            }
        }

        private void AtualizarBotaoLed(bool ligado)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AtualizarBotaoLed(ligado)));
                return;
            }

            ledStatus = ligado;
            btn_act.Text = ligado ? "OFF" : "ON";
            btn_act.BackColor = ligado ? Color.Red : Color.Green;
            btn_act.ForeColor = Color.White;
        }

        private void AppendLog(string texto)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.BeginInvoke(new Action(() => AppendLog(texto)));
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
                    MessageBox.Show(
                        "Conecta-te primeiro.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    DefinirControlesConectados(false);
                    return;
                }

                string comando = !ledStatus ? "ON" : "OFF";
                serial.Send(comando);
            }
            catch (Exception ex)
            {
                AppendLog("Erro ao enviar comando LED: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serial.IsConnected)
                {
                    MessageBox.Show(
                        "Conecta-te primeiro.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                serial.Send("PING");
            }
            catch (Exception ex)
            {
                AppendLog("Erro ao enviar PING: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            serial.DataReceived -= Serial_DataReceived;
            serial.ErrorOccurred -= Serial_ErrorOccurred;
            serial.Dispose();

            base.OnFormClosing(e);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            using BluetoothConfig dialog = new BluetoothConfig(serial);
            dialog.ShowDialog(this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using ServerConfig dialog = new ServerConfig(serial);
            dialog.ShowDialog(this);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using WifiConfig dialog = new WifiConfig(serial);
            dialog.ShowDialog(this);
        }
    }
}