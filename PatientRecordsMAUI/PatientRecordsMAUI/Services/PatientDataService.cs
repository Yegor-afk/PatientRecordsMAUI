using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using PatientRecordsMAUI.Models;

namespace PatientRecordsMAUI.Services
{
    /// <summary>
    /// Služba pro práci s pacienty a jejich metrikami v LiteDB (CRUD).
    /// </summary>
    public class PatientDataService : IDisposable
    {
        private readonly LiteDatabase _db;
        private const string DbFileName = "patientData.db";

        /// <summary>
        /// Inicializuje instanci databázové služby.
        /// </summary>
        public PatientDataService()
        {
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbFileName);

            _db = new LiteDatabase(dbPath);

            var metricsCol = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            metricsCol.EnsureIndex(x => x.PatientId);
            metricsCol.EnsureIndex(x => x.Date);
        }

        #region Správa pacientů

        /// <summary>
        /// Vytvoří nového pacienta.
        /// </summary>
        public void CreatePatient(Patient patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            if (patient.Id == Guid.Empty)
                patient.Id = Guid.NewGuid();

            var col = _db.GetCollection<Patient>("patients");
            col.Insert(patient);
        }

        /// <summary>
        /// Aktualizuje údaje pacienta.
        /// </summary>
        public void UpdatePatient(Patient patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            var col = _db.GetCollection<Patient>("patients");
            col.Update(patient);
        }

        /// <summary>
        /// Odstraní pacienta a všechny jeho záznamy měření (DailyMetricsRecord).
        /// </summary>
        public void DeletePatient(Guid patientId)
        {
            var pCol = _db.GetCollection<Patient>("patients");
            pCol.Delete(patientId);

            var mCol = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            mCol.DeleteMany(x => x.PatientId == patientId);
        }

        /// <summary>
        /// Vrátí všechny pacienty.
        /// </summary>
        public IEnumerable<Patient> GetAllPatients()
        {
            var col = _db.GetCollection<Patient>("patients");
            return col.FindAll().ToList();
        }

        /// <summary>
        /// Vrátí pacienta podle ID.
        /// </summary>
        public Patient GetPatientById(Guid patientId)
        {
            var col = _db.GetCollection<Patient>("patients");
            return col.FindById(patientId);
        }

        #endregion

        #region Správa pravidel metrik (TrackedMetrics)

        /// <summary>
        /// Přidá nebo aktualizuje metriku v seznamu sledovaných pro pacienta.
        /// </summary>
        public void AddOrUpdateMetricDefinition(Guid patientId, MetricDefinition metricDef)
        {
            if (metricDef == null)
                throw new ArgumentNullException(nameof(metricDef));

            var patient = GetPatientById(patientId);
            if (patient == null)
                throw new InvalidOperationException($"Pacient s ID {patientId} nebyl nalezen.");

            var existing = patient.TrackedMetrics.FirstOrDefault(m => string.Equals(m.Name, metricDef.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.DataType = metricDef.DataType;
                existing.AllowedOptions = metricDef.AllowedOptions;
            }
            else
            {
                patient.TrackedMetrics.Add(metricDef);
            }

            UpdatePatient(patient);
        }

        /// <summary>
        /// Odstraní pravidlo metriky z karty pacienta.
        /// </summary>
        public void RemoveMetricDefinition(Guid patientId, string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Název metriky nemůže být prázdný.", nameof(metricName));

            var patient = GetPatientById(patientId);
            if (patient == null)
                throw new InvalidOperationException($"Pacient s ID {patientId} nebyl nalezen.");

            int removedCount = patient.TrackedMetrics.RemoveAll(m => 
                string.Equals(m.Name, metricName, StringComparison.OrdinalIgnoreCase));

            if (removedCount > 0)
            {
                UpdatePatient(patient);
            }
        }

        #endregion

        #region Správa měření (HourlyMeasurement)

        /// <summary>
        /// Zaznamená hodnotu měření pro určitou hodinu konkrétního dne.
        /// </summary>
        public void SetMetricValue(Guid patientId, DateTime date, int hour, string metricName, object value)
        {
            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), "Hodina musí být od 0 do 23.");

            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Název metriky nemůže být prázdný.", nameof(metricName));

            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            DateTime pureDate = date.Date;

            // Hledáme 'bucket' pro pacienta na konkrétní den
            var record = col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);

            if (record == null)
            {
                record = new DailyMetricsRecord
                {
                    PatientId = patientId,
                    Date = pureDate,
                    Measurements = new List<HourlyMeasurement>()
                };
            }

            // Hledáme měření pro tuto hodinu
            var measurement = record.Measurements.FirstOrDefault(m => m.Hour == hour);
            if (measurement == null)
            {
                measurement = new HourlyMeasurement { Hour = hour };
                record.Measurements.Add(measurement);
            }

            // Přidáme nebo aktualizujeme hodnotu
            // V případě práce s LiteDB bude object (pokud jde o základní typ BSON) serializován správně
            measurement.Metrics[metricName] = value;

            col.Upsert(record);
        }

        /// <summary>
        /// Odstraní měření metriky z určité hodiny konkrétního dne.
        /// </summary>
        public void RemoveMetricValue(Guid patientId, DateTime date, int hour, string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Název metriky nemůže být prázdný.", nameof(metricName));

            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), "Hodina musí být od 0 do 23.");

            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            DateTime pureDate = date.Date;

            var record = col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);
            if (record != null)
            {
                var measurement = record.Measurements.FirstOrDefault(m => m.Hour == hour);
                if (measurement != null && measurement.Metrics.ContainsKey(metricName))
                {
                    measurement.Metrics.Remove(metricName);

                    // Volitelně: pokud v hodině již nejsou žádné metriky, lze odstranit i samotné HourlyMeasurement
                    if (measurement.Metrics.Count == 0)
                    {
                        record.Measurements.Remove(measurement);
                    }

                    col.Update(record);
                }
            }
        }

        /// <summary>
        /// Vrátí denní měření podle ID pacienta a přesného data.
        /// </summary>
        public DailyMetricsRecord GetDailyRecord(Guid patientId, DateTime date)
        {
            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            DateTime pureDate = date.Date;

            return col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);
        }

        #endregion

        /// <summary>
        /// Uvolní prostředky připojení k databázi.
        /// </summary>
        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}