using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// added System.ServiceModel reference

namespace Sensors.Contracts
{
 
    public enum DataQuality // Only GOOD goes to Concensus, each sensor has a quality type  
    {
        GOOD = 0,
        BAD = 1,
        UNCERTAIN = 2
    }
    public enum AlarmPriority // None if there is no priority
    {
        None = 0,
        Priority1 = 1,
        Priority2 = 2,
        Priority3 = 3
    }
    public enum SensorRole
    {
        Active,   // 
        Standby,  // 
        Dead      // 
    }

}
