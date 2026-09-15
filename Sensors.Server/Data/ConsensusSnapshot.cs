using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensors.Contracts;

namespace Sensors.Server.Data
{
    public class ConsensusSnapshot
    {
        // Zahtev 3: evidencija SVAKE konsenzus odluke - koja vrednost je izabrana,
        // sa kog senzora, kakav je bio prosek u tom trenutku. 
        public int Id { get; set; }
        public DateTime SnapshotTime { get; set; }
        public double Value { get; set; }
        public int SourceSensorId { get; set; } // senzor koji je dao vrednost koja je izabrana za konsenzus
        public double AverageAtDecision { get; set; } // prosek svih senzora u trenutku kad je doneta odluka
        public double ToleranceUsed { get; set; } // tolerancija koja je bila u upotrebi u trenutku kad je doneta odluka
    }
}
