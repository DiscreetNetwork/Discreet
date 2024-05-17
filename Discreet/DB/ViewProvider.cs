using Discreet.DB;
using Discreet.Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public abstract class ViewProvider
    {
        private static IView defaultProvider;

        static ViewProvider()
        {
            defaultProvider = (Daemon.DaemonConfig.GetConfig().Sandbox.Value ? DataView.GetView() : SandboxView.GetView());
        }

        public static IView GetDefaultProvider() => defaultProvider;
    }
}
