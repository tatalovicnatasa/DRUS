using Sensors.Server.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Sensors.Contracts;


namespace Sensors.Server
{
    // InstanceContextMode.Single = JEDNA instanca ove klase opsluzuje SVE
    // pozive od SVIH klijenata (svih 10 senzora). 
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class SensorService : ISensorService
    {
        private readonly object _lock = new object();

        // zahtev 4: anti-replay poslednji prihvaceni MessageId po senzoru.
        private readonly Dictionary<int, long> _lastMessageId = new Dictionary<int, long>();

        // zahtev 2: da li je senzor trenutno aktivan (u memoriji, radi brzine;
        // baza - SensorStatuses tabela - je "trajna" kopija istog stanja).
        private readonly Dictionary<int, bool> _activeMap = new Dictionary<int, bool>(); // koji je ahtivan
        private readonly Dictionary<int, DateTime> _lastHeartbeat = new Dictionary<int, DateTime>(); // kad se poslednji put javio senzor


        // PREPISIVANJE METODA KOJE MORAJU DA SE IMPLEMENTIRAJU 
        public async Task<bool> SubmitReadingAsync(
            int sensorId, byte[] encryptedPayload, byte[] signature,
            long messageId, DateTime timestampUtc, AlarmPriority alarm)
        {
            lock (_lock)
            {
                // zahtev 4: anti-replay provera (PRE dekripcije - jeftinije) 
                if (_lastMessageId.TryGetValue(sensorId, out var lastId) && messageId <= lastId)
                {
                    Console.WriteLine($"[SECURITY] Odbijena poruka od S{sensorId}: MessageId {messageId} <= {lastId}");
                    return false;
                }

                // zahtev 4: verifikacija potpisa 
                if (!CryptoHelper.Verify(encryptedPayload, messageId, timestampUtc, signature))
                {
                    Console.WriteLine($"[SECURITY] Neispravan potpis od S{sensorId} - poruka odbacena.");
                    return false;
                }

                _lastMessageId[sensorId] = messageId;
            }

            // zahtev 4: dekripcija 
            var json = CryptoHelper.Decrypt(encryptedPayload);
            var parts = json.Split(';');
            var value = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var quality = (DataQuality)Enum.Parse(typeof(DataQuality), parts[1]);

            PrintAlarm(sensorId, value, alarm);

            // upis u bazu
            using (var db = new SensorsDbContext())
            {
                switch (sensorId)
                {
                    case 1: db.Sensor1Readings.Add(new Sensor1Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 2: db.Sensor2Readings.Add(new Sensor2Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 3: db.Sensor3Readings.Add(new Sensor3Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 4: db.Sensor4Readings.Add(new Sensor4Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 5: db.Sensor5Readings.Add(new Sensor5Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 6: db.Sensor6Readings.Add(new Sensor6Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 7: db.Sensor7Readings.Add(new Sensor7Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 8: db.Sensor8Readings.Add(new Sensor8Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 9: db.Sensor9Readings.Add(new Sensor9Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    case 10: db.Sensor10Readings.Add(new Sensor10Reading { Timestamp = timestampUtc, Value = value, Quality = quality, Alarm = alarm, Kind = ReadingKind.Raw }); break;
                    default: throw new ArgumentOutOfRangeException(nameof(sensorId), "SensorId mora biti 1..10.");
                }
                await db.SaveChangesAsync(); // ceka da se upisu promene i tad drzi nit
            }

            return true;
        }

        private void PrintAlarm(int sensorId, double value, AlarmPriority alarm)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = alarm == AlarmPriority.Priority1 ? ConsoleColor.Yellow
                                     : alarm == AlarmPriority.Priority2 ? ConsoleColor.DarkYellow
                                     : alarm == AlarmPriority.Priority3 ? ConsoleColor.Red
                                     : ConsoleColor.Gray;

            Console.WriteLine(alarm != AlarmPriority.None
                ? $"[ALARM {(int)alarm}] S{sensorId} -> {value:F2} @ {DateTime.UtcNow:HH:mm:ss}"
                : $"S{sensorId} -> {value:F2} @ {DateTime.UtcNow:HH:mm:ss}");

            Console.ForegroundColor = prev;
        }

        public async Task<double?> GetLatestConsensusAsync()
        {   // citanje iz baze poslednjeg konsenzusnog snimka (ako postoji)
            using (var db = new SensorsDbContext())
            {
                return await db.ConsensusSnapshots // pristup celoj tabeli DbSet<ConsensusSnapshot>
                    .AsNoTracking() // ne prati promene, samo cita (brze)
                    .OrderByDescending(x => x.SnapshotTime) // sortira po vremenu opadajuce (najnoviji prvi)
                    .Select(x => (double?)x.Value) // selektuje samo vrednost (cast na nullable double)
                    .FirstOrDefaultAsync(); // uzima prvi (najnoviji) ili null ako nema nijednog
            }
        }

        public async Task<Dictionary<int, double?>> GetLatestRawValuesAsync()
        {
            using (var db = new SensorsDbContext())
            {
                var map = new Dictionary<int, double?>(10);
                map[1] = await db.Sensor1Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[2] = await db.Sensor2Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[3] = await db.Sensor3Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[4] = await db.Sensor4Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[5] = await db.Sensor5Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[6] = await db.Sensor6Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[7] = await db.Sensor7Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[8] = await db.Sensor8Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[9] = await db.Sensor9Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                map[10] = await db.Sensor10Readings.AsNoTracking().OrderByDescending(r => r.Timestamp).Select(r => (double?)r.Value).FirstOrDefaultAsync();
                return map;
            }
        }

        // --- Zahtev 1: Report ---
        public async Task<AlarmReportEntry[]> GetAlarmReportAsync()
        {
            using (var db = new SensorsDbContext())
            {
                var all = new List<AlarmReportEntry>();
                await CollectAlarms(db.Sensor1Readings, 1, all);
                await CollectAlarms(db.Sensor2Readings, 2, all);
                await CollectAlarms(db.Sensor3Readings, 3, all);
                await CollectAlarms(db.Sensor4Readings, 4, all);
                await CollectAlarms(db.Sensor5Readings, 5, all);
                await CollectAlarms(db.Sensor6Readings, 6, all);
                await CollectAlarms(db.Sensor7Readings, 7, all);
                await CollectAlarms(db.Sensor8Readings, 8, all);
                await CollectAlarms(db.Sensor9Readings, 9, all);
                await CollectAlarms(db.Sensor10Readings, 10, all);

                return all.OrderByDescending(e => e.Alarm).ThenByDescending(e => e.Timestamp).ToArray();
            }
        }
        // genericka pomocna metoda
        private async Task CollectAlarms<T>(DbSet<T> set, int sensorId, List<AlarmReportEntry> into) where T : BaseReading
        {
            var rows = await set.AsNoTracking().Where(r => r.Alarm != AlarmPriority.None).ToListAsync();
            into.AddRange(rows.Select(r => new AlarmReportEntry { SensorId = sensorId, Timestamp = r.Timestamp, Value = r.Value, Alarm = r.Alarm }));
        }

        // zahtev 2: heartbeat 
        public Task HeartbeatAsync(int sensorId, bool isCurrentlyActive)
        {
            lock (_lock)
            {
                _lastHeartbeat[sensorId] = DateTime.UtcNow;
                //_activeMap[sensorId] = isCurrentlyActive; 

                if (!_activeMap.ContainsKey(sensorId))
                {
                    _activeMap[sensorId] = isCurrentlyActive;
                }
            }

            using (var db = new SensorsDbContext())
            {
                var status = db.SensorStatuses.Find(sensorId);
                if (status == null)
                    db.SensorStatuses.Add(new SensorStatusEntity { SensorId = sensorId, IsActive = isCurrentlyActive, LastHeartbeatUtc = DateTime.UtcNow });
                else
                {
                    status.IsActive = isCurrentlyActive;
                    status.LastHeartbeatUtc = DateTime.UtcNow;
                }
                db.SaveChanges();
            }
            // uisuje koji je ziv i u kom trenutku
            return Task.CompletedTask;
        }

        public Task<bool> AmIActiveAsync(int sensorId)
        {
            lock (_lock)
            {
                return Task.FromResult(_activeMap.TryGetValue(sensorId, out var active) && active);
            }
        }

        // Ovo koristi FailoverMonitor (sledeci fajl - Program.cs) da otkrije "mrtve" senzore.
        // vraca kopiju 
        public Dictionary<int, bool> ActiveMapSnapshot() { lock (_lock) return new Dictionary<int, bool>(_activeMap); }
        public Dictionary<int, DateTime> HeartbeatSnapshot() { lock (_lock) return new Dictionary<int, DateTime>(_lastHeartbeat); }
        public void SetActive(int sensorId, bool active) { lock (_lock) _activeMap[sensorId] = active; }
    }
}