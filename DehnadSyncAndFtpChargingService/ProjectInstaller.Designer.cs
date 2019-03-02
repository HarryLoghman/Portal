namespace DehnadSyncAndFtpChargingService
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
            this.DehnadSyncAndFtpChargingServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadSyncAndFtpChargingServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadSyncAndFtpChargingServiceProcessInstaller
            // 
            this.DehnadSyncAndFtpChargingServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadSyncAndFtpChargingServiceProcessInstaller.Password = null;
            this.DehnadSyncAndFtpChargingServiceProcessInstaller.Username = null;
            // 
            // DehnadSyncAndFtpChargingServiceInstaller
            // 
            this.DehnadSyncAndFtpChargingServiceInstaller.Description = "MCI Ftp Charging";
            this.DehnadSyncAndFtpChargingServiceInstaller.DisplayName = "MCI Ftp Charging";
            this.DehnadSyncAndFtpChargingServiceInstaller.ServiceName = "DehnadSyncAndFtpChargingService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadSyncAndFtpChargingServiceProcessInstaller,
            this.DehnadSyncAndFtpChargingServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadSyncAndFtpChargingServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadSyncAndFtpChargingServiceInstaller;
    }
}