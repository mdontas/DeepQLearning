using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DQN_Env
{
    class TSP
    {
        public static readonly int LKH_MAX_ROUTE_CAPACITY = 2000;  // max route size that LKH can solve 

        //[DllExport("tsp", CallingConvention = CallingConvention.Cdecl)]
        public static void TSP_On_Routes(Solution sol, Model m, int max_runs = 1, bool hide_errors = false)
        {
            foreach (Route rt in sol.routes)
            {
                LKHAlgorithm(m, rt, max_runs, hide_errors);
            }
            sol.updateRouteInformation(-1, -1);

            if (!hide_errors && !sol.Validate(m))
            {
                Console.WriteLine("Infeasible route in LKH TSP");
            }
        }


        /**
         * Run the  LKH 2 TSP (LKH-2.exe) algorithm http://webhotel4.ruc.dk/~keld/research/LKH/
         *
         * The LKH2.exe needs to be in the debug or release folder as well as dlls:
         *  i) VCRUNTIME140D.DLL https://www.dll-files.com/vcruntime140d.dll.html (x64 version)
         *  ii) UCRTBASED.DLL https://www.dll-files.com/ucrtbased.dll.html (x64 version)
         */
        public static void LKHAlgorithm(Model m, Route rt, int runs, bool hide_errors)
        {
            //Vars
            Dictionary<int, Node> mappingDict = new Dictionary<int, Node>();
            StringBuilder problemStr = new StringBuilder();

            // easy ref
            int n = rt.nodes.Count - 1; //leave second depot outside

            //var watch = System.Diagnostics.Stopwatch.StartNew();
            // Build problem string
            problemStr.Append(string.Format("{0} {1}", runs, n));
            Node nodeCur;
            int tspPrecisionFactor = 1000;
            for (int i = 0; i < n; i++)
            {
                nodeCur = rt.nodes[i];
                problemStr.Append(string.Format(" {0} {1} {2}", i + 1, (int)Math.Round(tspPrecisionFactor * nodeCur.x_coord), (int)Math.Round(tspPrecisionFactor * nodeCur.y_coord)));
                mappingDict.Add(i + 1, nodeCur);
            }
            //Console.WriteLine(problemStr.ToString());
            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //Console.WriteLine("Building string input: " + elapsedMs + " ms");

            //var watch2 = System.Diagnostics.Stopwatch.StartNew();
            // call the exe
            // Sample input "8 5 1 50 50 2 10 11 3 20 22 4 30 33 5 40 44";
            // runs nodes depot x y cust2 x2 y2 cust3 x3 y3 etc. the customer id are increasing starting from depot which is 1
            Process process = new Process();
            process.StartInfo.FileName = "LKH-2.exe";
            process.StartInfo.Arguments = problemStr.ToString();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();
            //watch2.Stop();
            //var elapsedMs2 = watch2.ElapsedMilliseconds;
            //Console.WriteLine("Optimizing: " + elapsedMs2 + " ms");

            //Console.WriteLine(err);
            //Console.WriteLine(output);

            int obj;
            List<int> newRoute = new List<int>();
            //var watch3 = System.Diagnostics.Stopwatch.StartNew();
            (obj, newRoute) = readObjAndTour(output);
            //watch3.Stop();
            //var elapsedMs3 = watch3.ElapsedMilliseconds;

            // chech if the solution is worse and rerun
            //if (obj > rt.time)
            //{
            //    if (runs == 2)
            //        Console.WriteLine("LKH failed to improve");
            //    //CountStuff.times_lkh_failed++;
            //    //LKHAlgorithm(m, sol, 5);
            //}
            if (obj < rt.cost)
            {
                // check if anything is changed
                bool changesExists = false;
                for (int i = 0; i < newRoute.Count - 1; i++)
                {
                    if (i + 1 != newRoute[i])
                    {
                        changesExists = true;
                        break;
                    }
                }

                if (changesExists)
                {
                    // recalculate time f
                    //double old_routeCost = 0; // rt.time;
                    //Node prev_node = rt.nodes[0];
                    //foreach (Node node in rt.nodes.GetRange(1, rt.nodes.Count - 1))
                    //{
                    //    old_routeCost += prev_node.cost[node.ID];
                    //    prev_node = node;
                    //}

                    // 1. Profit is the same

                    // 2. update the nodes lists
                    rt.nodes.Clear();

                    for (int i = 0; i < newRoute.Count; i++)
                    {
                        int idx = newRoute[i];
                        Node node = mappingDict[idx];
                        //rt.nodes_seq.Insert(rt.nodes_seq.Count - 1, node);
                        rt.nodes.Add(node);
                        //m.sets[node.set_id].in_route = true;
                    }
                    //rt.nodes_seq.Add(depot);
                    //rt.sets_included.Add(m.sets[depot.set_id]);

                    //// 3. update time
                    //rt.time = 0;
                    //for (int j = 1; j < rt.nodes.Count; j++)
                    //{
                    //    rt.time += rt.nodes[j - 1].cost[rt.nodes[j].ID];
                    //    //Console.WriteLine(m.dist_matrix[rt.nodes_seq[j - 1].id, rt.nodes_seq[j].id]);
                    //}


                    //if (old_routeCost != rt.time)
                    //{
                    //    //Console.WriteLine("Improvement from TSP: old distance = {0} --> optimized distance = {1}", old_routeCost, rt.time);
                    //}
                    //if (obj != rt.time)
                    //{
                    //    Console.WriteLine("Error in LKH TSP objective: LKH = {0} vs scratch = {1}", obj, rt.time); //attention maybe due to the precision error
                    //    //CountStuff.wrong_obj_in_lkh++;
                    //}
                    //watch4.Stop();
                    //var elapsedMs4 = watch4.ElapsedMilliseconds;
                    //Console.WriteLine("Storing solution: " + elapsedMs4 + " ms");
                }
            }
        }

        private static (int obj, List<int> newRoute) readObjAndTour(string output)
        {
            int obj = -1;
            List<int> newRoute = new List<int>();

            //split
            var lines = output.Split('\n');

            // TOUR_SECTION,1,3,2,27,51,59,50,35,36,8,28,21,18,26,34,33,19,32,17,16,31,29,20,30,25,22,23,24,14,13,55,38,43,42,12,11,10,15,9,44,39,54,56,53,52,37,41,40,58,57,47,48,46,49,45,6,7,5,4,1
            //COMMENT: Length = 46989

            for (int idx = lines.Length - 2; idx > -1; idx--)
            {
                if (lines[idx].StartsWith("TOUR_SECTION"))
                {
                    var splits = lines[idx].Split(',');
                    for (int i = 1; i < splits.Length; i++)
                    {
                        newRoute.Add(Int32.Parse(splits[i]));
                    }
                    splits = lines[idx - 1].Split(' ');
                    obj = Int32.Parse(splits[splits.Length - 1].Replace("\r", ""));
                    break;
                }
            }

            return (obj, newRoute);
        }
    }
}

