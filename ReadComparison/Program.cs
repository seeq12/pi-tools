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
        private static int[] daysToReadList = { 180, 180, 180 };
        private static Func<String> tagNameSupplier = () => "testSinusoid_" + new Random().Next(0, 6000);

        private static void Main(string[] args) {
            readData(true, "pi2015", "pi2016", "pi2017", "pi2018");

            Console.Error.Write("\n\nPress any key to exit");
            Console.ReadKey();
        }

        private static void readData(bool reuseConnection, params String[] serverNames) {
            if (reuseConnection) {
                Console.WriteLine("Reusing the connection between tests on the same server\n");
            } else {
                Console.WriteLine("All tests are in their own connection\n");
            }

            foreach (String serverName in serverNames) {
                if (reuseConnection) {
                    readData(serverName, tagNameSupplier, daysToReadList);
                } else {
                    foreach (int daysToRead in daysToReadList) {
                        readData(serverName, tagNameSupplier, new int[] { daysToRead });
                    }
                }

                Console.WriteLine("");
            }
        }

        private static void readData(String serverName, Func<String> tagNameSupplier, int[] daysToReadList) {
            PIServer server = new PIServers()
                .Where(s => s.Name.Contains(serverName))
                .First();
            server.Connect();

            foreach (int daysToRead in daysToReadList) {
                Console.Write("[{0} {1}d] ", serverName, daysToRead);

                Stopwatch roundTripStopwatch = Stopwatch.StartNew();
                PIPoint tag = PIPoint.FindPIPoint(server, tagNameSupplier.Invoke());
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
            }

            server.Disconnect();
        }
    }
}