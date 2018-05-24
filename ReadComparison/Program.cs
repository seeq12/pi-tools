using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;

namespace _ReadComparison {

    internal class Program {
        private static DateTime readStart = new DateTime(2017, 1, 1);
        private static int[] daysToReadList = { 1, 7, 30, 90, 120 };
        private static Func<String> tagNameSupplier = () => "testSinusoid_" + new Random().Next(0, 6000);

        private static void Main(string[] args) {
            readData("pi2015", "pi2016", "pi2017");

            Console.Error.Write("\n\nPress any key to exit");
            Console.ReadKey();
        }

        private static void readData(params String[] serverNames) {
            foreach (String serverName in serverNames) {
                foreach (int daysToRead in daysToReadList) {
                    readData(serverName, tagNameSupplier.Invoke(), daysToRead);
                }

                Console.WriteLine("");
            }
        }

        private static void readData(String serverName, String tagName, int daysToRead) {
            Console.Write("[{0} {1}d] ", serverName, daysToRead);

            PIServer server = new PIServers()
                .Where(s => s.Name.Contains(serverName))
                .First();
            server.Connect();

            Stopwatch roundTripStopwatch = Stopwatch.StartNew();
            PIPoint tag = PIPoint.FindPIPoint(server, tagName);
            roundTripStopwatch.Stop();

            AFTimeRange timeRange = new AFTimeRange(new AFTime(readStart), new AFTime(readStart.Add(new TimeSpan(daysToRead, 0, 0, 0))));

            try {
                Stopwatch readStopwatch = Stopwatch.StartNew();
                AFValues values = tag.RecordedValues(timeRange, AFBoundaryType.Outside, "", true, 0);
                readStopwatch.Stop();

                Console.WriteLine("Read {0:n0} samples in {1:0.000}s (1m samples in {2:0.000}s) EstimatedRoundTripTime: {3}ms",
                    values.Count,
                    readStopwatch.ElapsedMilliseconds / 1000.0,
                    ((double)readStopwatch.ElapsedMilliseconds) / values.Count * 1000.0,
                    roundTripStopwatch.ElapsedMilliseconds);
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}", e.ToString());
            }

            server.Disconnect();
        }
    }
}