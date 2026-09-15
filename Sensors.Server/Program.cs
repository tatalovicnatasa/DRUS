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
            MainAsync().GetAwaiter().GetResult(); // blokira Main thread dok se ne zavrsi MainAsync
        }

        static async Task MainAsync()
        {
            // Ensure DB exists (Code-First: EF sam pravi bazu/tabele ako ne postoje)
            Database.SetInitializer(new CreateDatabaseIfNotExists<SensorsDbContext>());

            _tolerance = ReadDouble("Tolerance", 5.0); // default tolerance 5.0
            _reconcileEvery = TimeSpan.FromSeconds(ReadInt("ReconcileIntervalSeconds", 60)); // default reconcile every 60s, cita se iz app.config , pravi se Timer 

            _serviceInstance = new SensorService(); // kreiramo instancu servisa koju cemo proslediti u ServiceHost (InstanceContextMode.Single zahteva da mi sami kreiramo instancu servisa i prosledimo je u ServiceHost konstruktor)

            var baseAddress = new Uri("net.tcp://localhost:9001/SensorService");// WCF host ce slusati na ovom adresom (net.tcp protokol, port 9001, endpoint SensorService)
            using (var host = new ServiceHost(_serviceInstance, baseAddress)) // kreiramo WCF host i prosledjujemo mu instancu servisa i baznu adresu
            {
                // InstanceContextMode.Single (postavljeno u SensorService.cs) zahteva
                // da RUCNO prosledimo instancu servisa u ServiceHost konstruktor
               
                host.AddServiceEndpoint(typeof(Sensors.Contracts.ISensorService), new NetTcpBinding(), ""); // dodajemo endpoint za ISensorService interfejs, sa net.tcp protokolom i praznim relativnim endpointom (znači ceo baseAddress je endpoint)

                host.Open(); // otvaramo WCF host, sada je spreman da prima pozive od klijenata
                Console.WriteLine($"WCF host started at {baseAddress}");
                Console.WriteLine($"Reconcile every {_reconcileEvery.TotalSeconds:N0}s, tolerance +-{_tolerance}");

                _reconcileTimer = new Timer(async _ => await SafeReconcileAsync(), null, _reconcileEvery, _reconcileEvery); // pokrecemo timer za reconciliation, SafeReconcileAsync() je asinhrona metoda koja se poziva svakih _reconcileEvery sekundi
                _failoverTimer = new Timer(_ => CheckFailover(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)); // pokrecemo timer za failover check, CheckFailover() je metoda koja se poziva svakih 5 sekundi

                Console.WriteLine("Press ENTER to stop...");
                Console.ReadLine();

                _reconcileTimer?.Dispose(); // Dispose timera kada se program zavrsava
                _failoverTimer?.Dispose(); 
            }
        }

        static int ReadInt(string key, int def) =>
            int.TryParse(ConfigurationManager.AppSettings[key], out var v) ? v : def;

        static double ReadDouble(string key, double def) =>
            double.TryParse(ConfigurationManager.AppSettings[key], out var v) ? v : def;

        // zahtev 2: FailoverMonitor logika (svakih 5s)
        static void CheckFailover() // trigeruje je Timer iz MainAsync zato je ovde void i ne treba async/await
        {
            var now = DateTime.UtcNow;
            var activeMap = _serviceInstance.ActiveMapSnapshot(); // uzimamo snapshot trenutnog stanja aktivnih senzora (Dictionary<int, bool> gde je key sensorId a value true/false za aktivan/suspendovan)
            var heartbeats = _serviceInstance.HeartbeatSnapshot(); // uzimamo snapshot poslednjih heartbeat-ova (Dictionary<int, DateTime> gde je key sensorId a value vreme poslednjeg heartbeat-a)

            var deadActive = activeMap // filtriramo samo one senzore koji su aktivni (value == true) i koji nisu javili heartbeat u poslednjih 15s
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
        static async Task SafeReconcileAsync() // trigeruje je Timer iz ReconcileAsync zato je ovde async Task i try/catch
        {
            try { await ReconcileAsync(); }
            catch (Exception ex) { Console.WriteLine("[Reconcile ERROR] " + ex.Message); }
        }

        static async Task ReconcileAsync() //
        {
            using (var db = new SensorsDbContext()) // kreiramo novu instancu SensorsDbContext za pristup bazi podataka
            {
                var latest = new (int sensorId, DateTime ts, double value)?[10]; // niz od 10 elemenata, svaki element je tuple (sensorId, timestamp, value) ili null
                latest[0] = await LatestGoodAsync(db.Sensor1Readings, 1); // pozivamo asinhronu metodu LatestGoodAsync koja vraca najnovije GOOD ocitavanje za dati DbSet i sensorId 
                latest[1] = await LatestGoodAsync(db.Sensor2Readings, 2);
                latest[2] = await LatestGoodAsync(db.Sensor3Readings, 3);
                latest[3] = await LatestGoodAsync(db.Sensor4Readings, 4);
                latest[4] = await LatestGoodAsync(db.Sensor5Readings, 5);
                latest[5] = await LatestGoodAsync(db.Sensor6Readings, 6);
                latest[6] = await LatestGoodAsync(db.Sensor7Readings, 7);
                latest[7] = await LatestGoodAsync(db.Sensor8Readings, 8);
                latest[8] = await LatestGoodAsync(db.Sensor9Readings, 9);
                latest[9] = await LatestGoodAsync(db.Sensor10Readings, 10);
                // radimo nad citavim have
                var have = latest.Where(x => x.HasValue).Select(x => x.Value).ToList(); // filtriramo samo one senzore koji imaju GOOD ocitavanje, i pravimo listu tuple (sensorId, timestamp, value)
                if (!have.Any()) // ako nema nijednog GOOD ocitavanja, ne radimo nista
                {
                    Console.WriteLine("[Reconcile] Nema GOOD ocitavanja jos uvek.");
                    return;
                }
                // Racunanje proseka i biranje pobednika
                var avg = have.Average(x => x.value); // racunamo prosek vrednosti svih GOOD ocitavanja
                var candidates = have.Where(x => Math.Abs(x.value - avg) <= _tolerance).ToList(); //
                var chosen = (candidates.Any() ? candidates : have).OrderBy(c => c.ts).Last(); // biramo onaj koji je najblizi proseku, a ako ih ima vise biramo onaj sa najnovijim timestampom
                var snapshotTime = DateTime.UtcNow; // vreme kada pravimo snapshot, ne uzimamo timestamp iz ocitavanja jer je to vreme kada je ocitavanje nastalo, a ne vreme kada pravimo snapshot

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
                    tx.Commit(); // ATOMINCOST
                }

                Console.WriteLine($"[Reconcile {snapshotTime:HH:mm:ss} UTC] avg={avg:F2}, chosen={chosen.value:F2} from S{chosen.sensorId} - CONSENSUS written.");
            }
        }

        //  only good added
        static async Task<(int sensorId, DateTime ts, double value)?> LatestGoodAsync<T>(
            DbSet<T> set, int sensorId) where T : BaseReading // T je tip koji nasledjuje BaseReading, set je DbSet<T> koji predstavlja tabelu u bazi, sensorId je id senzora
        { 
            var row = await set.AsNoTracking() // AsNoTracking() je EF metoda koja govori EF-u da ne prati promene na entitetima koje vraca, sto je brze i manje memorijski zahtevno za read-only upite
                .Where(r => r.Kind == ReadingKind.Raw && r.Quality == DataQuality.GOOD) // filtriramo samo ocitavanja koja su RAW i GOOD
                .OrderByDescending(r => r.Timestamp) // najnovije ocitavanje prvo
                .Select(r => new { r.Timestamp, r.Value }) // selektujemo samo Timestamp i Value jer nam ne treba ceo entitet
                .FirstOrDefaultAsync(); // uzimamo prvo (najnovije) ocitavanje ili null ako nema takvih ocitavanja

            if (row == null) return null; // ako nema GOOD ocitavanja vracamo null
            return (sensorId, row.Timestamp, row.Value); // vracamo tuple sa sensorId, Timestamp i Value
        }
    }
} //Base Reading je klasa koja sadrzi zajednicka svojstva za sve senzore, a SensorXReading su klase koje nasledjuju BaseReading i predstavljaju ocitavanja za svaki od 10 senzora.