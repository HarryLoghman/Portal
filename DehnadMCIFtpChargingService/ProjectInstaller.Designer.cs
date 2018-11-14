namespace DehnadMCIFtpChargingService
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
            this.DehnadMCIFtpChargingServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadMCIFtpChargingServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadMCIFtpChargingServiceProcessInstaller
            // 
            this.DehnadMCIFtpChargingServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadMCIFtpChargingServiceProcessInstaller.Password = null;
            this.DehnadMCIFtpChargingServiceProcessInstaller.Username = null;
            // 
            // DehnadMCIFtpChargingServiceInstaller
            // 
            this.DehnadMCIFtpChargingServiceInstaller.Description = "MCI Ftp Charging";
            this.DehnadMCIFtpChargingServiceInstaller.DisplayName = "MCI Ftp Charging";
            this.DehnadMCIFtpChargingServiceInstaller.ServiceName = "DehnadMCIFtpChargingService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadMCIFtpChargingServiceProcessInstaller,
            this.DehnadMCIFtpChargingServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadMCIFtpChargingServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadMCIFtpChargingServiceInstaller;
    }
}