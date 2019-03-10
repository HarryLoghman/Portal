namespace DehnadMonitoringService
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
            this.DehnadMonitoringServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadMonitoringServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadMonitoringServiceProcessInstaller
            // 
            this.DehnadMonitoringServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadMonitoringServiceProcessInstaller.Password = null;
            this.DehnadMonitoringServiceProcessInstaller.Username = null;
            // 
            // DehnadMonitoringServiceInstaller
            // 
            this.DehnadMonitoringServiceInstaller.Description = "Monitors all actions of dehnad processes";
            this.DehnadMonitoringServiceInstaller.DisplayName = "DehnadMonitoringService";
            this.DehnadMonitoringServiceInstaller.ServiceName = "DehnadMonitoringService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadMonitoringServiceProcessInstaller,
            this.DehnadMonitoringServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadMonitoringServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadMonitoringServiceInstaller;
    }
}