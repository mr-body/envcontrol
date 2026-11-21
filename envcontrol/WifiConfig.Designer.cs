namespace envcontrol
{
    partial class WifiConfig
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            wifiStatus = new Label();
            wifiSSID = new Label();
            wifiIp = new Label();
            label7 = new Label();
            avalible_wifi = new ComboBox();
            wps2 = new CheckBox();
            box_password = new TextBox();
            label8 = new Label();
            button1 = new Button();
            wifiEnCheck = new CheckBox();
            esp_message = new Label();
            SuspendLayout();
            // 
            // wifiStatus
            // 
            wifiStatus.AutoSize = true;
            wifiStatus.Location = new Point(337, 57);
            wifiStatus.Name = "wifiStatus";
            wifiStatus.Size = new Size(38, 15);
            wifiStatus.TabIndex = 1;
            wifiStatus.Text = "label2";
            // 
            // wifiSSID
            // 
            wifiSSID.AutoSize = true;
            wifiSSID.Location = new Point(337, 27);
            wifiSSID.Name = "wifiSSID";
            wifiSSID.Size = new Size(38, 15);
            wifiSSID.TabIndex = 2;
            wifiSSID.Text = "label3";
            // 
            // wifiIp
            // 
            wifiIp.AutoSize = true;
            wifiIp.Location = new Point(337, 84);
            wifiIp.Name = "wifiIp";
            wifiIp.Size = new Size(38, 15);
            wifiIp.TabIndex = 3;
            wifiIp.Text = "label4";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 20F);
            label7.Location = new Point(12, 18);
            label7.Name = "label7";
            label7.Size = new Size(155, 37);
            label7.TabIndex = 6;
            label7.Text = "WIFI Config";
            // 
            // avalible_wifi
            // 
            avalible_wifi.FormattingEnabled = true;
            avalible_wifi.Location = new Point(12, 77);
            avalible_wifi.Name = "avalible_wifi";
            avalible_wifi.Size = new Size(143, 23);
            avalible_wifi.TabIndex = 7;
            // 
            // wps2
            // 
            wps2.AutoSize = true;
            wps2.Location = new Point(12, 106);
            wps2.Name = "wps2";
            wps2.Size = new Size(56, 19);
            wps2.TabIndex = 8;
            wps2.Text = "WPS2";
            wps2.UseVisualStyleBackColor = true;
            // 
            // box_password
            // 
            box_password.Location = new Point(12, 173);
            box_password.Name = "box_password";
            box_password.Size = new Size(143, 23);
            box_password.TabIndex = 9;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 155);
            label8.Name = "label8";
            label8.Size = new Size(64, 15);
            label8.TabIndex = 10;
            label8.Text = "Passoword";
            // 
            // button1
            // 
            button1.Location = new Point(12, 214);
            button1.Name = "button1";
            button1.Size = new Size(143, 23);
            button1.TabIndex = 11;
            button1.Text = "Conectar";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // wifiEnCheck
            // 
            wifiEnCheck.AutoSize = true;
            wifiEnCheck.Location = new Point(543, 247);
            wifiEnCheck.Name = "wifiEnCheck";
            wifiEnCheck.Size = new Size(82, 19);
            wifiEnCheck.TabIndex = 12;
            wifiEnCheck.Text = "checkBox1";
            wifiEnCheck.UseVisualStyleBackColor = true;
            wifiEnCheck.CheckedChanged += wifiEnStr_CheckedChanged;
            // 
            // esp_message
            // 
            esp_message.AutoSize = true;
            esp_message.Location = new Point(12, 254);
            esp_message.Name = "esp_message";
            esp_message.Size = new Size(116, 15);
            esp_message.TabIndex = 14;
            esp_message.Tag = "hk";
            esp_message.Text = "Mensagem do esp32";
            esp_message.Visible = false;
            // 
            // WifiConfig
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(637, 278);
            Controls.Add(esp_message);
            Controls.Add(wifiEnCheck);
            Controls.Add(button1);
            Controls.Add(label8);
            Controls.Add(box_password);
            Controls.Add(wps2);
            Controls.Add(avalible_wifi);
            Controls.Add(label7);
            Controls.Add(wifiIp);
            Controls.Add(wifiSSID);
            Controls.Add(wifiStatus);
            Name = "WifiConfig";
            Text = "WifiConfig";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label wifiStatus;
        private Label wifiSSID;
        private Label wifiIp;
        private Label label7;
        private ComboBox avalible_wifi;
        private CheckBox wps2;
        private TextBox box_password;
        private Label label8;
        private Button button1;
        private CheckBox wifiEnCheck;
        private Label esp_message;
    }
}