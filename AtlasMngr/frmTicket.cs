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
using Atlas.Core.Logic.Aggregates;
using Atlas.Core.Logic.Entities;
using AtlasMngr.Classes;
using AtlasMngr.WCFProfile;
using TicketStatus = Atlas.Core.Logic.ValueObjects.TicketStatusModel;

namespace AtlasMngr
{
    public partial class frmTicket : Form
    {
        
        TicketingLogic logic = new TicketingLogic("192.168.2.50", "AtlasDB","sa","P@ssw0rd",false );
        private long? _ticketId;
        private MasterTicket _ticket;
        public frmTicket(long? pTicketId)
        {
            InitializeComponent();
            lblStatusValue.Text = "Created";
            if (pTicketId == null)
                return;
            _ticketId = pTicketId;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            var appId = (long)((ComboboxItem) cbxApplication.SelectedItem).Value;
            var categoryId = (long)((ComboboxItem)cbxCategory.SelectedItem).Value;
            var bankId = (int)((ComboboxItem)cbxBanks.SelectedItem).Value;
            long? profileId = null;
            long? customerId = null;

            long priorityId = (long)((ComboboxItem)cbxPriority.SelectedItem).Value;
            long? departmentId = null;
            if(cbxDepartment.SelectedIndex != 0)
                departmentId = (long)((ComboboxItem)cbxDepartment.SelectedItem).Value;
            if (tbxCustomerName.Tag != null)
            profileId = ((WCFProfile.ProfileResponse) tbxCustomerName.Tag).Profile.ProfileInfo.ProfileId;
            if (_ticketId == null)
                logic.AddNewTicket(
                    new Ticket(-1, -1, "Al", bankId, profileId, customerId, tbxTitle.Text, tbxIssueDescription.Text, appId, categoryId, null, -1, null, null, priorityId, departmentId, DateTime.UtcNow, DateTime.UtcNow),
                    null, null, "Initial Entry", false, false
                    );
            else
            {
                _ticket.Ticket.PriorityId = priorityId;
                _ticket.Ticket.DepartmentId = departmentId;
                _ticket.Ticket.Description = tbxIssueDescription.Text;
                logic.UpdateTicket(_ticket.Ticket, null);

            }

            this.Close();
        }

        private void LoadData()
        {

            var applications = logic.GetApplications();

            foreach (var item in applications)
            {
                var lvAppItem = new ComboboxItem
                {
                    Text = item.Value.Description,
                    Value = item.Id
                };
                cbxApplication.Items.Add(lvAppItem);
            }

            var categories = logic.GetCategories();

            foreach (var item in categories)
            {
                var lvAppItem = new ComboboxItem
                {
                    Text = item.Value.Description,
                    Value = item.Id
                };
                cbxCategory.Items.Add(lvAppItem);
            }

            var statuses = logic.GetStatuses();

            foreach (var item in statuses)
            {
                var lvAppItem = new ComboboxItem
                {
                    Text = item.StatusDescription,
                    Value = item.StatusId
                };
                cbxStatus.Items.Add(lvAppItem);
            }

            var banks = GlobalData.GetBanks();
            foreach (var item in banks)
            {
                var lvBankItem = new ComboboxItem
                {
                    Text = item.BankName,
                    Value = item.BankId
                };
                cbxBanks.Items.Add(lvBankItem);
            }


            var priorities = logic.GetPriorities();

            foreach (var item in priorities)
            {
                var lvAppItem = new ComboboxItem
                {
                    Text = item.Value.Description,
                    Value = item.Id
                };
                cbxPriority.Items.Add(lvAppItem);
            }


            var depertments = logic.GetDepartments();
            var lvAppItem1 = new ComboboxItem
            {
                Text = "Not Assigned",
                Value = -1
            };
            cbxDepartment.Items.Add(lvAppItem1);
            foreach (var item in depertments)
            {
                var lvAppItem = new ComboboxItem
                {
                    Text = item.Value.Description,
                    Value = item.Id
                };
                cbxDepartment.Items.Add(lvAppItem);
            }

            cbxDepartment.SelectedIndex = 0;
            cbxPriority.SelectedIndex = 0;
            cbxApplication.SelectedIndex = 0;
            cbxCategory.SelectedIndex = 0;
            cbxStatus.SelectedIndex = 0;
            cbxBanks.SelectedIndex = 0;
        }

