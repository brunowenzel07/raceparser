namespace XMLFeedParser
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
            this.XMLFeedParserProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.XMLFeedParserInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // XMLFeedParserProcessInstaller
            // 
            this.XMLFeedParserProcessInstaller.Password = null;
            this.XMLFeedParserProcessInstaller.Username = null;
            // 
            // XMLFeedParserInstaller
            // 
            this.XMLFeedParserInstaller.ServiceName = "XMLFeedParser";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.XMLFeedParserProcessInstaller,
            this.XMLFeedParserInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller XMLFeedParserProcessInstaller;
        private System.ServiceProcess.ServiceInstaller XMLFeedParserInstaller;
    }
}