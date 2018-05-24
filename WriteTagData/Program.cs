using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;

namespace WriteTagData {

    internal class Program {

        private static void Main(string[] args) {
            PIServer server = new PIServers().DefaultPIServer;

            server.Connect();

            List<PIPoint> points = PIPoint.FindPIPoints(server, "testSinusoid_*").ToList();
            if (points.Count == 0) {
                server.CreatePIPoints(Enumerable.Range(1, 6000).Select(x => "testSinusoid_" + x),
                    new Dictionary<String, Object>() { { "compressing", 0 } });
                points = PIPoint.FindPIPoints(server, "testSinusoid_*").ToList();
            }

            Console.WriteLine("Found {0} points", points.Count);

            TimeSpan chunkSize = new TimeSpan(5, 0, 0);
            for (DateTime start = new DateTime(2017, 1, 1); start < new DateTime(2018, 1, 1); start = start.Add(chunkSize)) {
                Console.WriteLine("Writing chunk starting at: " + start);
                List<AFValue> values = getSinusoidData(start, start.Add(chunkSize), new TimeSpan(0, 0, 15));

                Parallel.ForEach(points, point => {
                    point.UpdateValues(values, AFUpdateOption.Replace);
                });
            }

            server.Disconnect();
        }

        private static List<AFValue> getSinusoidData(DateTime start, DateTime end, TimeSpan samplePeriod) {
            List<AFValue> values = new List<AFValue>();
            TimeSpan period = new TimeSpan(0, 5, 0);

            start = new DateTime(start.Ticks - start.Ticks % samplePeriod.Ticks);
            for (DateTime currentKey = new DateTime(start.Ticks - start.Ticks % samplePeriod.Ticks);
                currentKey.Ticks <= end.Ticks; currentKey = currentKey.Add(samplePeriod)) {
                values.Add(new AFValue(Math.Sin(((currentKey.Ticks % period.Ticks) * 2 * Math.PI) / period.Ticks), new AFTime(currentKey)));
            }

            return values;
        }
    }
}