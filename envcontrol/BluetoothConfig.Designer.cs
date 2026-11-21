namespace envcontrol
{
    partial class BluetoothConfig
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
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            lbt_name = new Label();
            lbt_pin = new Label();
            lbt_status = new Label();
            label7 = new Label();
            label8 = new Label();
            box_bt_name = new TextBox();
            box_bt_pass = new TextBox();
            label9 = new Label();
            check_bt_status = new CheckBox();
            btn_save_bt_config = new Button();
            esp_message = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20F);
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(212, 37);
            label1.TabIndex = 0;
            label1.Text = "BluetoothConfig";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(21, 87);
            label2.Name = "label2";
            label2.Size = new Size(40, 15);
            label2.TabIndex = 1;
            label2.Text = "Nome";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(22, 110);
            label3.Name = "label3";
            label3.Size = new Size(26, 15);
            label3.TabIndex = 2;
            label3.Text = "PIN";
            // 
            // lbt_name
            // 
            lbt_name.AutoSize = true;
            lbt_name.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbt_name.Location = new Point(74, 87);
            lbt_name.Name = "lbt_name";
            lbt_name.Size = new Size(40, 15);
            lbt_name.TabIndex = 3;
            lbt_name.Text = "label4";
            // 
            // lbt_pin
            // 
            lbt_pin.AutoSize = true;
            lbt_pin.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbt_pin.Location = new Point(74, 111);
            lbt_pin.Name = "lbt_pin";
            lbt_pin.Size = new Size(40, 15);
            lbt_pin.TabIndex = 4;
            lbt_pin.Text = "label5";
            // 
            // lbt_status
            // 
            lbt_status.AutoSize = true;
            lbt_status.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbt_status.Location = new Point(75, 63);
            lbt_status.Name = "lbt_status";
            lbt_status.Size = new Size(40, 15);
            lbt_status.TabIndex = 6;
            lbt_status.Text = "label6";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(21, 63);
            label7.Name = "label7";
            label7.Size = new Size(39, 15);
            label7.TabIndex = 5;
            label7.Text = "Status";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(22, 211);
            label8.Name = "label8";
            label8.Size = new Size(40, 15);
            label8.TabIndex = 7;
            label8.Text = "Nome";
            // 
            // box_bt_name
            // 
            box_bt_name.Location = new Point(21, 229);
            box_bt_name.Name = "box_bt_name";
            box_bt_name.PlaceholderText = "ESP32_ENVCONTROL";
            box_bt_name.Size = new Size(203, 23);
            box_bt_name.TabIndex = 8;
            // 
            // box_bt_pass
            // 
            box_bt_pass.Location = new Point(22, 293);
            box_bt_pass.Name = "box_bt_pass";
            box_bt_pass.PlaceholderText = "* * * * * *";
            box_bt_pass.Size = new Size(202, 23);
            box_bt_pass.TabIndex = 10;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(22, 273);
            label9.Name = "label9";
            label9.Size = new Size(57, 15);
            label9.TabIndex = 9;
            label9.Text = "Password";
            // 
            // check_bt_status
            // 
            check_bt_status.AutoSize = true;
            check_bt_status.Location = new Point(706, 12);
            check_bt_status.Name = "check_bt_status";
            check_bt_status.Size = new Size(82, 19);
            check_bt_status.TabIndex = 11;
            check_bt_status.Text = "checkBox1";
            check_bt_status.UseVisualStyleBackColor = true;
            check_bt_status.CheckedChanged += check_bt_status_CheckedChanged;
            // 
            // btn_save_bt_config
            // 
            btn_save_bt_config.Location = new Point(230, 229);
            btn_save_bt_config.Name = "btn_save_bt_config";
            btn_save_bt_config.Size = new Size(109, 23);
            btn_save_bt_config.TabIndex = 12;
            btn_save_bt_config.Text = "Salvar";
            btn_save_bt_config.UseVisualStyleBackColor = true;
            btn_save_bt_config.Click += btn_save_bt_config_Click;
            // 
            // esp_message
            // 
            esp_message.AutoSize = true;
            esp_message.Location = new Point(24, 408);
            esp_message.Name = "esp_message";
            esp_message.Size = new Size(116, 15);
            esp_message.TabIndex = 13;
            esp_message.Tag = "hk";
            esp_message.Text = "Mensagem do esp32";
            esp_message.Visible = false;
            // 
            // button1
            // 
            button1.Location = new Point(230, 293);
            button1.Name = "button1";
            button1.Size = new Size(109, 23);
            button1.TabIndex = 14;
            button1.Text = "Salvar";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BluetoothConfig
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btn_save_bt_config;
            ClientSize = new Size(796, 445);
            Controls.Add(button1);
            Controls.Add(esp_message);
            Controls.Add(btn_save_bt_config);
            Controls.Add(check_bt_status);
            Controls.Add(box_bt_pass);
            Controls.Add(label9);
            Controls.Add(box_bt_name);
            Controls.Add(label8);
            Controls.Add(lbt_status);
            Controls.Add(label7);
            Controls.Add(lbt_pin);
            Controls.Add(lbt_name);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "BluetoothConfig";
            StartPosition = FormStartPosition.CenterParent;
            Text = "BluetoothConfig";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label lbt_name;
        private Label lbt_pin;
        private Label lbt_status;
        private Label label7;
        private Label label8;
        private TextBox box_bt_name;
        private TextBox box_bt_pass;
        private Label label9;
        private CheckBox check_bt_status;
        private Button btn_save_bt_config;
        private Label esp_message;
        private Button button1;
    }
}