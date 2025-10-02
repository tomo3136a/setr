using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace Tmm
{
    partial class UI
    {
        /////////////////////////////////////////////////////////////////////
        // dialog

        /// <summary>
        /// input dialog
        /// </summary>
        public class InputDialog : Form
        {
            Label textLabel = new Label();
            Button accept = new Button();
            Button cancel = new Button();
            Button config = new Button();
            ComboBox textBox = new ComboBox();
            Label txt1Label = new Label();
            Label txt2Label = new Label();
            Label modeLabel = new Label();
            ComboBox comboBox = new ComboBox();
            ListBox listBox = new ListBox();

            public InputDialog(string text, string caption, bool bList = false, string sConfig = null)
            {
                int width = 400;
                int height = 190;
                int hList = 0;
                if (bList)
                {
                    hList = 100;
                    height = height + hList;
                }
                this.Width = width;
                this.Height = height;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ShowIcon = false;
                this.Text = caption;
                this.MinimumSize = new Size(width, height);
                this.StartPosition = FormStartPosition.CenterScreen;
                int w = this.ClientRectangle.Width;
                int h = this.ClientRectangle.Height;
                this.TopMost = true;

                textLabel.Left = 10;
                textLabel.Top = 10;
                textLabel.Text = text;
                textLabel.AutoSize = true;
                textLabel.MaximumSize = new Size(width - 20, 0);

                modeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                modeLabel.Text = "";
                modeLabel.Left = w - 10 - modeLabel.Width;
                modeLabel.Top = 10;
                modeLabel.AutoSize = true;
                modeLabel.Click += new EventHandler(on_mode);
                modeLabel.Visible = false;

                accept.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                accept.Text = "Ok";
                accept.Width = 100;
                accept.Left = w - 2 * (10 + 100);
                accept.Top = h - 10 - 22;
                accept.AutoSize = true;
                accept.Top = h - 10 - accept.Height;
                accept.DialogResult = DialogResult.OK;
                accept.Click += new EventHandler(on_close);

                cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancel.Text = "Cancel";
                cancel.Width = 100;
                cancel.Left = w - 10 - 100;
                cancel.Top = h - 10 - cancel.Height;
                cancel.DialogResult = DialogResult.Cancel;
                cancel.Click += new EventHandler(on_close);

                config.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                config.Text = "Abort";
                config.Width = 100;
                config.Left = 10;
                config.Top = h - 10 - cancel.Height;
                config.DialogResult = DialogResult.Abort;
                config.Click += new EventHandler(on_close);

                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left
                    | AnchorStyles.Right;
                textBox.Width = w - 10 * 2;
                textBox.Left = 10;
                textBox.AutoSize = true;
                textBox.Top = 32;

                txt1Label.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                txt1Label.Left = 10;
                txt1Label.Top = h - 7 * 10 + 5 - accept.Height;
                txt1Label.Text = "";
                txt1Label.AutoSize = true;
                txt1Label.MaximumSize = new Size(width - 20, 0);

                txt2Label.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                txt2Label.Left = 10;
                txt2Label.Top = h - 5 * 10 + 10 - accept.Height;
                txt2Label.Text = "";
                txt2Label.AutoSize = true;
                txt2Label.MaximumSize = new Size(width - 20, 0);

                comboBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                comboBox.Left = 10;
                comboBox.Top = h - 10 - accept.Height;
                comboBox.AutoSize = true;
                comboBox.Visible = false;

                if (bList)
                {
                    listBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
                        | AnchorStyles.Left | AnchorStyles.Right;
                    listBox.Width = w - 10 * 2;
                    listBox.Height = hList;
                    listBox.Left = 10;
                    listBox.Top = 60;
                    listBox.AutoSize = true;
                    listBox.Visible = true;
                    listBox.SelectedIndexChanged += new EventHandler(on_changed);
                    listBox.DoubleClick += new EventHandler(on_ok_close);
                }

                this.Controls.Add(textBox);
                this.Controls.Add(txt1Label);
                this.Controls.Add(txt2Label);
                this.Controls.Add(comboBox);
                if (bList)
                {
                    this.Controls.Add(listBox);
                }
                if (null != sConfig)
                {
                    if (sConfig.Length > 0) config.Text = sConfig;
                    this.Controls.Add(config);
                }
                this.Controls.Add(accept);
                this.Controls.Add(cancel);
                this.Controls.Add(textLabel);
                this.Controls.Add(modeLabel);
                this.AcceptButton = accept;
                this.CancelButton = cancel;
            }

            void on_ok_close(Object sender, EventArgs e)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            void on_close(Object sender, EventArgs e)
            {
                this.Close();
            }

            void on_changed(Object sender, EventArgs e)
            {
                this.textBox.Text = this.listBox.Text;
            }


            /////////////////////////////////////////////////////////////////////

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Value
            {
                get
                {
                    return textBox.Text;
                }
                set
                {
                    textBox.Text = value;
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Text1
            {
                set
                {
                    txt1Label.Text = value;
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Text2
            {
                set
                {
                    txt2Label.Text = value;
                }
            }

            /////////////////////////////////////////////////////////////////////

            int mode;
            public List<string> ModeList = new List<string>();

            void on_mode(Object sender, EventArgs e)
            {
                if (ModeList.Count > 0)
                {

                    ModeIndex = (mode + 1) % ModeList.Count;
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int ModeIndex
            {
                set
                {
                    if (value < ModeList.Count)
                    {
                        mode = value;
                        int w = ClientRectangle.Width;
                        modeLabel.Text = ModeList[mode];
                        modeLabel.Left = w - 10 - modeLabel.Width;
                    }
                }
            }

            /////////////////////////////////////////////////////////////////////

            public void AddText(string s)
            {
                textBox.Items.Add(s);
                if (s == "") return;
                if (listBox.Items.Contains(s)) return;
                listBox.Items.Add(s);
            }

            public void AddListItem(string s)
            {
                listBox.Items.Add(s);
            }

            public void UpdateList(string kw, string val)
            {
                foreach (var v in Config.GetValues(kw))
                {
                    AddListItem(v);
                }
                AddText("");
                var flg = false;
                foreach (var v in Config.GetValues(kw + @"\recent"))
                {
                    if (v != val) flg = true;
                }
                if (!flg)
                {
                    if (val != null)
                    {
                        if (val.Length > 0) AddText(val);
                    }
                }
                foreach (var v in Config.GetValues(kw + @"\recent"))
                {
                    AddText(v);
                }
            }

            /////////////////////////////////////////////////////////////////////

            public void AddFormatType(string s)
            {
                if (comboBox.Items.Count == 1)
                {
                    comboBox.Visible = true;
                    comboBox.SelectedIndex = 0;
                }
                comboBox.Items.Add(s);
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string FormatType
            {
                get
                {
                    return comboBox.Text;
                }
                set
                {
                    comboBox.Text = value;
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int FormatTypeIndex
            {
                set
                {
                    comboBox.SelectedIndex = value;
                }
            }
        }
    }
}
