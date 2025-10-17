using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace consistent_system
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TemperatureUnit : ITemperatureUnit
    {
        private readonly string TAG = "UNIT";
        private readonly int QUORUM_SIZE = 2;
        private readonly int HISTORY_LIMIT = 1000;
        private readonly int N_SENSORS = 3;
        private readonly double PRECISION = 5;
        private readonly TimeSpan AUTO_SYNC_DELAY = TimeSpan.FromMinutes(1);
        private Timer _timer;
        private ImmutableList<ITemperatureSensor> _sensors;
        private ConcurrentQueue<double> _history = new ConcurrentQueue<double>();
        private ReaderWriterLockSlim _opLock = new ReaderWriterLockSlim();

        public TemperatureUnit()
        {
            _sensors = DiscoverSensors().ToImmutableList();
            _timer = new Timer(AutoSyncCallback, null, AUTO_SYNC_DELAY, AUTO_SYNC_DELAY);
        }

        public double ReadTemperature()
        {
            var readings = new ConcurrentBag<double>();

            _opLock.EnterReadLock();
            try
            {
                Parallel.ForEach(_sensors, (sensor) =>
                {
                    try
                    {
                        double temp = sensor.ReadTemperature();
                        readings.Add(temp);
                    }
                    catch (FaultException ex)
                    {
                        Console.WriteLine($"Sensor read failed: {ex.Message}");
                    }
                });
            }
            finally
            {
                _opLock.ExitReadLock();
            }

            var counts = readings
                .GroupBy(r => Math.Round(r, 1))
                .Select(g => new { Value = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList();

            Console.WriteLine(
                $"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Sensor readings:" +
                Environment.NewLine +
                string.Join(Environment.NewLine,
                    counts.Select(item => $"  Value: {item.Value:F2}K, Count: {item.Count}"))
            );

            if (counts.Count == 0)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] No sensors responded");
                return double.NaN;
            }

            var consensus = counts.First();
            var dFromAverage = Math.Abs(consensus.Value - readings.Average());

            if (consensus.Count < QUORUM_SIZE || dFromAverage > PRECISION)
            {
                if (consensus.Count < QUORUM_SIZE)
                {
                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Quorum not reached ({consensus.Count}/{QUORUM_SIZE})"
                    );
                }
                else if (dFromAverage > PRECISION)
                {
                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Consensus {consensus.Value:F2}K too far from average {readings.Average():F2}K"
                    );
                }

                bool started = false;
                Task.Run(() =>
                {
                    started = true;
                    Sync();
                });

                while (!started) { }
                return double.NaN;
            }

            _history.Enqueue(consensus.Value);
            while (_history.Count > HISTORY_LIMIT)
                _history.TryDequeue(out _);

            return consensus.Value;
        }

        private void Sync()
        {
            _opLock.EnterWriteLock();
            try
            {
                double avgTemperature = _history.ToArray()
                    .DefaultIfEmpty(293.15)
                    .Average();

                Console.WriteLine(
                    $"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Syncing all sensors to {avgTemperature:F2}K"
                );

                Parallel.ForEach(_sensors, (sensor) =>
                    sensor.SyncTemperature(avgTemperature)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Sync failed: {ex.Message}"
                );
            }
            finally
            {
                _opLock.ExitWriteLock();
            }
        }

        private List<ITemperatureSensor> DiscoverSensors()
        {
            return Enumerable.Range(0, N_SENSORS)
                .Select(i => $"http://localhost:{8000 + i}/TemperatureSensor.svc")
                .Select(url => new ChannelFactory<ITemperatureSensor>(
                    new BasicHttpBinding(),
                    new EndpointAddress(url)
                ).CreateChannel())
                .ToList();
        }

        private void AutoSyncCallback(object state)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: [{TAG}] Auto-sync triggered");
            Sync();
        }
    }
}