        private void frmNewTicket_Load(object sender, EventArgs e)
        {
            LoadData();

            _ticket = logic.GeTicket(_ticketId??0, "Al");
            if(_ticket == null)
                return;

            var status = _ticket.GetCurrentStatus();

            lblStatusValue.Text             = status?.StatusDescription;
            //cbxApplication.SelectedValue    = _ticket.Ticket.ApplicationId;
            //cbxCategory.SelectedValue       = _ticket.Ticket.CategoryId;
            //cbxStatus.SelectedValue         = status?.StatusId;
            cbxApplication.Enabled          = false;
            cbxCategory.Enabled             = false;


            
            SelectComboBoxValue(status?.StatusId??0, ref cbxStatus);
            SelectComboBoxValue(_ticket.Ticket.CategoryId, ref cbxCategory);
            SelectComboBoxValue(_ticket.Ticket.ApplicationId, ref cbxApplication);
            SelectComboBoxValue(_ticket.Ticket.PriorityId??0, ref cbxPriority);
            SelectComboBoxValue(_ticket.Ticket.DepartmentId ?? 0, ref cbxDepartment);            
            
            tbxTitle.Text                   = _ticket.Ticket.Title;
            tbxIssueDescription.Text        = _ticket.Ticket.Description;
            lvAudits.Items.Clear();
            foreach (var comment in _ticket.Comments)
            {
                var lvItem = new ListViewItem { Text = comment.Date.ToString("G") };
                lvItem.SubItems.Add("");
                lvItem.SubItems.Add(comment.CommentEntry);
                lvItem.Tag = comment;
                lvAudits.Items.Add(lvItem);
            }
        }

        private void SelectComboBoxValue(long pValue,ref ComboBox pComboBox)
        {
            foreach(var item in pComboBox.Items)
            {
                ComboboxItem lItem = (ComboboxItem)item;
                if(Convert.ToInt64(lItem.Value) == pValue)
                {
                    pComboBox.SelectedItem = lItem;
                    break;
                }
                   
            }
        }

        private void btnAddComment_Click(object sender, EventArgs e)
        {
            if (_ticket == null) return;

            var status = _ticket.GetCurrentStatus();

            var selectedStatus = (ComboboxItem) cbxStatus.SelectedItem;
            if (status.StatusId != (long)selectedStatus.Value)
                _ticket.ChangeStatus(TicketStatus.GetStatus((long)selectedStatus.Value));

            if (string.IsNullOrWhiteSpace(tbxComment.Text)) return;

            _ticket.AddComments(tbxComment.Text);
            lvAudits.Items.Clear();
            foreach (var comment in _ticket.Comments)
            {
                var lvItem = new ListViewItem {Text = comment.Date.ToString("G")};
                lvItem.SubItems.Add("");
                lvItem.SubItems.Add(comment.CommentEntry);
                lvItem.Tag = comment;
                lvAudits.Items.Add(lvItem);
            }
        }

        private void btnFindCustomer_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(tbxMobile.Text))
            {
                using (var client = new WCFProfile.ProfileServiceClient("PinPayProfile.WCF.ProfileService.ProfileService"))
                {
                    var profile = client.GetProfile(new ProfileRequest
                    {
                        Request = new GenericRequest {RequestId = Guid.NewGuid().ToString()}
                        ,
                        Header = new Header
                        {
                            BankId = 999,
                            APIKey = "",
                            MobileNumber = tbxMobile.Text
                        }
                    });
                    if (profile.Response.ResultCode == 9999)
                    {
                        tbxCustomerName.Text = profile.Profile.ProfileInfo.FullName;
                        tbxEmail.Text = profile.Profile.ProfileInfo.Email;
                        tbxCustomerName.Tag = profile;
                    }
                    else
                    {
                        tbxCustomerName.Text = String.Empty;
                        tbxEmail.Text = String.Empty;
                        tbxCustomerName.Tag = null;
                        MessageBox.Show("Customer not found");
                    }
                }

            }
        }
    }
}
