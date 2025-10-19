using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace consistent_system
{
    class Program
    {
        static void Main(string[] args)
        {
            var sensorUrlFmt = "http://localhost:{0}/TemperatureSensor.svc";
            var N_SENSORS = 3;
            var sensorNames = (SensorName[])Enum.GetValues(typeof(SensorName));
            var hosts = new List<ServiceHost>();

            try
            {
                for (int i = 0; i < N_SENSORS; ++i)
                {
                    var baseUri = new Uri(string.Format(sensorUrlFmt, 8_000 + i));
                    Console.WriteLine($"Starting Sensor {i + 1} at {baseUri}");
                    var host = new ServiceHost(
                        new TemperatureSensorService(sensorNames[i]),
                        baseUri
                    );
                    host.AddServiceEndpoint(
                        typeof(ITemperatureSensor),
                        new BasicHttpBinding(),
                        ""
                    );
                    host.Open();
                    hosts.Add(host);
                }

                var unitBaseUri = new Uri($"http://localhost:{8_000 + N_SENSORS + 1}/TemperatureUnit.svc");
                Console.WriteLine($"Starting Temperature Unit at {unitBaseUri}");
                var unitHost = new ServiceHost(typeof(TemperatureUnit), unitBaseUri);
                unitHost.AddServiceEndpoint(
                    typeof(ITemperatureUnit),
                    new BasicHttpBinding(),
                    ""
                );
                unitHost.Open();
                hosts.Add(unitHost);

                var unit = new ChannelFactory<ITemperatureUnit>(
                    new BasicHttpBinding(),
                    new EndpointAddress(unitBaseUri)
                ).CreateChannel();

                Console.WriteLine("Services running. Press Enter to exit..." + Environment.NewLine);
                var exit = Task.Run(Console.ReadLine);

                Thread.Sleep(500);

                int j = 0;
                while (++j < 10 && !exit.IsCompleted)
                {
                    Console.WriteLine($"\u001b[32m======== Reading {j:D3} - {DateTime.Now:HH:mm:ss} ========\u001b[0m");
                    double reading = unit.ReadTemperature();

                    if (!double.IsNaN(reading))
                    {
                        Console.WriteLine($"✓ Valid Temperature: {reading:F2}K");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Invalid reading (sync triggered)");
                    }

                    Console.WriteLine("\u001b[32m================================\u001b[0m");
                    Thread.Sleep(2_000);
                }
            }
            finally
            {
                foreach (var host in hosts)
                {
                    host.Close();
                }
            }
        }
    }
}
