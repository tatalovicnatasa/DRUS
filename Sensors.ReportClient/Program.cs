using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Sensors.Contracts;

namespace Sensors.ReportClient
{
    class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var binding = new NetTcpBinding();
            var address = new EndpointAddress("net.tcp://localhost:9001/SensorService");
            var factory = new ChannelFactory<ISensorService>(binding, address);
            var proxy = factory.CreateChannel();

            var report = await proxy.GetAlarmReportAsync();

            Console.WriteLine("=== IZVESTAJ ALARMA (sortirano po prioritetu) ===");
            foreach (var entry in report)
                Console.WriteLine($"[P{(int)entry.Alarm}] S{entry.SensorId} -> {entry.Value:F2} @ {entry.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("\n=== POSLEDNJA OCITAVANJA PO SENZORU ===");
            var raw = await proxy.GetLatestRawValuesAsync();
            foreach (var kv in raw)
                Console.WriteLine($"S{kv.Key} -> {(kv.Value.HasValue ? kv.Value.Value.ToString("F2") : "nema podataka")}");

            //poslednji konsenzus
            var consensus = await proxy.GetLatestConsensusAsync();
            Console.WriteLine($"\n=== POSLEDNJI KONSENZUS ===\n{(consensus.HasValue ? consensus.Value.ToString("F2") : "jos nema konsenzusa")}");


            Console.WriteLine("\nPritisni ENTER za izlaz.");
            Console.ReadLine();
        }
    }
}