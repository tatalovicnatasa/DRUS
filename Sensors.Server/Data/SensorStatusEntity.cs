using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sensors.Contracts;
using System.Threading.Tasks;

namespace Sensors.Server.Data
{

    // Zahtev 2: server ovde pamti da li je svaki senzor trenutno aktivan
    // ili standby, i kad je poslednji put javio heartbeat.
    public class SensorStatusEntity
    {
        public int SensorId { get; set; }          // NIJE primarni kljuc <ClassId> = primarni kljuc 
        public bool IsActive { get; set; }
        public DateTime LastHeartbeatUtc { get; set; }
    }
   
}
