using Sensors.Contracts;
using Sensors.Server.Data;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Sensors.Server
{
    class Program
    {
        static Timer _reconcileTimer;
        static Timer _failoverTimer;
        static TimeSpan _reconcileEvery;
        static double _tolerance;
        static SensorService _serviceInstance;

        // Standardni obrazac: Main ostaje "obican" (static void), a sva
        // asinhrona logika je u MainAsync koju ovde sacekamo da zavrsi.
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            // Ensure DB exists (Code-First: EF sam pravi bazu/tabele ako ne postoje)
            Database.SetInitializer(new CreateDatabaseIfNotExists<SensorsDbContext>());

            _tolerance = ReadDouble("Tolerance", 5.0);
            _reconcileEvery = TimeSpan.FromSeconds(ReadInt("ReconcileIntervalSeconds", 60));

            _serviceInstance = new SensorService();

            var baseAddress = new Uri("net.tcp://localhost:9001/SensorService");
            using (var host = new ServiceHost(_serviceInstance, baseAddress))
            {
                // InstanceContextMode.Single (postavljeno u SensorService.cs) zahteva
                // da RUCNO prosledimo instancu servisa u ServiceHost konstruktor
               
                host.AddServiceEndpoint(typeof(Sensors.Contracts.ISensorService), new NetTcpBinding(), "");

                host.Open();
                Console.WriteLine($"WCF host started at {baseAddress}");
                Console.WriteLine($"Reconcile every {_reconcileEvery.TotalSeconds:N0}s, tolerance +-{_tolerance}");

                _reconcileTimer = new Timer(async _ => await SafeReconcileAsync(), null, _reconcileEvery, _reconcileEvery);
                _failoverTimer = new Timer(_ => CheckFailover(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

                Console.WriteLine("Press ENTER to stop...");
                Console.ReadLine();

                _reconcileTimer?.Dispose();
                _failoverTimer?.Dispose();
            }
        }

        static int ReadInt(string key, int def) =>
            int.TryParse(ConfigurationManager.AppSettings[key], out var v) ? v : def;

        static double ReadDouble(string key, double def) =>
            double.TryParse(ConfigurationManager.AppSettings[key], out var v) ? v : def;

        // zahtev 2: FailoverMonitor logika (svakih 5s)
        static void CheckFailover()
        {
            var now = DateTime.UtcNow;
            var activeMap = _serviceInstance.ActiveMapSnapshot();
            var heartbeats = _serviceInstance.HeartbeatSnapshot();

            var deadActive = activeMap
                .Where(kv => kv.Value == true)
                .Where(kv => heartbeats.TryGetValue(kv.Key, out var last) && (now - last).TotalSeconds > 15)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var deadId in deadActive)
            {
                Console.WriteLine($"[FAILOVER] S{deadId} nije javio heartbeat > 15s. Proglasavam mrtvim.");
                _serviceInstance.SetActive(deadId, false);

                var standby = activeMap.Keys.FirstOrDefault(id => id != deadId && !activeMap[id]);
                if (standby != 0)
                {
                    _serviceInstance.SetActive(standby, true);
                    Console.WriteLine($"[FAILOVER] S{standby} preuzima ulogu aktivnog senzora.");
                }
                else
                {
                    Console.WriteLine("[FAILOVER] Nema dostupnog standby senzora!");
                }
            }
        }

        // zahtev 3: reconciliation, GOOD-only filter 
        //
        static async Task SafeReconcileAsync()
        {
            try { await ReconcileAsync(); }
            catch (Exception ex) { Console.WriteLine("[Reconcile ERROR] " + ex.Message); }
        }

        static async Task ReconcileAsync()
        {
            using (var db = new SensorsDbContext())
            {
                var latest = new (int sensorId, DateTime ts, double value)?[10];
                latest[0] = await LatestGoodAsync(db.Sensor1Readings, 1);
                latest[1] = await LatestGoodAsync(db.Sensor2Readings, 2);
                latest[2] = await LatestGoodAsync(db.Sensor3Readings, 3);
                latest[3] = await LatestGoodAsync(db.Sensor4Readings, 4);
                latest[4] = await LatestGoodAsync(db.Sensor5Readings, 5);
                latest[5] = await LatestGoodAsync(db.Sensor6Readings, 6);
                latest[6] = await LatestGoodAsync(db.Sensor7Readings, 7);
                latest[7] = await LatestGoodAsync(db.Sensor8Readings, 8);
                latest[8] = await LatestGoodAsync(db.Sensor9Readings, 9);
                latest[9] = await LatestGoodAsync(db.Sensor10Readings, 10);

                var have = latest.Where(x => x.HasValue).Select(x => x.Value).ToList();
                if (!have.Any())
                {
                    Console.WriteLine("[Reconcile] Nema GOOD ocitavanja jos uvek.");
                    return;
                }

                var avg = have.Average(x => x.value);
                var candidates = have.Where(x => Math.Abs(x.value - avg) <= _tolerance).ToList();
                var chosen = (candidates.Any() ? candidates : have).OrderBy(c => c.ts).Last();
                var snapshotTime = DateTime.UtcNow;

                using (var tx = db.Database.BeginTransaction())
                {
                    db.Sensor1Readings.Add(new Sensor1Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor2Readings.Add(new Sensor2Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor3Readings.Add(new Sensor3Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor4Readings.Add(new Sensor4Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor5Readings.Add(new Sensor5Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor6Readings.Add(new Sensor6Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor7Readings.Add(new Sensor7Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor8Readings.Add(new Sensor8Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor9Readings.Add(new Sensor9Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });
                    db.Sensor10Readings.Add(new Sensor10Reading { Timestamp = snapshotTime, Value = chosen.value, Kind = ReadingKind.Consensus, Quality = DataQuality.GOOD });

                    db.ConsensusSnapshots.Add(new ConsensusSnapshot
                    {
                        SnapshotTime = snapshotTime,
                        Value = chosen.value,
                        SourceSensorId = chosen.sensorId,
                        AverageAtDecision = avg,
                        ToleranceUsed = _tolerance
                    });

                    await db.SaveChangesAsync(); // nit koja se izvrsava u okviru Timer callback-a je "background thread" i ne sme da blokira, pa je async/await bolja opcija od SaveChanges() koji bi blokirao nit.
                    // nit ovde odlazi, baza radi sada svoj posao i ta nit nije blokirana vec je slobodna. Kad se zavrsi net uzima bilo koju drugu slobosnu nit 
                    // da je SaveChanges() bez awaita onda bi bloki
                    tx.Commit();
                }

                Console.WriteLine($"[Reconcile {snapshotTime:HH:mm:ss} UTC] avg={avg:F2}, chosen={chosen.value:F2} from S{chosen.sensorId} - CONSENSUS written.");
            }
        }

        //  only good added
        static async Task<(int sensorId, DateTime ts, double value)?> LatestGoodAsync<T>(
            DbSet<T> set, int sensorId) where T : BaseReading
        {
            var row = await set.AsNoTracking()
                .Where(r => r.Kind == ReadingKind.Raw && r.Quality == DataQuality.GOOD)
                .OrderByDescending(r => r.Timestamp)
                .Select(r => new { r.Timestamp, r.Value })
                .FirstOrDefaultAsync();

            if (row == null) return null;
            return (sensorId, row.Timestamp, row.Value);
        }
    }
}