using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using PatientRecordsMAUI.Models;

namespace PatientRecordsMAUI.Services
{
    /// <summary>
    /// Сервис для работы с базой данных пациентов и метрик (Singleton для эффективного использования LiteDB).
    /// </summary>
    public class LiteDbMedicalService : IDisposable
    {
        private readonly LiteDatabase _db;
        private const string DbFileName = "medicalData.db";

        /// <summary>
        /// Инициализирует новый экземпляр сервиса работы с БД LiteDB.
        /// </summary>
        public LiteDbMedicalService()
        {
            // Для правильного пути в зависимости от ОС:
            // В .NET MAUI: FileSystem.AppDataDirectory обычно рекомендуется,
            // но для простоты используем текущую директорию или системную:
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbFileName);

            // Инициализация базы данных
            _db = new LiteDatabase(dbPath);
            
            // Настройка индексов для оптимизации запросов
            var metricsCol = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            metricsCol.EnsureIndex(x => x.PatientId);
            metricsCol.EnsureIndex(x => x.Date);
        }

        /// <summary>
        /// Добавляет нового пациента в базу данных.
        /// </summary>
        /// <param name="patient">Модель пациента.</param>
        public void AddPatient(Patient patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            var col = _db.GetCollection<Patient>("patients");
            
            // Задаем новый Guid, если не инициализирован
            if (patient.Id == Guid.Empty)
                patient.Id = Guid.NewGuid();
                
            col.Insert(patient);
        }

        /// <summary>
        /// Сохраняет или обновляет ежечасные метрики для пациента за определенную дату и час.
        /// Использует паттерн Bucket.
        /// </summary>
        public void SaveHourlyMetrics(
            Guid patientId, DateTime date, int hour, 
            double m1, double m2, double m3, double m4, double m5, double m6, double m7)
        {
            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), "Час должен быть в диапазоне от 0 до 23.");

            // Убираем время из DateTime (оставляем только дату)
            DateTime pureDate = date.Date;

            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");

            // Ищем корзину (bucket) для данного пациента и даты
            var bucket = col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);

            if (bucket == null)
            {
                // Создаем новую корзину
                bucket = new DailyMetricsRecord
                {
                    PatientId = patientId,
                    Date = pureDate,
                    Measurements = new List<HourlyMeasurement>()
                };
            }

            // Ищем существующий замер за этот час
            var measurement = bucket.Measurements.FirstOrDefault(m => m.Hour == hour);
            
            if (measurement == null)
            {
                measurement = new HourlyMeasurement { Hour = hour };
                bucket.Measurements.Add(measurement);
            }

            // Обновляем метрики
            measurement.Metric1 = m1;
            measurement.Metric2 = m2;
            measurement.Metric3 = m3;
            measurement.Metric4 = m4;
            measurement.Metric5 = m5;
            measurement.Metric6 = m6;
            measurement.Metric7 = m7;

            // Если корзина новая (Id == ObjectId.Empty), Insert, иначе Update
            col.Upsert(bucket);
        }

        /// <summary>
        /// Получает все метрики пациента за конкретный день для анализа.
        /// </summary>
        /// <param name="patientId">ID пациента.</param>
        /// <param name="date">Дата поиска.</param>
        /// <returns>Запись за день или null, если данных нет.</returns>
        public DailyMetricsRecord GetPatientMetricsForDay(Guid patientId, DateTime date)
        {
            var col = _db.GetCollection<DailyMetricsRecord>("daily_metrics");
            DateTime pureDate = date.Date;
            
            return col.FindOne(x => x.PatientId == patientId && x.Date == pureDate);
        }

        /// <summary>
        /// Освобождает ресурсы базы данных.
        /// </summary>
        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}