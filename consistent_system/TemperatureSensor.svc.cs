using System;
using System.ServiceModel;
using System.Threading;

namespace consistent_system
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TemperatureSensorService : ITemperatureSensor
    {
        private readonly SensorDatabase _db;
        private readonly Random _rand = new Random();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private Timer _measurementTimer;

        public TemperatureSensorService(SensorName name)
        {
            _db = new SensorDatabase(name);
            StartMeasurementGeneration();
        }

        private void StartMeasurementGeneration()
        {
            GenerateMeasurement();
            int interval = _rand.Next(1000, 10000);
            _measurementTimer = new Timer(_ =>
            {
                GenerateMeasurement();
                _measurementTimer.Change(_rand.Next(1000, 10000), Timeout.Infinite);
            }, null, interval, Timeout.Infinite);
        }

        private void GenerateMeasurement()
        {
            double temperature = 273.15 + _rand.Next(0, 30) + _rand.NextDouble();
            _lock.EnterWriteLock();
            try
            {
                _db.InsertMeasurement(temperature);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public double ReadTemperature()
        {
            _lock.EnterReadLock();
            try
            {
                var last = _db.GetLastMeasurement();
                if (last.HasValue)
                    return last.Value;

                throw new Exception("No measurement available");
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void SyncTemperature(double temperature)
        {
            _lock.EnterWriteLock();
            try
            {
                _db.InsertMeasurement(temperature);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: [Sensor] Synced to {temperature:F2}K");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
