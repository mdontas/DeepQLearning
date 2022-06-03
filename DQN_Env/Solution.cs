using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace DQN_Env
{
    public class Solution
    {
        public List<Route> routes;
        public double profit;
        public double actual_profit;
        public double time;
        public double actual_time;
        public double lengthCovered; // total distance covered
        public List<Node> sortedUnservedProfitables;

        public Solution(Model m)  // constructor for initial solution objects : [depot, depot]
        {
            Node depot = m.nodes[0];
            routes = new List<Route>();
            for (int i = 0; i < m.vehicles; i++)
            {
                Node dt = depot;
                if (i > 0)
                {
                    dt = new Node();
                    dt.ID = depot.ID;
                    dt.cost = depot.cost;
                    dt.x_coord = depot.x_coord;
                    dt.y_coord = depot.y_coord;
                    dt.averageCostToAllNodes = depot.averageCostToAllNodes;
                    dt.IDdepot = i;
                }
                routes.Add(new Route(new List<Node> { dt, dt}, m));
            }
        }

        public Solution(List<Route> rts, Model m)
        {
            routes = new List<Route>();
            for (int i = 0; i < rts.Count; i++)
            {
                Route rt = new Route(new List<Node>(rts[i].nodes), m);
                routes.Add(rt);
            }
            this.time = routes.Select(x => x.cost).Sum();
            this.profit = routes.Select(x => x.profit).Sum();
            this.lengthCovered = routes.Select(x => x.cost).Sum();
            updateRouteInformation(-1, -1);
        }

        public Solution()
        {
        }

        public Solution DeepCopy(Model m)
        {
            Solution sol = new Solution(this.routes, m);
            sol.sortedUnservedProfitables = this.sortedUnservedProfitables;
            return sol;
        }

        public void updateRouteInformation(int rtInd1, int rtInd2, bool hide_errors = false)
        {
            for (int i = 0; i < routes.Count; i++)
            {
                if (i == rtInd1 || i == rtInd2 || ((rtInd1 == rtInd2) && (rtInd1 == -1)))
                {
                    Route r = routes[i];
                    r.timeTillHere_st_incl.Clear();
                    r.timeTillHere_st_incl.Add((double)0.0);
                    r.demandTillHere.Clear();
                    r.demandTillHere.Add((int)0.0);
                    r.nodes[0].IDP = 0;
                    r.nodes[0].IDR = i;

                    double rLoad = 0;
                    double rProf = 0;
                    double rCost = 0;
                    double rTime = 0;

                    for (int j = 0; j < r.nodes.Count; j++)
                    {
                        if (j == 0)
                        {
                            Node n = r.nodes[j];
                        }
                        if (j > 0)
                        {
                            Node n = r.nodes[j];
                            Node n_prev = r.nodes[j - 1];
                            double time_ = r.timeTillHere_st_incl[j - 1] + n_prev.cost[n.ID] + n.sTime;
                            double dem_ = r.demandTillHere[j - 1] + n.take;
                            rLoad += n.take;
                            r.timeTillHere_st_incl.Add(time_);
                            r.demandTillHere.Add(dem_);
                            if (j != r.nodes.Count - 1)
                            {
                                n.IDR = i;
                                n.IDP = j;
                            }
                            rProf += n.profit;
                            rCost += n_prev.cost[n.ID];
                            rTime += n_prev.cost[n.ID] + n_prev.sTime;
                        }
                    }
                    r.profit = rProf;
                    r.cost = rCost;
                    r.objective = Model.M * rProf - rCost;
                    r.load = rLoad;
                    r.time = rTime;

                    /*
                    if (r.time > maxCost)
                    {
                        Console.WriteLine("Time Limit route " + i);
                    }
                    if (r.load > maxLoad)
                    {
                        Console.WriteLine("Load Limit route " + i);
                    }
                    */

                    if (!hide_errors)
                    {
                        if (r.time > r.maxCost)
                        {
                            Console.WriteLine("Updating route...: Time Limit Issue route {0}: {1}/{2}", i, r.time.ToString("0.00"), r.maxCost.ToString("0.00"));
                            throw new Exception();
                        }
                        if (r.load > r.maxLoad)
                        {
                            Console.WriteLine("Updating route...: Load Limit Issue route {0}: {1}/{2}", i, r.load.ToString("0.00"), r.maxLoad.ToString("0.00"));
                        }
                    }
                }
            }
            this.time = routes.Select(x => x.time).Sum();
            this.profit = routes.Select(x => x.profit).Sum();
            this.lengthCovered = routes.Select(x => x.cost).Sum();
        }

        public void UpdateServedNodes(Model m)
        {
            foreach (Node n in m.customers)
            {
                if (n.InSol(this))
                {
                    n.served = true;
                }
                else
                {
                    n.served = false;
                }
            }
        }

        //public Solution Append(Solution sol, Model m)  // unifies two separate solution objects into a single one
        //{
        //    List<Route> unified_routes = (List<Route>) this.routes.Concat(sol.routes).ToList();
        //    return new Solution(unified_routes, m);
        //}

        public bool Validate(Model m)  // check whether the solution violates any constraints
        {
            //if (!CheckOccurences(m))  // check whether served nodes show up 1 time and unserved 0 times
            //{
            //    return false;
            //}

            double profitCollected = 0;
            double lengthCovered = 0;
            for (int i = 0; i < routes.Count; i++)
            {
                Route rt = routes[i];
                double rp = 0;
                double rl = 0;
                double rload = 0;
                for (int j = 0; j < rt.nodes.Count - 1; j++)
                {
                    Node n = rt.nodes[j];
                    Node nn = rt.nodes[j + 1];

                    rp += n.profit;
                    rl += n.cost[nn.ID];
                    rload += n.demand;
                }
                if (rt.profit != rp)
                {
                    Console.WriteLine("Profit Issue route {0}", i);
                    return false;
                }
                if (rt.cost != rl)
                {
                    Console.WriteLine("Cost Issue route {0}", i);
                    return false;
                }
                if (rt.load != rload)
                {
                    Console.WriteLine("Load Issue route {0}", i);
                    return false;
                }

                if (rt.time > m.maxCost)
                {
                    Console.WriteLine("Time Limit Issue route {0}: {1}/{2}", i, rt.time.ToString("0.00"), m.maxCost.ToString("0.00"));
                    return false;
                }
                if (rt.load > m.maxLoad)
                {
                    Console.WriteLine("Load Limit Issue route {0}: {1}/{2}", i, rt.load.ToString("0.00"), m.maxLoad.ToString("0.00"));
                    return false;
                }

                profitCollected += rt.profit;
                lengthCovered += rt.cost;
            }
            //objective = M * profitCollected - lengthCovered;
            return true;
        }

        private bool CheckOccurences(Model m)
        {
            List<Route> routes = this.routes;
            for (int i = 0; i < m.nodes.Count; i++)
            {
                Node n = m.nodes[i];
                int occ = 0;

                for (int j = 0; j < routes.Count; j++)
                {
                    Route rt = routes[j];

                    for (int k = 1; k < rt.nodes.Count - 1; k++)
                    {
                        Node cc = rt.nodes[k];
                        if (cc == n)
                        {
                            occ++;
                        }
                    }
                }

                if ((n.served && occ != 1) || (!n.served && occ != 0))
                {
                    Console.WriteLine("Check!!!! Problem at node " + n.ID);
                    return false;
                }
            }
            return true;
        }

        override
        public string ToString()
        {
            string sol = "";
            for (int i = 0; i < this.routes.Count; i++)
            {
                sol += "Vehicle " + (i + 1) + ": " + routes[i].ToString();
            }
            sol += "\nMaxDuration: " + routes[0].maxCost + "\nMaxCapacity: " + routes[0].maxLoad //+ "\nTime: " +  
                + "\nTotal cost: " + time + "\nTotal profit: " + profit + "\nTotal actual profit: " + actual_profit
                + "\n---------------------------------------------------\n";
            return sol;
        }

        public void ExportSolution(string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            writer.Write(this.ToString());
            writer.Close();
        }
    }
}

