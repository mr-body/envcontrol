using System;
using System.Windows.Forms;
using envcontrol.module;

namespace envcontrol
{
    public partial class BluetoothConfig : Form
    {
        private readonly SerialCommunication serial;
        private bool _isUpdatingUi = false;

        public BluetoothConfig(SerialCommunication serial)
        {
            InitializeComponent();

            this.serial = serial;

            // Registar eventos antes de enviar o comando
            serial.DataReceived += Serial_DataReceived;
            this.FormClosing += BluetoothConfig_FormClosing;

            // Solicitar configurações completas ao ESP32
            serial.Send("CONFIG");
        }

        private void BluetoothConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serial != null)
            {
                serial.DataReceived -= Serial_DataReceived;
            }
        }

        // ======================================
        // RECEBER DADOS DO ESP32
        // ======================================

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

        // ======================================
        // PROCESSAR MENSAGENS
        // ======================================

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
                case "BT_NAME":
                case "NAME":
                    box_bt_name.Text = content;
                    lbt_name.Text = content;
                    break;

                case "BT_PIN":
                case "PIN":
                case "PASSWORD":
                    box_bt_pass.Text = content;
                    lbt_pin.Text = content;
                    break;

                case "BT_ENABLED":
                case "ENABLED":
                    SetCheckedWithoutEvent(content.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                                          content.Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                    lbt_status.Text = content;
                    break;
            }
        }

        private void SetCheckedWithoutEvent(bool isChecked)
        {
            _isUpdatingUi = true;
            try
            {
                check_bt_status.Checked = isChecked;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        // ======================================
        // GUARDAR CONFIGURAÇÕES
        // ======================================

        private void button1_Click(object sender, EventArgs e)
        {
            string password = box_bt_pass.Text.Trim();

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Introduza o PIN Bluetooth.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (password.Length < 4 || password.Length > 16)
            {
                MessageBox.Show(
                    "O PIN Bluetooth deve ter entre 4 e 16 caracteres.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            serial.Send("SET BT PIN " + password);
            serial.Send("RESTART");
        }

        private void btn_save_bt_config_Click(object sender, EventArgs e)
        {
            string name = box_bt_name.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Introduza o nome Bluetooth.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            serial.Send("SET BT NAME " + name);
            serial.Send("RESTART");
        }

        private void check_bt_status_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi)
                return;

            bool status = check_bt_status.Checked;
            serial.Send("SET BT ENABLED " + (status ? "ON" : "OFF"));
            serial.Send("RESTART");
        }
    }
}