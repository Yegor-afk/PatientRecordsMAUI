using System;
using System.Collections.Generic;
using LiteDB;

namespace PatientRecordsMAUI.Models
{
    /// <summary>
    /// Представляет профиль пациента.
    /// </summary>
    public class Patient
    {
        [BsonId]
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        // Дополнительные свойства можно добавить здесь
    }

    /// <summary>
    /// Представляет запись метрик пациента за один конкретный день (Паттерн Bucket).
    /// </summary>
    public class DailyMetricsRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        /// <summary>
        /// Идентификатор пациента (для связи).
        /// </summary>
        public Guid PatientId { get; set; }
        
        /// <summary>
        /// Дата записи (время должно быть 00:00:00).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Коллекция ежечасных замеров (от 0 до 23).
        /// </summary>
        public List<HourlyMeasurement> Measurements { get; set; } = new List<HourlyMeasurement>();
    }

    /// <summary>
    /// Содержит замеры за определенный час для DailyMetricsRecord.
    /// </summary>
    public class HourlyMeasurement
    {
        /// <summary>
        /// Час замера (0 - 23).
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