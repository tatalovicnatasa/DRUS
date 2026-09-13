using Sensors.Contracts;
using System;


namespace Sensors.Server.Data
{
    // Bazna klasa - svih 10 Sensor{N}Reading tabela imaju identicnu strukturu
    // pa zajednicka polja izdvajamo ovde da ih ne ponavljamo 10 puta 
    public abstract class BaseReading
    {
        public int Id { get; set; }               // EF automatski prepoznaje "Id" kao primary key (konvencija)
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public ReadingKind Kind { get; set; }      // Raw ili Consensus flag
        public DataQuality Quality { get; set; }   // zahtev 1: kvalitet podatka; zahtev 3: GOOD-only filter
        public AlarmPriority Alarm { get; set; }   // zahtev 1: prioritet alarma
    }
}
