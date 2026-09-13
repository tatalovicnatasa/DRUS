using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensors.Server.Data
{
    // dodati da svaka vrednost ima flag koji označava da li je konsenzus vrednost
    // ReadingKind.Raw/ReadingKind.Consensus
    public enum ReadingKind
    {
        Raw = 0,
        Consensus = 1
    }
}
