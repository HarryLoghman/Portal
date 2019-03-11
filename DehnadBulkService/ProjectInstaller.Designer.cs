namespace DehnadBulkService
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DehnadBulkServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadBulkServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadBulkServiceProcessInstaller
            // 
            this.DehnadBulkServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadBulkServiceProcessInstaller.Password = null;
            this.DehnadBulkServiceProcessInstaller.Username = null;
            // 
            // DehnadBulkServiceInstaller
            // 
            this.DehnadBulkServiceInstaller.Description = "Service to Control and Send BulkMessages";
            this.DehnadBulkServiceInstaller.DisplayName = "DehnadBulkService";
            this.DehnadBulkServiceInstaller.ServiceName = "DehnadBulkService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadBulkServiceProcessInstaller,
            this.DehnadBulkServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadBulkServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadBulkServiceInstaller;
    }
}