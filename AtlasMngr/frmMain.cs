using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtlasMngr
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void tsBtnTickets_Click(object sender, EventArgs e)
        {
            var frm = new frmTickets();
            frm.MdiParent = this;
            frm.Show();
        }
    }
}
