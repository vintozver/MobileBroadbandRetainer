using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MobileBroadbandPersistence
{
    public partial class Service : ServiceBase
    {
        WwanConnectivityRetainer retainer = null;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Debug.Assert(retainer == null);
            retainer = new WwanConnectivityRetainer();
        }

        protected override void OnStop()
        {
            retainer.Dispose();
            retainer = null;
        }
    }
}
