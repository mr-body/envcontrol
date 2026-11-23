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

            serial.DataReceived += Serial_DataReceived;

            FormClosing += WifiConfig_FormClosing;
            Shown += WifiConfig_Shown;
        }

        // ======================================
        // INICIALIZAÇÃO
        // ======================================

        private async void WifiConfig_Shown(object? sender, EventArgs e)
        {
            TrySend("CONFIG");
            await RefreshAvailableWifiAsync();
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
                        StringComparison.OrdinalIgnoreCase))
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
                    {
                        bool enabled =
                            content.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                            content.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                            content.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

                        SetCheckedWithoutEvent(enabled);
                        break;
                    }

                case "INFO":
                    // Só reage a eventos de Wi-Fi
                    if (content.StartsWith("WIFI", StringComparison.OrdinalIgnoreCase))
                    {
                        wifiStatus.Text = content;

                        if (content.Equals("WIFI CONNECTED", StringComparison.OrdinalIgnoreCase))
                            TrySend("CONFIG"); // atualizar o IP
                    }
                    break;
            }
        }

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

        private async void button1_Click(object? sender, EventArgs e)
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

            // O separador no firmware é '|'
            if (ssid.Contains('|'))
            {
                MessageBox.Show(
                    "O nome da rede não pode conter o carácter '|'.",
                    "Rede inválida",
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

            _lastKnownSsid = ssid;

            if (!TrySend($"SET WIFI SSID PASS {ssid}|{password}"))
                return;

            wifiStatus.Text = "CONNECTING...";

            await Task.Delay(1000);

            if (!IsDisposed && IsHandleCreated)
                TrySend("CONFIG");
        }

        // ======================================
        // ATIVAR / DESATIVAR WI-FI
        // ======================================

        private void wifiEnStr_CheckedChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingUi)
                return;

            TrySend("WIFI " + (wifiEnCheck.Checked ? "ON" : "OFF"));
            if (!IsDisposed && IsHandleCreated)
                TrySend("CONFIG");
        }

        // ======================================
        // ATUALIZAR
        // ======================================

        private void buttonRefresh_Click(object? sender, EventArgs e)
        {
            TrySend("CONFIG");
        }

        // ======================================
        // ENCERRAMENTO
        // ======================================

        private void WifiConfig_FormClosing(object? sender, FormClosingEventArgs e)
        {
            serial.DataReceived -= Serial_DataReceived;
        }
    }
}