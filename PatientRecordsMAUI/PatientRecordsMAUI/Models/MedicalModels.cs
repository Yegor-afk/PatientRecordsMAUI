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

        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }
        public double Metric4 { get; set; }
        public double Metric5 { get; set; }
        public double Metric6 { get; set; }
        public double Metric7 { get; set; }
    }
}