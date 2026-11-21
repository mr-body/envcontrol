namespace envcontrol
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btn_act = new Button();
            label = new Label();
            txtLogs = new RichTextBox();
            button1 = new Button();
            portList = new ComboBox();
            btn_ping = new Button();
            button2 = new Button();
            button3 = new Button();
            SuspendLayout();
            // 
            // btn_act
            // 
            btn_act.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btn_act.BackColor = Color.Red;
            btn_act.Enabled = false;
            btn_act.Font = new Font("Segoe UI", 14F);
            btn_act.ForeColor = Color.White;
            btn_act.Location = new Point(24, 395);
            btn_act.Name = "btn_act";
            btn_act.Size = new Size(395, 93);
            btn_act.TabIndex = 0;
            btn_act.Text = "OFF";
            btn_act.UseVisualStyleBackColor = false;
            btn_act.Click += btn_act_Click;
            // 
            // label
            // 
            label.Font = new Font("Segoe UI", 25F);
            label.Location = new Point(24, 9);
            label.Name = "label";
            label.Size = new Size(194, 46);
            label.TabIndex = 1;
            label.Text = "EspControl";
            // 
            // txtLogs
            // 
            txtLogs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLogs.Location = new Point(24, 61);
            txtLogs.Name = "txtLogs";
            txtLogs.Size = new Size(709, 328);
            txtLogs.TabIndex = 2;
            txtLogs.Text = "";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button1.Location = new Point(582, 395);
            button1.Name = "button1";
            button1.Size = new Size(151, 40);
            button1.TabIndex = 4;
            button1.Text = "Conectar";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // portList
            // 
            portList.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            portList.FormattingEnabled = true;
            portList.Location = new Point(487, 32);
            portList.Name = "portList";
            portList.Size = new Size(246, 23);
            portList.TabIndex = 5;
            portList.Text = "Select port";
            // 
            // btn_ping
            // 
            btn_ping.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_ping.Enabled = false;
            btn_ping.Location = new Point(582, 450);
            btn_ping.Name = "btn_ping";
            btn_ping.Size = new Size(151, 38);
            btn_ping.TabIndex = 6;
            btn_ping.Text = "Ping";
            btn_ping.UseVisualStyleBackColor = true;
            btn_ping.Click += button2_Click;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button2.Enabled = false;
            button2.Location = new Point(425, 395);
            button2.Name = "button2";
            button2.Size = new Size(151, 40);
            button2.TabIndex = 7;
            button2.Text = "Bluetooth Config";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click_1;
            // 
            // button3
            // 
            button3.Location = new Point(425, 450);
            button3.Name = "button3";
            button3.Size = new Size(151, 38);
            button3.TabIndex = 8;
            button3.Text = "WI-FI";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(762, 500);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(btn_ping);
            Controls.Add(portList);
            Controls.Add(button1);
            Controls.Add(txtLogs);
            Controls.Add(label);
            Controls.Add(btn_act);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_act;
        private Label label;
        private RichTextBox txtLogs;
        private Button button1;
        private ComboBox portList;
        private Button btn_ping;
        private Button button2;
        private Button button3;
    }
}
