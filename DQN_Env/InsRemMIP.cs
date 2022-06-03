using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace DQN_Env
{
    public class InsRemMIP
    {
        public static GRBEnv gurobiEnv = new GRBEnv();

        public static Random ran;
        public static List<Node> sortedUnservedProfitables;

        /**
        * forceChanges: true to force the MIP makes collectivelly at least minForcedIns insertions and minForcedMax deletions
        *  
        */
        [DllExport("insDelMIP", CallingConvention = CallingConvention.Cdecl)]
        public static void SolveInsRemMIP(Solution sol, Model m, int maxIns, int maxDel, int timeLimit = 60, int minForcedIns = 0, int minForcedDel = 0, bool forceChanges = false, bool forcePerRoute = false, bool hide_messages = false)
        {
            {
                for (int i = 0; i < m.nodes.Count; i++)
                {
                    m.nodes[i].ID_math = i;
                }

                List<Route> routes = sol.routes;
                double profitCollected = sol.profit;
                // params 
                bool addValidInequalities = false;

                // easy access
                double maxVal = m.maxCost;
                double oldProf = profitCollected;

                // create model
                try
                {
                    GRBModel SIDSubproblem = new GRBModel(gurobiEnv);

                    // SOP model params
                    SIDSubproblem.ModelName = "SIDSubproblem" + DateTime.Now.ToString("HH:mm:ss tt");
                    SIDSubproblem.Parameters.OutputFlag = 0; // Gurobi logging
                    //SIDSubproblem.Parameters.MIPGap = gapLimit; // termination condition, stop when reaching X% difference between lower and upper bound 
                    SIDSubproblem.Parameters.Threads = 1; // usually we use 1 thread when solving MIPs for reasons of direct comparisons
                    SIDSubproblem.Parameters.TimeLimit = timeLimit; // termination condition in seconds 

                    // Preprocessing
                    bool[] existsInSol = new bool[m.nodes.Count]; //array to store which nodes are visited 

                    // no need for initialization as false, this occurs by default
                    //for (int i = 0; i < m.nodes.Count; i++)
                    //{
                    //    Node node = m.nodes[i];
                    //    {
                    //        existsInSol[node.ID] = false;
                    //    }
                    //}

                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];
                        for (int i = 0; i < rt.nodes.Count; i++)
                        {
                            Node node = rt.nodes[i];
                            existsInSol[node.ID_math] = true;
                        }
                    }

                    // Calculate constants
                    double[] remCost = new double[m.nodes.Count];
                    double[,] addCost = new double[m.nodes.Count, routes.Count];
                    int[,] addPosition = new int[m.nodes.Count, routes.Count];


                    // also where to add the nodes 
                    for (int i = 0; i < m.nodes.Count; i++)
                    {
                        Node node = m.nodes[i];
                        for (int r = 0; r < routes.Count; r++)
                        {
                            addCost[node.ID_math, r] = 2 * maxVal;
                        }
                        remCost[node.ID_math] = 0;
                    }

                    // Removal savings 
                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];
                        for (int i = 1; i < rt.nodes.Count - 1; i++)
                        {
                            Node pred = rt.nodes[i - 1];
                            Node me = rt.nodes[i];
                            Node suc = rt.nodes[i + 1];

                            remCost[me.ID_math] = pred.cost[suc.ID] - pred.cost[me.ID] - me.cost[suc.ID]; // negative cost
                        }
                    }


                    // Addition costs
                    for (int i = 0; i < m.nodes.Count; i++)
                    {
                        Node me = m.nodes[i];
                        if (!existsInSol[me.ID_math])
                        {
                            for (int r = 0; r < routes.Count; r++)
                            {
                                Route rt = routes[r];

                                double minCost = maxVal;

                                for (int j = 1; j < rt.nodes.Count; j++)
                                {
                                    Node pred = rt.nodes[j - 1];
                                    Node suc = rt.nodes[j];

                                    double insCost = pred.cost[me.ID] + me.cost[suc.ID] - pred.cost[suc.ID]; // positive cost

                                    if (insCost < minCost)
                                    {
                                        minCost = insCost;
                                        addPosition[me.ID_math, r] = j;
                                    }
                                }
                                addCost[me.ID_math, r] = minCost;
                            }
                        }
                    }

                    // ============================================================================================================================================================//
                    // Decision variables declaration

                    GRBVar[,] add = new GRBVar[m.nodes.Count, routes.Count]; // addCost
                    GRBVar[] rem = new GRBVar[m.nodes.Count]; // remCost
                    //GRBVar[] z = new GRBVar[setsNum];

                    for (int i = 1; i < m.nodes.Count; i++) // no depot 
                    {
                        Node node = m.nodes[i];

                        for (int r = 0; r < routes.Count; r++)
                        {
                            Route rt = routes[r];

                            if (existsInSol[node.ID_math])
                            {
                                rem[node.ID_math] = SIDSubproblem.AddVar(0.0, 1.0, -node.profit, GRB.BINARY, "rem_" + node.ID_math);
                                add[node.ID_math, r] = SIDSubproblem.AddVar(0.0, 0.0, node.profit, GRB.BINARY, "add_" + node.ID_math + "_" + r);
                            }
                            else
                            {
                                rem[node.ID_math] = SIDSubproblem.AddVar(0.0, 0.0, -node.profit, GRB.BINARY, "rem_" + node.ID_math);
                                add[node.ID_math, r] = SIDSubproblem.AddVar(0.0, 1.0, node.profit, GRB.BINARY, "add_" + node.ID_math + "_" + r);
                            }
                        }
                    }

                    // ============================================================================================================================================================//
                    // Objective sense
                    SIDSubproblem.ModelSense = GRB.MAXIMIZE;

                    // ============================================================================================================================================================//


                    // TODO: make proportional insertions deletions (e.g., 20% of the route lenght minus the depots)

                    // Constraints
                    // Constraint 1: Max insertions
                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];

                        GRBLinExpr exp1 = 0.0;
                        for (int i = 1; i < m.nodes.Count; i++)
                        {
                            Node node = m.nodes[i];

                            if (!existsInSol[node.ID_math])
                            {
                                exp1.AddTerm(1.0, add[node.ID_math, r]);
                            }
                        }
                        SIDSubproblem.AddConstr(exp1 <= maxIns, "con1_" + r + "_max_insertions");
                        if (forcePerRoute)
                        {
                            SIDSubproblem.AddConstr(exp1 >= 2, "con1_" + r + "_min_insertions");

                        }

                    }


                    // Constraint 2: Max deletions
                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];

                        GRBLinExpr exp1 = 0.0;
                        for (int i = 1; i < rt.nodes.Count - 1; i++)
                        {
                            Node node = rt.nodes[i];

                            exp1.AddTerm(1.0, rem[node.ID_math]);
                        }
                        SIDSubproblem.AddConstr(exp1 <= maxDel, "con2_" + r + "_max_deletions");
                        if (forcePerRoute)
                        {
                            SIDSubproblem.AddConstr(exp1 >= 1, "con2_" + r + "_min_deletions");
                        }

                    }


                    // Constraint 3: Capacity 
                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];

                        GRBLinExpr exp1 = 0.0;
                        exp1.AddConstant(rt.load);

                        for (int i = 1; i < m.nodes.Count; i++)
                        {
                            Node node = m.nodes[i];

                            if (!existsInSol[node.ID_math])
                            {
                                exp1.AddTerm(node.demand, add[node.ID_math, r]);
                            }
                        }
                        for (int i = 1; i < rt.nodes.Count - 1; i++)
                        {
                            Node node = rt.nodes[i];

                            exp1.AddTerm(-node.demand, rem[node.ID_math]);
                        }
                        SIDSubproblem.AddConstr(exp1 <= m.maxLoad, "con3_" + r + "_capacity");
                    }


                    // Constraint 4: Time 
                    for (int r = 0; r < routes.Count; r++)
                    {
                        Route rt = routes[r];

                        GRBLinExpr exp1 = 0.0;
                        exp1.AddConstant(rt.time);
                        for (int i = 1; i < m.nodes.Count; i++)
                        {
                            Node node = m.nodes[i];

                            if (!existsInSol[node.ID_math])
                            {
                                exp1.AddTerm(addCost[node.ID_math, r] + node.sTime, add[node.ID_math, r]);
                            }
                        }
                        for (int i = 1; i < rt.nodes.Count - 1; i++)
                        {
                            Node node = rt.nodes[i];

                            exp1.AddTerm(remCost[node.ID_math] - node.sTime, rem[node.ID_math]);
                        }
                        SIDSubproblem.AddConstr(exp1 <= m.maxCost, "con4_" + r + "_timecost");
                    }


                    // Constraint 5: Each node can be inserted once
                    for (int i = 1; i < m.nodes.Count; i++)
                    {
                        Node node = m.nodes[i];
                        GRBLinExpr exp5 = 0.0;

                        for (int r = 0; r < routes.Count; r++)
                        {
                            exp5.AddTerm(1.0, add[node.ID_math, r]);
                        }
                        SIDSubproblem.AddConstr(exp5 <= 1, "con5_" + node.ID_math);
                    }

                    // Force minimum changes
                    if (forceChanges)
                    {
                        // Constraint 6: force changes insertions
                        GRBLinExpr exp3 = 0.0;
                        for (int i = 1; i < m.nodes.Count; i++)
                        {
                            Node node = m.nodes[i];

                            for (int r = 0; r < routes.Count; r++)
                            {
                                exp3.AddTerm(1.0, add[node.ID_math, r]);
                            }
                        }
                        SIDSubproblem.AddConstr(exp3 >= minForcedIns, "con6_min_insertions");


                        // Constraint 7: force changes deletions
                        GRBLinExpr exp2 = 0.0;
                        for (int i = 1; i < m.nodes.Count; i++)
                        {
                            Node node = m.nodes[i];

                            if (existsInSol[node.ID_math])
                            {
                                exp2.AddTerm(1.0, rem[node.ID_math]);
                            }
                        }
                        SIDSubproblem.AddConstr(exp2 >= minForcedDel, "con7_min_deletions");
                    }

                    //int minForcedIns = 0, int minForcedMax = 0, bool forceChanges = false


                    // ============================================================================================================================================================//
                    // Valid inequalities
                    // Valid inequalities are extra not required constraints that are used to cut down the solution space
                    if (addValidInequalities)
                    {

                    }

                    // ==================================================Optimize====================================================================
                    SIDSubproblem.Optimize();


                    // ==================================================Results====================================================================
                    switch (SIDSubproblem.Status)
                    {
                        case GRB.Status.OPTIMAL:
                        case GRB.Status.TIME_LIMIT:
                            {
                                //Console.WriteLine(SIDSubproblem.ObjVal + "\nNodes to remove: ");
                                //foreach (Node node in m.nodes.GetRange(1, m.nodes.Count - 1))
                                //{
                                //    if (IsEqual(rem[node.ID].X, 1.0, 1e-3))
                                //    {
                                //        Console.Write(node.ID + " ");
                                //    }
                                //}
                                for (int r = 0; r < routes.Count; r++)
                                {
                                    Route rt = routes[r];
                                    //Console.WriteLine(rt);
                                    if (!hide_messages)
                                        Console.WriteLine("Init Route {0}     : time {1}, load {2}, profit {3} and nodes {4}", r, rt.time.ToString("0.00"), rt.load.ToString("0.00"), rt.profit.ToString("0.00"), rt.nodes);

                                    // UPDATE DELETED
                                    for (int i = rt.nodes.Count - 2; i > 0; i--)
                                    {
                                        Node toBeRemoved = rt.nodes[i];

                                        if (IsEqual(rem[toBeRemoved.ID_math].X, 1.0, 1e-3)) //remove customer
                                        {
                                            if (!hide_messages)
                                                Console.WriteLine("Removing node {0} from route {3} with saving of {1} and profit {2}", toBeRemoved.ID_math, remCost[toBeRemoved.ID_math], toBeRemoved.profit, r);

                                            rt.nodes.Remove(toBeRemoved);
                                            //Promises.promises_nodes_del[toBeRemoved.ID_math] = profitCollected;
                                            sol.sortedUnservedProfitables.Insert(0, toBeRemoved);
                                            sol.sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
                                            toBeRemoved.served = false;

                                            //rt.cost += pdel.costChangeRt1;
                                            //rt.time += pdel.costChangeRt1 - removed.sTime;
                                            //rt.profit -= removed.profit;
                                            //rt.load -= removed.demand;

                                            profitCollected -= toBeRemoved.profit;
                                            sol.updateRouteInformation(r, r, hide_errors: true);
                                            //updateRouteInformation(r, r);

                                        }
                                    }
                                    if (!hide_messages)
                                        Console.WriteLine("Route {0} after del: time {1}, load {2}, profit {3} and nodes {4}", r, rt.time.ToString("0.00"), rt.load.ToString("0.00"), rt.profit.ToString("0.00"), rt.nodes);
                                    //Console.WriteLine(rt);

                                    // UPDATE INSERTED
                                    for (int i = 1; i < m.nodes.Count; i++)
                                    {
                                        Node toBeInserted = m.nodes[i];

                                        if (IsEqual(add[toBeInserted.ID_math, r].X, 1.0, 1e-3)) //insert customer
                                        {
                                            if (!hide_messages)
                                                Console.WriteLine("Inserting node {0} to route {3} with extra cost of {1} and profit {2}", toBeInserted.ID_math, addCost[toBeInserted.ID_math, r], toBeInserted.profit, r);


                                            toBeInserted.served = true;

                                            // insert to best position (this may differ from addPosition[x.IDV, r] when multiple nodes are introduced and removed) 
                                            double minCost = maxVal;
                                            int pos = -1;
                                            for (int ii = 1; ii < rt.nodes.Count; ii++)
                                            {
                                                Node pred = rt.nodes[ii - 1];
                                                Node suc = rt.nodes[ii];

                                                double insCost = pred.cost[toBeInserted.ID] + toBeInserted.cost[suc.ID] - pred.cost[suc.ID];

                                                if (insCost < minCost)
                                                {
                                                    minCost = insCost;
                                                    pos = ii;
                                                }

                                            }
                                            if (rt.nodes.Count == 2)
                                            {
                                                pos = 1;
                                            }
                                            rt.nodes.Insert(pos, toBeInserted);


                                            sol.sortedUnservedProfitables.Remove(toBeInserted);

                                            //Promises.promises_nodes_ins[toBeInserted.ID_math] = profitCollected;


                                            //rt.cost += pIns.costChangeRt1;
                                            //rt.time += pIns.costChangeRt1 + ins.sTime;
                                            //rt.load = rt.load + ins.demand;

                                            profitCollected += toBeInserted.profit;
                                            sol.updateRouteInformation(r, r, hide_errors: true);
                                            //sol.actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
                                            //updateRouteInformation(r, r);

                                        }
                                    }
                                    if (!hide_messages)
                                        Console.WriteLine("Route {0} after ins: time {1}, load {2}, profit {3} and nodes {4}", r, rt.time.ToString("0.00"), rt.load.ToString("0.00"), rt.profit.ToString("0.00"), rt.nodes);
                                    //Console.WriteLine(rt);

                                    // Run TSP for each route
                                    //MathProgramming.SolveTSP(m, sol, secLimit, mipGapLimit, heur);


                                    // FIX INFEASIBILITIES
                                    //if (rt.time > m.maxCost) //infeasible due to approximation
                                    //{
                                    //    RestoreTimeInfeasibility(add, sol, m, r, rt, false);
                                    //}
                                    //if (rt.load > m.maxLoad)
                                    //{
                                    //    RestoreLoadInfeasibility(add, sol, m, r, rt, false);
                                    //}

                                    //sol.Validate(m);
                                }
                                if (!hide_messages)
                                    Console.WriteLine("Simultaneous Insertions/Deletions ({0},{1}) objective change: {2}", maxIns, maxDel, profitCollected - oldProf);

                                for (int r = 0; r < sol.routes.Count; r++)
                                {
                                    Route rt = sol.routes[r];
                                    if (rt.time > m.maxCost) //infeasible due to approximation
                                    {
                                        RestoreTimeInfeasibility(add, sol, m, r, rt, false);
                                    }
                                    if (rt.load > m.maxLoad)
                                    {
                                        RestoreLoadInfeasibility(add, sol, m, r, rt, false);
                                    }
                                }

                                //sol.Validate(m);

                                /*
                                for (int r = 0; r < routes.Count; r++)
                                {
                                    Route rt = routes[r];
                                    while (rt.time > maxCost || rt.load > maxLoad)
                                    {
                                        SolveInsRemMIP(0, 1, 60);
                                    }
                                }
                                */

                                for (int r = 0; r < routes.Count; r++)
                                {
                                    Route rt = routes[r];
                                    while (rt.time > m.maxCost || rt.load > m.maxLoad)
                                    {
                                        Console.WriteLine("MIP error");
                                    }
                                }

                                /*
                                // for random instances this may take a while. time limit in seconds and MIP gap may be added.
                                int secLimit = 30;
                                double mipGapLimit = 0.005; //0.5%
                                double heur = 0.80; //80% 

                                if (sol.total_time > m.t_max) //infeasible due to approximation
                                {
                                    RestoreDistanceInfeasibility(m, sol, add);
                                }

                                MathProgramming.SolveTSP(m, sol, secLimit, mipGapLimit, heur);

                                if (!sol.route.CheckRoute(m))
                                {
                                    Console.WriteLine("Error SIDSubproblem");
                                }
                                //Console.WriteLine("Simultaneous Insertions/Deletions ({0},{1}) objective change: {2} (Dist: {3} --> {4})", maxIns,maxDel, sol.total_profit-oldProf, oldTime, sol.total_time);
                                //MathProgramming.OptimizeNodesGivenSets(m, sol);
                                */

                                break;
                            }
                        case GRB.Status.INFEASIBLE:
                            {
                                Console.WriteLine("Model is infeasible");
                                // compute and write out IIS
                                SIDSubproblem.ComputeIIS();
                                SIDSubproblem.Write("SIDSubproblem" + SIDSubproblem.ModelName + ".ilp");
                                break;
                            }
                        case GRB.Status.UNBOUNDED:
                            {
                                Console.WriteLine("Model is unbounded");
                                break;
                            }
                        default:
                            {
                                Console.WriteLine("Optimization was stopped with status = " + SIDSubproblem.Status);
                                break;
                            }

                    }
                    // Dispose of model
                    SIDSubproblem.Dispose();
                    //gurobiEnv.Dispose();
                }
                catch (GRBException e)
                {
                    Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
                }
            }
        }

        // Heuristic method to restore the t_max infeasibility by sequentially removing the node with the best distance change/profit ratio
        public static void RestoreTimeInfeasibility(GRBVar[,] add, Solution sol, Model m, int r, Route rt, bool random = false)
        {
            //int[] savings = new int[m.sets.Count];
            while (rt.time > rt.maxCost) //infeasible due to approximation
            {
                double[] savings = new double[rt.nodes.Count];

                //Calculate savings
                //for (int l = 0; l < rt.nodes.Count; l++)
                //{
                //    savings[l] = 0;
                //}
                for (int l = 1; l < rt.nodes.Count - 1; l++) // nodes except depots
                {
                    Node me = rt.nodes[l];
                    Node pred = rt.nodes[l - 1];
                    Node suc = rt.nodes[l + 1];
                    savings[l] = (double)(-pred.cost[suc.ID] + me.sTime + pred.cost[me.ID] + me.cost[suc.ID]) / me.profit;
                    if (IsEqual(add[me.ID_math, r].X, 1.0, 1e-3)) // if just added avoid  removing
                    {
                        savings[l] = 0.0001;
                    }
                }

                //Find best set to remove
                double max = savings.Max();
                int idx = Array.IndexOf(savings, max);
                if (random)
                    idx = ran.Next(1, savings.Length - 1);
                Node nodeRem = rt.nodes[idx];
                Node nodeRem_pred = rt.nodes[idx - 1];
                Node nodeRem_suc = rt.nodes[idx + 1];

                double saving = nodeRem_pred.cost[nodeRem_suc.ID] - nodeRem_pred.cost[nodeRem.ID] - nodeRem.cost[nodeRem_suc.ID] - nodeRem.sTime;

                rt.nodes.Remove(nodeRem);
                sol.sortedUnservedProfitables.Insert(0, nodeRem);
                sol.sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
                nodeRem.served = false;

                sol.updateRouteInformation(r, r, hide_errors: true);
                //sol.actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
                //Console.WriteLine("Removing node {0} with saving of {1} and profit {2}", nodeRem.ID, saving, nodeRem.profit);
            }
        }

        public static void RestoreLoadInfeasibility(GRBVar[,] add, Solution sol, Model m, int r, Route rt, bool random = false)
        {
            //int[] savings = new int[m.sets.Count];
            while (rt.load > rt.maxLoad) //infeasible due to approximation
            {
                double[] savings = new double[rt.nodes.Count];

                //Calculate savings
                for (int l = 0; l < rt.nodes.Count; l++)
                {
                    savings[l] = 0;
                }
                for (int l = 1; l < rt.nodes.Count - 1; l++) // nodes except depots
                {
                    Node me = rt.nodes[l];
                    Node pred = rt.nodes[l - 1];
                    Node suc = rt.nodes[l + 1];
                    savings[l] = (double)(me.demand) / me.profit;
                    if (IsEqual(add[me.ID_math, r].X, 1.0, 1e-3)) // if just added avoid  removing
                    {
                        savings[l] = 0.0001;
                    }
                }

                //Find best set to remove
                double max = savings.Max();
                int idx = Array.IndexOf(savings, max);
                if (random)
                    idx = ran.Next(1, savings.Length - 1);
                Node nodeRem = rt.nodes[idx];
                Node nodeRem_pred = rt.nodes[idx - 1];
                Node nodeRem_suc = rt.nodes[idx + 1];

                double saving = nodeRem.demand;

                rt.nodes.Remove(nodeRem);
                sortedUnservedProfitables.Insert(0, nodeRem);
                sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
                nodeRem.served = false;

                sol.updateRouteInformation(r, r, hide_errors: true);

                // Console.WriteLine("Removing set {0} with saving of {1} and profit {2}", setRem.id, saving, setRem.profit);
            }
        }

        public static bool IsEqual(double a, double b, double prec)
        {
            return Math.Abs(a - b) > prec ? false : true;
        }
    }
}

