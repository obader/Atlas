using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Atlas.Core.Logic;

namespace AtlasMngr
{
    public partial class frmTickets : Form
    {
        public frmTickets()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            LoadTickets();
        }

        private void LoadTickets()
        {
            lvTickets.Items.Clear();
            var logic = new TicketingLogic("192.168.2.50", "AtlasDB", "sa", "P@ssw0rd", false);
            var tickets = logic.GetTickets("Al", dtpFromDate.Value, dtpToDate.Value, 1, 0, 0, null);
            if (tickets == null)
            {
                lvTickets.Items.Clear();
                return;
            }
            foreach (var masterTicket in tickets)
            {
                var lvi = new ListViewItem {Text = masterTicket.Ticket.Id.ToString()};
                lvi.SubItems.Add(masterTicket.Ticket.Title);
                var status = masterTicket.GetCurrentStatus();
                if (status == null)continue;
                lvi.SubItems.Add(status.StatusDescription);
                lvi.SubItems.Add(masterTicket.Ticket.CreationDate.ToString("u"));
                lvi.SubItems.Add(masterTicket.LastModifieDate.ToString("u"));
                lvi.Tag = masterTicket.Ticket.Id;
                lvTickets.Items.Add(lvi);
            }
        }

        private void btnNewTicket_Click(object sender, EventArgs e)
        {
            var lFrm = new frmTicket(null);
            lFrm.ShowDialog();
        }

        private void lvTickets_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var ticketId = (long)lvTickets.SelectedItems[0].Tag;
            var lFrm = new frmTicket(ticketId);
            lFrm.ShowDialog();
        }
    }
}
