using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MobileBroadbandPersistence
{
    [RunInstaller(true)]
    public class ThisServiceInstaller : Installer
    {
        public ThisServiceInstaller()
        {
            using (ServiceProcessInstaller procInstaller = new ServiceProcessInstaller())
            {
                procInstaller.Account = ServiceAccount.LocalSystem;
                using (ServiceInstaller installer = new ServiceInstaller())
                {
                    installer.StartType = ServiceStartMode.Automatic;
                    installer.DelayedAutoStart = true;
                    installer.ServiceName = "MobileBroadbandPersistence";
                    installer.DisplayName = "Mobile Broadband Persistence";
                    installer.Description = "Reconnetcs all mobile broadband connections whenever possible (after connectivity is lost, manually disconnected, provisional action). Takes a timeout before retrying.";

                    this.Installers.Add(procInstaller);
                    this.Installers.Add(installer);
                }
            }
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
