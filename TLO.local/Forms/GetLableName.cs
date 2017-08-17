using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLO.local.Forms
{
    public partial class GetLableName : Form
    {
        internal string Value { get { return _txtLabel.Text; } set { _txtLabel.Text = value; } }

        public GetLableName()
        {
            InitializeComponent();
        }

        private void btClick(object sender, EventArgs e)
        {
            if (sender == btCancel)
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
                Close();
            }
            else if (sender == btOk)
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;

            }
        }
    }
}
