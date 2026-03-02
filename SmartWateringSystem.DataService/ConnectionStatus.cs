using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartWateringSystem.DataService
{
    public enum EConnectionStatus
    {
        Disconnected,   // Never connected or explicitly closed
        Connecting,     // Attempting to establish connection
        Connected,      // Communicating successfully
        Lost            // Was connected, now failing (different from never connected)
    }
}
