using System;
using System.Globalization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Sensors.Contracts;

namespace Sensors.Client
{
    class Program
    {
        //static ISensorService _proxy; // WCF proxy za komunikaciju sa serverom
        static readonly bool[] _sleeping = new bool[11];  // indeks 1..10, [0] se ne koristi
        static readonly bool[] _isActive = new bool[11];
        static readonly Random _rng = new Random(); // za simulaciju random vremena cekanja i vrednosti senzora
        static readonly object _rngLock = new object();

        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            //var binding = new NetTcpBinding(); // Koristimo NetTcpBinding za WCF komunikaciju
            //var address = new EndpointAddress("net.tcp://localhost:9001/SensorService"); //
            //var factory = new ChannelFactory<ISensorService>(binding, address);
            //
            //_proxy = factory.CreateChannel();

            for (int i = 1; i <= 5; i++) _isActive[i] = true;   // S1-S5 pocinju aktivni
            for (int i = 6; i <= 10; i++) _isActive[i] = false; // S6-S10 pocinju standby

            Console.WriteLine("Simulator started. S1-S5 aktivni, S6-S10 standby.");
            Console.WriteLine("Komande: 'sleep id' za simulaciju pada, prazan red za izlaz.\n");

            for (int i = 1; i <= 10; i++)
            {
                int sid = i;
                // Svaki senzor pravi SVOJ NEZAVISAN proxy/kanal - ne deli se vise!
                var binding = new NetTcpBinding();
                var address = new EndpointAddress("net.tcp://localhost:9001/SensorService");
                var factory = new ChannelFactory<ISensorService>(binding, address);
                ISensorService myProxy = factory.CreateChannel();

                _ = Task.Run(() => SensorLoop(sid, myProxy));
                _ = Task.Run(() => HeartbeatLoop(sid, myProxy));
            }


            string line;
            while ((line = Console.ReadLine()) != "")
            {
                var parts = line?.Trim().Split(' ');
                if (parts != null && parts.Length == 2
                    && parts[0].Equals("sleep", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out var id) && id >= 1 && id <= 10)
                {
                    _sleeping[id] = true;
                    Console.WriteLine($"[S{id}] Simuliram pad na 20s...");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(20000);
                        _sleeping[id] = false;
                        Console.WriteLine($"[S{id}] Budim se posle sleep-a.");
                    });
                }
                else
                {
                    Console.WriteLine("Nepoznata komanda. Koristi: sleep id");
                }
            }
        }
        static int NextInt(int minInclusive, int maxExclusive)
        {
            lock (_rngLock)
            {
                return _rng.Next(minInclusive, maxExclusive);
            }
        }
        static double NextDouble()
        {
            lock (_rngLock)
            {
                return _rng.NextDouble();
            }
        }
        // Glavna petlja jednog senzora - generise vrednost, proverava alarm, salje.
        static async Task SensorLoop(int sensorId, ISensorService proxy)
        {
            long messageId = 0;
            double min = 10, max = 35;
            double alarm1 = 26, alarm2 = 31, alarm3 = 32;
            var quality = sensorId == 4 ? DataQuality.UNCERTAIN : DataQuality.GOOD; // S4 namerno UNCERTAIN, za demo GOOD-filtera

            while (true)
            {
                try
                {
                    int secs = NextInt(1, 11); // 1-10s
                    await Task.Delay(TimeSpan.FromSeconds(secs));

                    if (_sleeping[sensorId]) continue;

                    // Zahtev 2: pitaj server da li sam trenutno aktivan (polling, zamena za duplex)
                    try { _isActive[sensorId] = await proxy.AmIActiveAsync(sensorId); }
                    catch { /* server privremeno nedostupan, ostani na poslednjoj poznatoj vrednosti */ }

                    if (!_isActive[sensorId]) continue; // standby ne salje ocitavanja

                    double value = min + NextDouble() * (max - min);
                    var alarmLevel = ComputeAlarm(value, alarm1, alarm2, alarm3);

                    PrintReading(sensorId, value, alarmLevel);

                    messageId++;
                    var timestamp = DateTime.UtcNow;
                    var payload = $"{value.ToString(CultureInfo.InvariantCulture)};{quality}";
                    var encrypted = CryptoHelper.Encrypt(payload);
                    //Console.WriteLine($"[DEMO] Original: \"{payload}\" -> Sifrovano (hex): {BitConverter.ToString(encrypted).Replace("-", "")}");
                    var signature = CryptoHelper.Sign(encrypted, messageId, timestamp);
                    await proxy.SubmitReadingAsync(sensorId, encrypted, signature, messageId, timestamp, alarmLevel);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[S{sensorId}] NEOCEKIVANA GRESKA u petlji: {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(2000); // kratka pauza pre sledeceg pokusaja, da ne "spami" konzolu

                }
            }
        }

        static void PrintReading(int sensorId, double value, AlarmPriority alarm)
        {
            if (alarm == AlarmPriority.None)
            {
                Console.WriteLine($"S{sensorId} -> {value:F2} at {DateTime.UtcNow:HH:mm:ss}");
                return;
            }

            var prev = Console.ForegroundColor;
            Console.ForegroundColor = alarm == AlarmPriority.Priority1 ? ConsoleColor.Yellow
                                     : alarm == AlarmPriority.Priority2 ? ConsoleColor.DarkYellow
                                     : ConsoleColor.Red;
            Console.WriteLine($"[S{sensorId}] ALARM {(int)alarm}: {value:F2} at {DateTime.UtcNow:HH:mm:ss}");
            Console.ForegroundColor = prev;
        }

        // Zahtev 2: SVIH 10 senzora salje heartbeat na 5s, bez obzira da li su aktivni.
        static async Task HeartbeatLoop(int sensorId, ISensorService proxy)
        {
            while (true)
            {
                try
                {
                    if (!_sleeping[sensorId])
                    {
                        await proxy.HeartbeatAsync(sensorId, _isActive[sensorId]);
                    }
                
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"[S{sensorId}] Heartbeat greska: {ex.Message}"); 
                }
                await Task.Delay(5000);

            }
        }

        static AlarmPriority ComputeAlarm(double value, double a1, double a2, double a3)
        {
            if (value >= a3) return AlarmPriority.Priority3;
            if (value >= a2) return AlarmPriority.Priority2;
            if (value >= a1) return AlarmPriority.Priority1;
            return AlarmPriority.None;
        }
    }
}