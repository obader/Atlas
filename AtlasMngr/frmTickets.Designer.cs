namespace AtlasMngr
{
    partial class frmTickets
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
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnSearch = new System.Windows.Forms.ToolStripButton();
            this.btnNewTicket = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.spSearch = new System.Windows.Forms.SplitContainer();
            this.label3 = new System.Windows.Forms.Label();
            this.sbxStatus = new System.Windows.Forms.ComboBox();
            this.gbxPeriod = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpToDate = new System.Windows.Forms.DateTimePicker();
            this.dtpFromDate = new System.Windows.Forms.DateTimePicker();
            this.lbxApplications = new System.Windows.Forms.CheckedListBox();
            this.lvTickets = new System.Windows.Forms.ListView();
            this.chTicketId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chCreatedDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLastChangedDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spSearch)).BeginInit();
            this.spSearch.Panel1.SuspendLayout();
            this.spSearch.Panel2.SuspendLayout();
            this.spSearch.SuspendLayout();
            this.gbxPeriod.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 490);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(926, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnSearch,
            this.btnNewTicket});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(926, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnSearch
            // 
            this.btnSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSearch.Image = global::AtlasMngr.Properties.Resources.Search;
            this.btnSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(23, 22);
            this.btnSearch.Text = "Search";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // btnNewTicket
            // 
            this.btnNewTicket.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnNewTicket.Image = global::AtlasMngr.Properties.Resources.addticket;
            this.btnNewTicket.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnNewTicket.Name = "btnNewTicket";
            this.btnNewTicket.Size = new System.Drawing.Size(23, 22);
            this.btnNewTicket.Text = "New";
            this.btnNewTicket.Click += new System.EventHandler(this.btnNewTicket_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.spSearch);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lvTickets);
            this.splitContainer1.Size = new System.Drawing.Size(926, 465);
            this.splitContainer1.SplitterDistance = 264;
            this.splitContainer1.TabIndex = 2;
            // 
            // spSearch
            // 
            this.spSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spSearch.Location = new System.Drawing.Point(0, 0);
            this.spSearch.Name = "spSearch";
            this.spSearch.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // spSearch.Panel1
            // 
            this.spSearch.Panel1.Controls.Add(this.label3);
            this.spSearch.Panel1.Controls.Add(this.sbxStatus);
            this.spSearch.Panel1.Controls.Add(this.gbxPeriod);
            // 
            // spSearch.Panel2
            // 
            this.spSearch.Panel2.Controls.Add(this.lbxApplications);
            this.spSearch.Size = new System.Drawing.Size(264, 465);
            this.spSearch.SplitterDistance = 164;
            this.spSearch.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Status";
            // 
            // sbxStatus
            // 
            this.sbxStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sbxStatus.FormattingEnabled = true;
            this.sbxStatus.Location = new System.Drawing.Point(70, 87);
            this.sbxStatus.Name = "sbxStatus";
            this.sbxStatus.Size = new System.Drawing.Size(121, 21);
            this.sbxStatus.TabIndex = 2;
            // 
            // gbxPeriod
            // 
            this.gbxPeriod.Controls.Add(this.label2);
            this.gbxPeriod.Controls.Add(this.label1);
            this.gbxPeriod.Controls.Add(this.dtpToDate);
            this.gbxPeriod.Controls.Add(this.dtpFromDate);
            this.gbxPeriod.Location = new System.Drawing.Point(12, 3);
            this.gbxPeriod.Name = "gbxPeriod";
            this.gbxPeriod.Size = new System.Drawing.Size(192, 78);
            this.gbxPeriod.TabIndex = 1;
            this.gbxPeriod.TabStop = false;
            this.gbxPeriod.Text = "Period";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "To";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "From";
            // 
            // dtpToDate
            // 
            this.dtpToDate.CustomFormat = "dd-MM-yyyy HH:mm";
            this.dtpToDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpToDate.Location = new System.Drawing.Point(46, 45);
            this.dtpToDate.Name = "dtpToDate";
            this.dtpToDate.Size = new System.Drawing.Size(133, 20);
            this.dtpToDate.TabIndex = 1;
            // 
            // dtpFromDate
            // 
            this.dtpFromDate.CustomFormat = "dd-MM-yyyy HH:mm";
            this.dtpFromDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFromDate.Location = new System.Drawing.Point(46, 19);
            this.dtpFromDate.Name = "dtpFromDate";
            this.dtpFromDate.Size = new System.Drawing.Size(133, 20);
            this.dtpFromDate.TabIndex = 0;
            // 
            // lbxApplications
            // 
            this.lbxApplications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbxApplications.FormattingEnabled = true;
            this.lbxApplications.Location = new System.Drawing.Point(0, 0);
            this.lbxApplications.Name = "lbxApplications";
            this.lbxApplications.Size = new System.Drawing.Size(264, 297);
            this.lbxApplications.TabIndex = 1;
            // 
            // lvTickets
            // 
            this.lvTickets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chTicketId,
            this.chTitle,
            this.chStatus,
            this.chCreatedDate,
            this.chLastChangedDate});
            this.lvTickets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvTickets.FullRowSelect = true;
            this.lvTickets.GridLines = true;
            this.lvTickets.Location = new System.Drawing.Point(0, 0);
            this.lvTickets.MultiSelect = false;
            this.lvTickets.Name = "lvTickets";
            this.lvTickets.Size = new System.Drawing.Size(658, 465);
            this.lvTickets.TabIndex = 0;
            this.lvTickets.UseCompatibleStateImageBehavior = false;
            this.lvTickets.View = System.Windows.Forms.View.Details;
            this.lvTickets.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvTickets_MouseDoubleClick);
            // 
            // chTicketId
            // 
            this.chTicketId.Text = "Ticket Id";
            this.chTicketId.Width = 111;
            // 
            // chTitle
            // 
            this.chTitle.Text = "Title";
            this.chTitle.Width = 109;
            // 
            // chStatus
            // 
            this.chStatus.Text = "Status";
            this.chStatus.Width = 138;
            // 
            // chCreatedDate
            // 
            this.chCreatedDate.Text = "CreatedDate";
            this.chCreatedDate.Width = 153;
            // 
            // chLastChangedDate
            // 
            this.chLastChangedDate.Text = "LastChangedDate";
            this.chLastChangedDate.Width = 126;
            // 
            // frmTickets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(926, 512);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Name = "frmTickets";
            this.Text = "Tickets";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.spSearch.Panel1.ResumeLayout(false);
            this.spSearch.Panel1.PerformLayout();
            this.spSearch.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spSearch)).EndInit();
            this.spSearch.ResumeLayout(false);
            this.gbxPeriod.ResumeLayout(false);
            this.gbxPeriod.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripButton btnSearch;
        private System.Windows.Forms.SplitContainer spSearch;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox sbxStatus;
        private System.Windows.Forms.GroupBox gbxPeriod;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpToDate;
        private System.Windows.Forms.DateTimePicker dtpFromDate;
        private System.Windows.Forms.CheckedListBox lbxApplications;
        private System.Windows.Forms.ListView lvTickets;
        private System.Windows.Forms.ColumnHeader chTicketId;
        private System.Windows.Forms.ColumnHeader chTitle;
        private System.Windows.Forms.ColumnHeader chStatus;
        private System.Windows.Forms.ColumnHeader chCreatedDate;
        private System.Windows.Forms.ColumnHeader chLastChangedDate;
        private System.Windows.Forms.ToolStripButton btnNewTicket;
    }
}