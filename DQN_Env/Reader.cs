using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;

namespace DQN_Env
{
    public class Reader
    {
        public static Dictionary<string, int> benchmarks = new Dictionary<string, int>();

        public static Model ReadProblemFile(string filepath)
        {
            List<Node> nodes = new List<Node>();
            FileInfo src = new FileInfo(filepath);
            char[] seperator = new char[2] { ' ', '\t' };
            string[] array;
            List<string> dt = new List<string>();
            String str;
            TextReader reader = src.OpenText();
            double x_coord, y_coord, take, sTime, profit;

            str = reader.ReadLine();
            str = reader.ReadLine();
            str = reader.ReadLine();
            array = str.Split(seperator, 1000);
            dt.Clear();
            for (int j = 0; j < array.Length; j++)
            { if (array[j] != "") dt.Add(array[j]); }
            int availableVehicles = int.Parse(dt[1]);
            //availableVehicles = 7;

            str = reader.ReadLine();
            array = str.Split(seperator, 1000);
            dt.Clear();
            for (int j = 0; j < array.Length; j++)
            { if (array[j] != "") dt.Add(array[j]); }
            int maxLoad = int.Parse(dt[1]);
            //maxLoad = 75;


            str = reader.ReadLine();
            array = str.Split(seperator, 1000);
            dt.Clear();
            for (int j = 0; j < array.Length; j++)
            { if (array[j] != "") dt.Add(array[j]); }
            int maxCost = int.Parse(dt[1]);
            //maxCost = 100;


            str = reader.ReadLine();
            str = reader.ReadLine();
            array = str.Split(seperator, 1000);
            dt.Clear();
            for (int j = 0; j < array.Length; j++)
            { if (array[j] != "") dt.Add(array[j]); }

            x_coord = double.Parse(dt[1]);
            y_coord = double.Parse(dt[2]);
            Node dp = new Node(x_coord, y_coord, 0, 0, 0, true);
            nodes.Add(dp);


            str = reader.ReadLine();

            str = reader.ReadLine();
            array = str.Split(seperator, 1000);
            dt.Clear();
            for (int j = 0; j < array.Length; j++)
            { if (array[j] != "") dt.Add(array[j]); }
            int tcs = int.Parse(dt[1]);

            str = reader.ReadLine();
            str = reader.ReadLine();

            for (int i = 0; i < tcs; i++)
            {
                str = reader.ReadLine();
                array = str.Split(seperator, 1000);
                dt.Clear();
                for (int j = 0; j < array.Length; j++)
                { if (array[j] != "") dt.Add(array[j]); }

                x_coord = double.Parse(dt[0]);
                y_coord = double.Parse(dt[1]);
                take = int.Parse(dt[2]);
                sTime = int.Parse(dt[3]);
                profit = (double.Parse(dt[4]));
                profit = profit == 0 ? 1 : profit;
                Node n = new Node(x_coord, y_coord, take, sTime, profit, false);
                nodes.Add(n);
            }

            string dataset = filepath.Split('/').Last();
            int liter_best = Get_Literature_Best(dataset);

            return new Model(dataset, availableVehicles, maxLoad, maxCost, nodes, liter_best);
        }

        private static Dictionary<string, int> ReadBenchmarks()
        {
            TextReader reader = new FileInfo("C:\\Users\\mdont\\Visual Studio\\CTOP_RL\\CTOP_RL\\Datasets\\benchmarks.csv").OpenText();
            string line = reader.ReadLine();  // first line is headers, we don't care
            line = reader.ReadLine();
            do
            {
                var pair = line.Split(';');
                benchmarks.Add(pair[0], Int16.Parse(pair[2]));
                line = reader.ReadLine();
            } while (line != null);

            return benchmarks;
        }

        private static int Get_Literature_Best(string dataset)
        {
            if (benchmarks.Count == 0)
            {
                ReadBenchmarks();
            }

            try
            {
                return benchmarks[dataset.Split('.')[0]];
            }
            catch (Exception e)
            {
                return -1;
            }
        }
    }
}

