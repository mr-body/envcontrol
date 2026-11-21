using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using envcontrol.module;

namespace envcontrol
{
    public partial class WifiConfig : Form
    {
        private readonly SerialCommunication serial;
        private bool _isUpdatingUi = false;
        private bool _isScanning = false;
        private string _lastKnownSsid = string.Empty;

        public WifiConfig(SerialCommunication serial)
        {
            InitializeComponent();

            this.serial = serial;

            // Registar eventos antes de enviar o comando
            serial.DataReceived += Serial_DataReceived;
            this.FormClosing += WifiConfig_FormClosing;
            this.Shown += WifiConfig_Shown;

            // Solicitar configurações completas ao ESP32
            serial.Send("CONFIG");
        }

        private async void WifiConfig_Shown(object sender, EventArgs e)
        {
            await RefreshAvailableWifiAsync();
        }

        public async Task RefreshAvailableWifiAsync()
        {
            if (IsDisposed || !IsHandleCreated || _isScanning)
                return;

            _isScanning = true;
            try
            {
                List<WifiNetworkInfo> networks = await Task.Run(() => WifiScanner.GetAvailableNetworks());

                if (IsDisposed || !IsHandleCreated)
                    return;

                string targetSsid = !string.IsNullOrWhiteSpace(_lastKnownSsid)
                    ? _lastKnownSsid
                    : ((avalible_wifi.SelectedItem as WifiNetworkInfo)?.Ssid
                       ?? avalible_wifi.SelectedItem?.ToString()
                       ?? string.Empty);

                avalible_wifi.BeginUpdate();
                try
                {
                    avalible_wifi.DataSource = null;
                    avalible_wifi.DataSource = networks;
                    avalible_wifi.DisplayMember = nameof(WifiNetworkInfo.Ssid);
                    avalible_wifi.ValueMember = nameof(WifiNetworkInfo.Ssid);

                    if (!string.IsNullOrWhiteSpace(targetSsid))
                    {
                        SelectNetworkInList(targetSsid);
                    }
                }
                finally
                {
                    avalible_wifi.EndUpdate();
                }
            }
            finally
            {
                _isScanning = false;
            }
        }

        private void WifiConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serial != null)
            {
                serial.DataReceived -= Serial_DataReceived;
            }
        }

        private void Serial_DataReceived(string message)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ProcessMessage(message)));
                return;
            }

            ProcessMessage(message);
        }

        private void ProcessMessage(string message)
        {
            message = message.Trim();
            if (string.IsNullOrWhiteSpace(message))
                return;

            Console.WriteLine("ESP32: " + message);

            // Linhas delimitadoras do bloco CONFIG
            if (message == "CONFIG_START" || message == "CONFIG_END")
                return;

            string[] parts = message.Split(':', 2);
            string type = parts[0].Trim().ToUpperInvariant();
            string content = parts.Length > 1 ? parts[1].Trim() : string.Empty;

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
                    SetCheckedWithoutEvent(
                        content.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                        content.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    );
                    break;
            }
        }

        private void SelectNetworkInList(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid) || avalible_wifi.Items.Count == 0)
                return;

            foreach (var item in avalible_wifi.Items)
            {
                if (item is WifiNetworkInfo net &&
                    net.Ssid.Equals(ssid, StringComparison.OrdinalIgnoreCase))
                {
                    avalible_wifi.SelectedItem = net;
                    return;
                }
            }
        }

        private void SetCheckedWithoutEvent(bool isChecked)
        {
            _isUpdatingUi = true;
            try
            {
                wifiEnCheck.Checked = isChecked;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string ssid = (avalible_wifi.SelectedItem as WifiNetworkInfo)?.Ssid
                          ?? avalible_wifi.SelectedItem?.ToString()?.Trim()
                          ?? string.Empty;
            string password = box_password.Text.Trim();

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

            if (string.IsNullOrWhiteSpace(password))
            {
                DialogResult result = MessageBox.Show(
                    "A senha está em branco. Deseja conectar como rede aberta?",
                    "Confirmação",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                    return;
            }

            _lastKnownSsid = ssid;
            serial.Send($"SET WIFI SSID PASS {ssid} {password}");

            await Task.Delay(800);
            if (!IsDisposed && IsHandleCreated)
            {
                serial.Send("CONFIG");
            }
        }

        private void wifiEnStr_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi)
                return;

            bool status = wifiEnCheck.Checked;
            serial.Send("SET WIFI ENABLED " + (status ? "ON" : "OFF"));
        }
    }
}