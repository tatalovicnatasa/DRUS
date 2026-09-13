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
        // sa kog senzora, kakav je bio prosek u tom trenutku. Ovo nije strogo
        // obavezno po specifikaciji, ali ti daje transparentnost za odbranu
        // ("evo dokaza da je algoritam pravilno birao vrednosti kroz vreme").
        public int Id { get; set; }
        public DateTime SnapshotTime { get; set; }
        public double Value { get; set; }
        public int SourceSensorId { get; set; }
        public double AverageAtDecision { get; set; }
        public double ToleranceUsed { get; set; }
    }
}
