
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using envcontrol.module;

namespace envcontrol
{
    public partial class WifiConfig : Form
    {
        private readonly SerialCommunication serial;

        private bool _isUpdatingUi;
        private bool _isScanning;
        private string _lastKnownSsid = string.Empty;

        public WifiConfig(SerialCommunication serial)
        {
            InitializeComponent();

            this.serial = serial;

            // Registar eventos antes de comunicar com o ESP32
            serial.DataReceived += Serial_DataReceived;

            FormClosing += WifiConfig_FormClosing;
            Shown += WifiConfig_Shown;

            // Solicitar configurações atuais
            serial.Send("CONFIG");
        }

        // ======================================
        // INICIALIZAÇÃO
        // ======================================

        private async void WifiConfig_Shown(object sender, EventArgs e)
        {
            await RefreshAvailableWifiAsync();
        }

        // ======================================
        // PROCURAR REDES WI-FI
        // ======================================

        public async Task RefreshAvailableWifiAsync()
        {
            if (IsDisposed || !IsHandleCreated || _isScanning)
                return;

            _isScanning = true;

            try
            {
                List<WifiNetworkInfo> networks = await Task.Run(
                    () => WifiScanner.GetAvailableNetworks()
                );

                if (IsDisposed || !IsHandleCreated)
                    return;

                string targetSsid = _lastKnownSsid;

                if (string.IsNullOrEmpty(targetSsid))
                {
                    targetSsid =
                        (avalible_wifi.SelectedItem as WifiNetworkInfo)?.Ssid
                        ?? string.Empty;
                }

                avalible_wifi.BeginUpdate();

                try
                {
                    avalible_wifi.DataSource = null;
                    avalible_wifi.DisplayMember = nameof(WifiNetworkInfo.Ssid);
                    avalible_wifi.ValueMember = nameof(WifiNetworkInfo.Ssid);
                    avalible_wifi.DataSource = networks;

                    SelectNetworkInList(targetSsid);
                }
                finally
                {
                    avalible_wifi.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show(
                        "Erro ao procurar redes Wi-Fi: " + ex.Message,
                        "Wi-Fi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            finally
            {
                _isScanning = false;
            }
        }

        private void SelectNetworkInList(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                return;

            foreach (var item in avalible_wifi.Items)
            {
                if (item is WifiNetworkInfo network &&
                    string.Equals(
                        network.Ssid,
                        ssid,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    avalible_wifi.SelectedItem = network;
                    return;
                }
            }
        }

        // ======================================
        // COMUNICAÇÃO SERIAL
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
                // Form encerrado durante a recepção
            }
        }

        private void ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            message = message.Trim();

            Console.WriteLine("[ESP32] " + message);

            // Ignorar delimitadores de configuração
            if (message == "CONFIG_START" ||
                message == "CONFIG_END")
                return;

            string[] parts = message.Split(new[] { ':' }, 2);

            string type = parts[0].Trim().ToUpperInvariant();

            string content = parts.Length > 1
                ? parts[1].Trim()
                : string.Empty;

            // Mostrar última mensagem recebida
            esp_message.Text = message;
            esp_message.Visible = true;
            esp_message.Tag = type;

            switch (type)
            {
                case "WIFI_SSID":

                    _lastKnownSsid = content;

                    wifiSSID.Text = content;

                    SelectNetworkInList(content);

                    break;

                case "WIFI_IP":

                    wifiIp.Text = content;

                    break;

                case "WIFI_STATUS":

                    wifiStatus.Text = content;

                    break;

                case "WIFI_ENABLED":

                    bool enabled =
                        content.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                        content.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

                    SetCheckedWithoutEvent(enabled);

                    break;

                case "INFO":

                    // Notificações do ESP32
                    wifiStatus.Text = content;

                    break;
            }
        }

        // ======================================
        // ATUALIZAR INTERFACE
        // ======================================

        private void SetCheckedWithoutEvent(bool value)
        {
            _isUpdatingUi = true;

            try
            {
                wifiEnCheck.Checked = value;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        // ======================================
        // CONFIGURAR WI-FI
        // ======================================

        private async void button1_Click(object sender, EventArgs e)
        {
            string ssid =
                (avalible_wifi.SelectedItem as WifiNetworkInfo)?.Ssid
                ?? string.Empty;

            string password = box_password.Text;

            if (string.IsNullOrWhiteSpace(ssid))
            {
                MessageBox.Show(
                    "Selecione uma rede Wi-Fi.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            // O protocolo atual do ESP32 utiliza o último espaço
            // para separar a password do SSID.
            if (password.Contains(' '))
            {
                MessageBox.Show(
                    "A versão atual do firmware não suporta espaços na password Wi-Fi.",
                    "Password inválida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            DialogResult confirmation = MessageBox.Show(
                $"Deseja conectar à rede '{ssid}'?",
                "Configurar Wi-Fi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                _lastKnownSsid = ssid;

                serial.Send(
                    $"SET WIFI SSID PASS {ssid} {password}"
                );

                wifiStatus.Text = "CONNECTING...";

                await Task.Delay(1000);

                if (!IsDisposed && IsHandleCreated)
                {
                    serial.Send("CONFIG");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao configurar Wi-Fi: " + ex.Message,
                    "Comunicação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ======================================
        // ATIVAR / DESATIVAR WI-FI
        // ======================================

        private void wifiEnStr_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi)
                return;

            bool enabled = wifiEnCheck.Checked;

            serial.Send(
                "WIFI " + (enabled ? "ON" : "OFF")
            );
        }

        // ======================================
        // ATUALIZAR CONFIGURAÇÕES
        // ======================================

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            serial.Send("CONFIG");
        }

        // ======================================
        // ENCERRAMENTO
        // ======================================

        private void WifiConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            serial.DataReceived -= Serial_DataReceived;
        }
    }
}
