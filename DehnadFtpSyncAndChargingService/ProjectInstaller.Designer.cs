namespace DehnadFtpSyncAndChargingService
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
            this.DehnadFtpSyncAndChargingServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadFtpSyncAndChargingServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadFtpSyncAndChargingServiceProcessInstaller
            // 
            this.DehnadFtpSyncAndChargingServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadFtpSyncAndChargingServiceProcessInstaller.Password = null;
            this.DehnadFtpSyncAndChargingServiceProcessInstaller.Username = null;
            // 
            // DehnadFtpSyncAndChargingServiceInstaller
            // 
            this.DehnadFtpSyncAndChargingServiceInstaller.Description = "Ftp Sync And Charging";
            this.DehnadFtpSyncAndChargingServiceInstaller.DisplayName = "DehnadFtpSyncAndChargingService";
            this.DehnadFtpSyncAndChargingServiceInstaller.ServiceName = "DehnadFtpSyncAndChargingService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadFtpSyncAndChargingServiceProcessInstaller,
            this.DehnadFtpSyncAndChargingServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadFtpSyncAndChargingServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadFtpSyncAndChargingServiceInstaller;
    }
}