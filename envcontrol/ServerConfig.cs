using envcontrol.module;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace envcontrol
{
    public partial class ServerConfig : Form
    {
        private readonly SerialCommunication serial;

        private bool _isUpdatingUi;

        public ServerConfig(SerialCommunication serial)
        {
            InitializeComponent();

            this.serial = serial;

            serial.DataReceived += Serial_DataReceived;

            FormClosing += ServerConfig_FormClosing;
            Shown += ServerConfig_Shown;
        }

        // ======================================
        // INICIALIZAÇÃO / ENCERRAMENTO
        // ======================================

        private void ServerConfig_Shown(object? sender, EventArgs e)
        {
            TrySend("CONFIG");
        }

        private void ServerConfig_FormClosing(object? sender, FormClosingEventArgs e)
        {
            serial.DataReceived -= Serial_DataReceived;
        }

        // ======================================
        // ENVIO SEGURO
        // ======================================

        private bool TrySend(string command)
        {
            try
            {
                if (!serial.IsConnected)
                {
                    ShowEspMessage("Sem ligação ao ESP32.");
                    return false;
                }

                serial.Send(command);
                return true;
            }
            catch (Exception ex)
            {
                ShowEspMessage("Erro ao enviar: " + ex.Message);
                return false;
            }
        }

        private void ShowEspMessage(string text)
        {
            esp_message.Text = text;
            esp_message.Visible = true;
        }

        // ======================================
        // RECEÇÃO
        // ======================================

        private void Serial_DataReceived(string message)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => ProcessMessage(message)));
                    return;
                }

                ProcessMessage(message);
            }
            catch (InvalidOperationException)
            {
                // Form encerrado durante a receção
            }
        }

        private void ProcessMessage(string message)
        {
            if (IsDisposed || string.IsNullOrWhiteSpace(message))
                return;

            message = message.Trim();

            if (message == "CONFIG_START" || message == "CONFIG_END")
                return;

            string[] parts = message.Split(new[] { ':' }, 2);

            string type = parts[0].Trim().ToUpperInvariant();
            string content = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            ShowEspMessage(message);

            switch (type)
            {
                case "LOCAL_SERVER_PORT":
                    box_local_server_port.Text = content;
                    break;
                case "LOCAL_PASSWORD":
                    box_local_password.Text = content;
                    break;
                case "REMOTE_TOKEN":
                    box_web_server_token.Text = content;
                    break;

                // O ESP32 já não envia a password nem o token,
                // só informa se estão definidos.
                case "LOCAL_PASSWORD_SET":
                    box_local_password.PlaceholderText = IsYes(content)
                        ? "Definida (escreve para alterar)"
                        : "Não definida";
                    break;

                case "REMOTE_TOKEN_SET":
                    box_web_server_token.PlaceholderText = IsYes(content)
                        ? "Definido (escreve para alterar)"
                        : "Não definido";
                    break;

                case "REMOTE_HOST":
                    box_web_server_host.Text = content;
                    break;

                case "REMOTE_PORT":
                    box_web_server_port.Text = content;
                    break;

                case "LOCAL_CLIENT_CONNECTED":
                    local_client_conected.Text = content;
                    break;

                case "LOCAL_CLIENT_AUTHENTICATED":
                    local_client_auth.Text = content;
                    break;

                case "REMOTE_CONNECTED":
                    remote_connected.Text = content;
                    break;

                case "REMOTE_AUTHENTICATED":
                    remote_auth.Text = content;
                    break;

                case "LOCAL_SERVER_RUNNING":
                    SetCheckedWithoutEvent(IsYes(content));
                    break;
            }
        }

        private static bool IsYes(string value)
        {
            return value.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private void SetCheckedWithoutEvent(bool value)
        {
            _isUpdatingUi = true;

            try
            {
                LocalServerEnCheck.Checked = value;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        // ======================================
        // VALIDAÇÃO
        // ======================================

        private static bool TryParsePort(string text, out int port)
        {
            return int.TryParse(text.Trim(), out port) && port >= 1 && port <= 65535;
        }

        private static void Warn(string text)
        {
            MessageBox.Show(
                text,
                "Validação",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        // ======================================
        // SERVIDOR REMOTO
        // ======================================

        private async void button1_Click(object? sender, EventArgs e)
        {
            string host = box_web_server_host.Text.Trim();
            string token = box_web_server_token.Text.Trim();

            if (host.Length == 0)
            {
                Warn("Indica o host do servidor remoto.");
                return;
            }

            if (!TryParsePort(box_web_server_port.Text, out int port))
            {
                Warn("Porta remota inválida (1-65535).");
                return;
            }

            // Token vazio = manter o atual
            if (token.Length > 0 && (token.Length < 32 || token.Length > 128))
            {
                Warn("O token deve ter entre 32 e 128 caracteres.");
                return;
            }

            if (!TrySend($"SET REMOTE HOST {host}"))
                return;
            await Task.Delay(150);

            if (!TrySend($"SET REMOTE PORT {port}"))
                return;
            await Task.Delay(150);

            if (token.Length > 0)
            {
                if (!TrySend($"SET REMOTE TOKEN {token}"))
                    return;

                box_web_server_token.Clear();
                await Task.Delay(150);
            }

            if (!IsDisposed && IsHandleCreated)
                TrySend("CONFIG");
        }

        // ======================================
        // SERVIDOR LOCAL
        // ======================================

        private async void button2_Click(object? sender, EventArgs e)
        {
            string password = box_local_password.Text;

            if (!TryParsePort(box_local_server_port.Text, out int port))
            {
                Warn("Porta local inválida (1-65535).");
                return;
            }

            // Password vazia = manter a atual
            if (password.Length > 0 && (password.Length < 8 || password.Length > 64))
            {
                Warn("A password deve ter entre 8 e 64 caracteres.");
                return;
            }

            if (!TrySend($"SET LOCAL PORT {port}"))
                return;
            await Task.Delay(150);

            if (password.Length > 0)
            {
                if (!TrySend($"SET LOCAL PASSWORD {password}"))
                    return;

                box_local_password.Clear();
                await Task.Delay(150);
            }

            // O servidor local reinicia sozinho; dar tempo e atualizar
            await Task.Delay(500);

            if (!IsDisposed && IsHandleCreated)
                TrySend("CONFIG");
        }

        private void label13_Click(object? sender, EventArgs e)
        {
        }

        private void LocalServerEnCheck_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}