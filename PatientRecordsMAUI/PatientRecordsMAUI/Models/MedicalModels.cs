using System;
using System.Collections.Generic;
using LiteDB;

namespace PatientRecordsMAUI.Models
{
    /// <summary>
    /// Datový typ pro metriku.
    /// </summary>
    public enum MetricDataType
    {
        Number,
        Boolean,
        String,
        Choice
    }

    /// <summary>
    /// Popis nastavení konkrétní metriky.
    /// </summary>
    public class MetricDefinition
    {
        /// <summary>
        /// Název metriky (např. "Tep", "Stav").
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Datový typ metriky.
        /// </summary>
        public MetricDataType DataType { get; set; }
        
        /// <summary>
        /// Seznam dostupných možností výběru (používá se pouze pro typ Choice).
        /// </summary>
        public List<string> AllowedOptions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Představuje profil pacienta.
    /// </summary>
    public class Patient
    {
        [BsonId]
        public Guid Id { get; set; }
        
        /// <summary>
        /// Celé jméno pacienta.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Datum narození pacienta.
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Seznam metrik, které sledujeme u tohoto pacienta.
        /// </summary>
        public List<MetricDefinition> TrackedMetrics { get; set; } = new List<MetricDefinition>();
    }

    /// <summary>
    /// Představuje záznam metrik pacienta za jeden konkrétní den (Vzor Bucket).
    /// </summary>
    public class DailyMetricsRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        /// <summary>
        /// Identifikátor pacienta.
        /// </summary>
        public Guid PatientId { get; set; }
        
        /// <summary>
        /// Datum záznamu (bez času).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Měření podle hodin.
        /// </summary>
        public List<HourlyMeasurement> Measurements { get; set; } = new List<HourlyMeasurement>();
    }

    /// <summary>
    /// Obsahuje měření za určitou hodinu pro konkrétní den.
    /// </summary>
    public class HourlyMeasurement
    {
        /// <summary>
        /// Hodina měření (0 - 23).
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Slovník s měřeními, kde klíč je název metriky a hodnota je samotná hodnota měření.
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }
}