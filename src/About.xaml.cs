using System;
using System.Deployment.Application;
using System.Reflection;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace SongFinder
{
    public partial class About : MetroWindow
    {
        public About()
        {
            InitializeComponent();
            Version.Content = "Version: " + GetRunningVersion() + " vom Oktober 2018";
        }
        
        /// <summary>
        /// Function to get the Version from current program
        /// </summary>
        /// <returns>The Version</returns>
        private static string GetRunningVersion()
        {
            try
            {
                var completeVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                return completeVersion.Major + "." + completeVersion.Minor;
            }
            catch (Exception)
            {
                var completeVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return completeVersion.Major + "." + completeVersion.Minor; 
            }
        }
    }
}
