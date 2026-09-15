using Sensors.Contracts; // sto je ovako podulano 
using System;
using System.Collections.Generic;
using System.ServiceModel; // methods 
using System.Threading.Tasks; //  Task, Task <T> return values

// implementing contract interface and DTO classes that share Server and Client

// Ovo je SERVISNI INTERFEJS - sta server nudi klijentima


namespace Sensors.Contracts
{
    
    [ServiceContract] // Allows clients to call it 
    public interface ISensorService 
    {
        // zahtev 1 - prijem ocitavanja
        [OperationContract]
        Task<bool> SubmitReadingAsync(
            int sensorId,               // identifikuje senzor - ko je poslao poruku ? 
            byte[] encryptedPayload,   // sad sadrzi IV + sifrat zajedno
            byte[] signature,         // potpis
            long messageId,
            DateTime timestampUtc, // pre defisrovanja nam trebaju 
            AlarmPriority alarm); // alarm polje nije sifrovano namerno 

        [OperationContract]
        Task<double?> GetLatestConsensusAsync();

        [OperationContract]
        Task<Dictionary<int, double?>> GetLatestRawValuesAsync();

        [OperationContract]
        Task<AlarmReportEntry[]> GetAlarmReportAsync(); // AlarmReportEntry[] je niz objekata koji sadrze podatke o alarmima

        [OperationContract]
        Task HeartbeatAsync(int sensorId, bool isCurrentlyActive);

        [OperationContract]
        Task<bool> AmIActiveAsync(int sensorId);
    }
    // SensorReading
    public class AlarmReportEntry // prenos podataka izmedju servera i klik
    {
        // No need to implement DataContract, all public properties
        public int SensorId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public AlarmPriority Alarm { get; set; }
    }
}
