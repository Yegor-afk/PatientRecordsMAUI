using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using PatientRecordsMAUI.Models;

namespace PatientRecordsMAUI.Services
{
    /// <summary>
    /// Služba pro práci s databází pacientů a metrik (Singleton pro efektivní využití LiteDB).
    /// </summary>
    public class LiteDbMedicalService : IDisposable
    {
        private readonly LiteDatabase _db;
        private const string DbFileName = "medicalData.db";

        /// <summary>
        /// Inicializuje novou instanci služby pro práci s databází LiteDB.
        /// </summary>
        public LiteDbMedicalService()
        {
            // Pro správnou cestu v závislosti na OS:
            // V .NET MAUI: FileSystem.AppDataDirectory se obvykle doporučuje,
            // ale pro jednoduchost používáme aktuální nebo systémový adresář:
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbFileName);

            // Inicializace databáze
            _db = new LiteDatabase(dbPath);

            // Nastavení indexů pro optimalizaci dotazů
            var metricsCol = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            metricsCol.EnsureIndex(x => x.PatientId);
            metricsCol.EnsureIndex(x => x.Date);
        }

        /// <summary>
        /// Přidává nového pacienta do databáze.
        /// </summary>
        /// <param name="patient">Model pacienta.</param>
        public void AddPatient(Patient patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            var col = _db.GetCollection<Patient>("patients");

            // Nastavujeme nové Guid, pokud není inicializováno
            if (patient.Id == Guid.Empty)
                patient.Id = Guid.NewGuid();

            col.Insert(patient);
        }

        /// <summary>
        /// Ukládá nebo aktualizuje konkrétní metriku pro pacienta za určité datum a hodinu.
        /// Používá vzor Bucket a dynamický slovník metrik.
        /// </summary>
        /// <param name="patientId">ID pacienta</param>
        /// <param name="date">Datum měření (čas bude ignorován)</param>
        /// <param name="hour">Hodina měření (0-23)</param>
        /// <param name="metricName">Název metriky (např. "HeartRate")</param>
        /// <param name="value">Hodnota metriky</param>
        public void UpdateHourlyMetric(Guid patientId, DateTime date, int hour, string metricName, double value)
        {
            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), "Hodina musí být v rozmezí od 0 do 23.");

            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Název metriky nemůže být prázdný.", nameof(metricName));

            // Odstraňujeme čas z DateTime (ponecháváme pouze datum)
            DateTime pureDate = date.Date;

            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");

            // Hledáme kbelík (bucket) pro daného pacienta a datum
            var bucket = col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);

            if (bucket == null)
            {
                // Vytváříme nový kbelík
                bucket = new DailyMetricsRecord
                {
                    PatientId = patientId,
                    Date = pureDate,
                    Measurements = new List<HourlyMeasurement>()
                };
            }

            // Hledáme existující měření za tuto hodinu
            var measurement = bucket.Measurements.FirstOrDefault(m => m.Hour == hour);

            if (measurement == null)
            {
                measurement = new HourlyMeasurement { Hour = hour };
                bucket.Measurements.Add(measurement);
            }

            // Aktualizujeme nebo přidáváme metriku
            measurement.Metrics[metricName] = value;

            // Pokud je kbelík nový (Id == ObjectId.Empty), Insert, jinak Update
            col.Upsert(bucket);
        }

        /// <summary>
        /// Odstraní konkrétní metriku z měření pro daného pacienta, den a hodinu.
        /// </summary>
        /// <param name="patientId">ID pacienta</param>
        /// <param name="date">Datum měření</param>
        /// <param name="hour">Hodina (0-23)</param>
        /// <param name="metricName">Název metriky k odstranění</param>
        public void RemoveHourlyMetric(Guid patientId, DateTime date, int hour, string metricName)
        {
            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), "Hodina musí být v rozmezí od 0 do 23.");

            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Název metriky nemůže být prázdný.", nameof(metricName));

            DateTime pureDate = date.Date;
            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");

            var bucket = col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);
            if (bucket == null) return;

            var measurement = bucket.Measurements.FirstOrDefault(m => m.Hour == hour);
            if (measurement != null && measurement.Metrics.ContainsKey(metricName))
            {
                measurement.Metrics.Remove(metricName);

                // Můžeme se rozhodnout, zda smazat celé měření, pokud neobsahuje žádné metriky
                // if (!measurement.Metrics.Any()) bucket.Measurements.Remove(measurement);

                col.Update(bucket);
            }
        }

        /// <summary>
        /// Získává všechny metriky pacienta pro konkrétní den k analýze.
        /// </summary>
        /// <param name="patientId">ID pacienta.</param>
        /// <param name="date">Datum hledání.</param>
        /// <returns>Záznam za den nebo null, pokud nejsou žádná data.</returns>
        public DailyMetricsRecord GetPatientMetricsForDay(Guid patientId, DateTime date)
        {
            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            DateTime pureDate = date.Date;

            return col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);
        }

        /// <summary>
        /// Uvolňuje prostředky databáze.
        /// </summary>
        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}