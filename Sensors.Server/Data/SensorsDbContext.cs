using Sensors.Server.Data;
using System.Data.Entity;

namespace Sensors.Server.Data
{
    public class SensorsDbContext : DbContext
    {
        // "name=SensorsDb" govori EF-u da potrazi connection string sa
        // imenom "SensorsDb" u App.config fajlu 
        public SensorsDbContext() : base("name=SensorsDb") { } // base("name=SensorsDb") je konstruktor DbContext-a, koji prima connection string name
        // EF pravi od ovoga tabele
        public DbSet<Sensor1Reading> Sensor1Readings { get; set; } // ime DbSet-a je ime tabele u bazi
        public DbSet<Sensor2Reading> Sensor2Readings { get; set; } // DbSet<Sensor2Reading> je tip tabele, a Sensor2Readings je ime tabele u bazi
        public DbSet<Sensor3Reading> Sensor3Readings { get; set; }
        public DbSet<Sensor4Reading> Sensor4Readings { get; set; }
        public DbSet<Sensor5Reading> Sensor5Readings { get; set; }
        public DbSet<Sensor6Reading> Sensor6Readings { get; set; }
        public DbSet<Sensor7Reading> Sensor7Readings { get; set; }
        public DbSet<Sensor8Reading> Sensor8Readings { get; set; }
        public DbSet<Sensor9Reading> Sensor9Readings { get; set; }
        public DbSet<Sensor10Reading> Sensor10Readings { get; set; }

        public DbSet<SensorStatusEntity> SensorStatuses { get; set; } // senzor status tabela
        public DbSet<ConsensusSnapshot> ConsensusSnapshots { get; set; } // consensus snapshot tabela

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Rucno govorimo EF-u da je SensorId primarni kljuc ove tabele,
            // jer se ne zove "Id" (EF konvencija) nego "SensorId"
            // mogla sam samo da nazovem kako je trebalo 
            modelBuilder.Entity<SensorStatusEntity>().HasKey(s => s.SensorId);
        }
    }
}