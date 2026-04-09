using System;
using System.Collections.Generic;
using LiteDB;

namespace PatientRecordsMAUI.Models
{
    /// <summary>
    /// Představuje profil pacienta.
    /// </summary>
    public class Patient
    {
        [BsonId]
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }

        /// <summary>
        /// Seznam metrik, které jsou pro tohoto pacienta sledovány (např. "HeartRate", "BloodPressure").
        /// </summary>
        public List<string> TrackedMetrics { get; set; } = new List<string>();
        // Další vlastnosti můžete přidat zde
    }

    /// <summary>
    /// Představuje záznam metrik pacienta za jeden konkrétní den (vzor Bucket).
    /// </summary>
    public class DailyMetricsRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        /// <summary>
        /// Identifikátor pacienta (pro propojení).
        /// </summary>
        public Guid PatientId { get; set; }
        
        /// <summary>
        /// Datum záznamu (čas by měl být 00:00:00).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Sbírka hodinových měření (od 0 do 23).
        /// </summary>
        public List<HourlyMeasurement> Measurements { get; set; } = new List<HourlyMeasurement>();
    }

    /// <summary>
    /// Obsahuje měření za určitou hodinu pro DailyMetricsRecord.
    /// </summary>
    public class HourlyMeasurement
    {
        /// <summary>
        /// Hodina měření (0 - 23).
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Dynamický slovník metrik. Klíč je název metriky (např. "HeartRate"), hodnota je naměřená hodnota.
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
    }
}