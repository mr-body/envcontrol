namespace envcontrol
{
    partial class ServerConfig
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
            label7 = new Label();
            label8 = new Label();
            box_web_server_host = new TextBox();
            label1 = new Label();
            box_web_server_port = new TextBox();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            box_local_server_port = new TextBox();
            LocalServerEnCheck = new CheckBox();
            label5 = new Label();
            box_web_server_token = new TextBox();
            guna2vSeparator1 = new Guna.UI2.WinForms.Guna2VSeparator();
            button1 = new Button();
            button2 = new Button();
            esp_message = new Label();
            local_client_auth = new Label();
            remote_connected = new Label();
            remote_auth = new Label();
            local_client_conected = new Label();
            label14 = new Label();
            box_local_password = new TextBox();
            SuspendLayout();
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 20F);
            label7.Location = new Point(12, 21);
            label7.Name = "label7";
            label7.Size = new Size(176, 37);
            label7.TabIndex = 7;
            label7.Text = "Server Config";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(20, 171);
            label8.Name = "label8";
            label8.Size = new Size(77, 15);
            label8.TabIndex = 12;
            label8.Text = "Servidor Web";
            // 
            // box_web_server_host
            // 
            box_web_server_host.Location = new Point(20, 189);
            box_web_server_host.Name = "box_web_server_host";
            box_web_server_host.Size = new Size(143, 23);
            box_web_server_host.TabIndex = 11;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(169, 171);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 14;
            label1.Text = "Porta";
            // 
            // box_web_server_port
            // 
            box_web_server_port.Location = new Point(169, 189);
            box_web_server_port.Name = "box_web_server_port";
            box_web_server_port.Size = new Size(64, 23);
            box_web_server_port.TabIndex = 13;
            box_web_server_port.Text = "9090";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 20F);
            label2.Location = new Point(12, 124);
            label2.Name = "label2";
            label2.Size = new Size(151, 37);
            label2.TabIndex = 15;
            label2.Text = "Web Server";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 20F);
            label3.Location = new Point(413, 124);
            label3.Name = "label3";
            label3.Size = new Size(156, 37);
            label3.TabIndex = 16;
            label3.Text = "Local server";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(421, 260);
            label4.Name = "label4";
            label4.Size = new Size(35, 15);
            label4.TabIndex = 18;
            label4.Text = "Porta";
            // 
            // box_local_server_port
            // 
            box_local_server_port.Location = new Point(421, 278);
            box_local_server_port.Name = "box_local_server_port";
            box_local_server_port.Size = new Size(64, 23);
            box_local_server_port.TabIndex = 17;
            box_local_server_port.Text = "8080";
            // 
            // LocalServerEnCheck
            // 
            LocalServerEnCheck.AutoSize = true;
            LocalServerEnCheck.Enabled = false;
            LocalServerEnCheck.Location = new Point(421, 176);
            LocalServerEnCheck.Name = "LocalServerEnCheck";
            LocalServerEnCheck.Size = new Size(82, 19);
            LocalServerEnCheck.TabIndex = 19;
            LocalServerEnCheck.Text = "checkBox1";
            LocalServerEnCheck.UseVisualStyleBackColor = true;
            LocalServerEnCheck.CheckedChanged += LocalServerEnCheck_CheckedChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(20, 223);
            label5.Name = "label5";
            label5.Size = new Size(116, 15);
            label5.TabIndex = 21;
            label5.Text = "Token do dispositivo";
            // 
            // box_web_server_token
            // 
            box_web_server_token.Location = new Point(20, 241);
            box_web_server_token.Name = "box_web_server_token";
            box_web_server_token.Size = new Size(213, 23);
            box_web_server_token.TabIndex = 20;
            // 
            // guna2vSeparator1
            // 
            guna2vSeparator1.Location = new Point(336, 45);
            guna2vSeparator1.Name = "guna2vSeparator1";
            guna2vSeparator1.Size = new Size(11, 358);
            guna2vSeparator1.TabIndex = 22;
            // 
            // button1
            // 
            button1.Location = new Point(20, 282);
            button1.Name = "button1";
            button1.Size = new Size(128, 23);
            button1.TabIndex = 23;
            button1.Text = "Salvar";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(491, 279);
            button2.Name = "button2";
            button2.Size = new Size(111, 23);
            button2.TabIndex = 24;
            button2.Text = "Salvar";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // esp_message
            // 
            esp_message.AutoSize = true;
            esp_message.Location = new Point(12, 423);
            esp_message.Name = "esp_message";
            esp_message.Size = new Size(116, 15);
            esp_message.TabIndex = 25;
            esp_message.Tag = "hk";
            esp_message.Text = "Mensagem do esp32";
            esp_message.Visible = false;
            // 
            // local_client_auth
            // 
            local_client_auth.AutoSize = true;
            local_client_auth.Location = new Point(20, 356);
            local_client_auth.Name = "local_client_auth";
            local_client_auth.Size = new Size(38, 15);
            local_client_auth.TabIndex = 27;
            local_client_auth.Text = "label9";
            // 
            // remote_connected
            // 
            remote_connected.AutoSize = true;
            remote_connected.Location = new Point(20, 388);
            remote_connected.Name = "remote_connected";
            remote_connected.Size = new Size(44, 15);
            remote_connected.TabIndex = 28;
            remote_connected.Text = "label10";
            // 
            // remote_auth
            // 
            remote_auth.AutoSize = true;
            remote_auth.Location = new Point(144, 322);
            remote_auth.Name = "remote_auth";
            remote_auth.Size = new Size(44, 15);
            remote_auth.TabIndex = 29;
            remote_auth.Text = "label11";
            // 
            // local_client_conected
            // 
            local_client_conected.AutoSize = true;
            local_client_conected.Location = new Point(20, 322);
            local_client_conected.Name = "local_client_conected";
            local_client_conected.Size = new Size(44, 15);
            local_client_conected.TabIndex = 30;
            local_client_conected.Text = "label12";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(421, 209);
            label14.Name = "label14";
            label14.Size = new Size(57, 15);
            label14.TabIndex = 33;
            label14.Text = "Password";
            // 
            // box_local_password
            // 
            box_local_password.Location = new Point(421, 227);
            box_local_password.Name = "box_local_password";
            box_local_password.Size = new Size(213, 23);
            box_local_password.TabIndex = 32;
            // 
            // ServerConfig
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label14);
            Controls.Add(box_local_password);
            Controls.Add(local_client_conected);
            Controls.Add(remote_auth);
            Controls.Add(remote_connected);
            Controls.Add(local_client_auth);
            Controls.Add(esp_message);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(guna2vSeparator1);
            Controls.Add(label5);
            Controls.Add(box_web_server_token);
            Controls.Add(LocalServerEnCheck);
            Controls.Add(label4);
            Controls.Add(box_local_server_port);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(box_web_server_port);
            Controls.Add(label8);
            Controls.Add(box_web_server_host);
            Controls.Add(label7);
            Name = "ServerConfig";
            Text = "ServerConfig";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label7;
        private Label label8;
        private TextBox box_web_server_host;
        private Label label1;
        private TextBox box_web_server_port;
        private Label label2;
        private Label label3;
        private Label label4;
        private TextBox box_local_server_port;
        private CheckBox LocalServerEnCheck;
        private Label label5;
        private TextBox box_web_server_token;
        private Guna.UI2.WinForms.Guna2VSeparator guna2vSeparator1;
        private Button button1;
        private Button button2;
        private Label esp_message;
        private Label local_client_auth;
        private Label remote_connected;
        private Label remote_auth;
        private Label local_client_conected;
        private Label label14;
        private TextBox box_local_password;
    }
}