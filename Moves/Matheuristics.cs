using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DQN_Env
{
    public class InsDelMove : Move
    {
        public int maxIns;
        public int maxDel;
        public int minForcedIns;
        public int minForcedDel;
        public double profit_before;
        public double cost_before;

        [DllImport("Actor.dll", EntryPoint = "insDelMIP", CallingConvention = CallingConvention.Cdecl)]
        public static extern void InsDelMIP(Solution sol, Model m, int maxIns, int maxDel, int timeLimit = 60, int minForcedIns = 0,
                                            int minForcedDel = 0, bool forceChanges = false, bool forcePerRoute = false, bool hide_messages = false);

        public override void FindBestMove(Solution sol, Model m = null)
        {
            //maxIns = ran.Next(2, 4); // 2;
            //maxDel = ran.Next(2, 4); // 2;
            maxIns = 3; // 2;
            maxDel = 3; // 2;
            minForcedIns = 2;
            minForcedDel = 1;
            profit_before = sol.profit;
            cost_before = sol.time;

            dummy_sol = sol.DeepCopy(m);
            InsDelMIP(dummy_sol, m, maxIns, maxDel, timeLimit: 20, minForcedIns, minForcedDel, true, false, hide_messages: true);
            //InsRemMIP.SolveInsRemMIP(dummy_sol, m, maxIns, maxDel, timeLimit: 20, minForcedIns, minForcedDel, true, false, hide_messages: true);
            objectiveChange = dummy_sol.profit - profit_before;
            //costChangeRt1 = dummy_sol.time - cost_before;
            //costChangeRt2 = 0;
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            sol = dummy_sol;
        }
    }


    public class TspMove : Move
    {
        public int max_runs;
        public int maxDel;
        public int minForcedIns;
        public int minForcedDel;
        public double cost_before;

        public override void FindBestMove(Solution sol, Model m = null)
        {
            max_runs = 1;
            cost_before = sol.time;
            //foreach (Route rt in sol.routes)
            //{
            //    Console.Write(rt.time + " ");
            //}
            dummy_sol = sol.DeepCopy(m);
            TSP.TSP_On_Routes(dummy_sol, m, max_runs, hide_errors: false);

            objectiveChange = 0;
            //costChangeRt1 = sol.time - cost_before;
            //costChangeRt2 = 0;
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            sol = dummy_sol;
        }
    }
}
