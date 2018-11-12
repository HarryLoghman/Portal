namespace DehnadAcharService
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
            this.DehnadAcharServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DehnadAcharServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DehnadAcharServiceProcessInstaller
            // 
            this.DehnadAcharServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DehnadAcharServiceProcessInstaller.Password = null;
            this.DehnadAcharServiceProcessInstaller.Username = null;
            // 
            // DehnadAcharServiceInstaller
            // 
            this.DehnadAcharServiceInstaller.Description = "DehnadAcharService";
            this.DehnadAcharServiceInstaller.ServiceName = "DehnadAcharService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DehnadAcharServiceProcessInstaller,
            this.DehnadAcharServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DehnadAcharServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DehnadAcharServiceInstaller;
    }
}