using System;
using System.Collections.Generic;
using System.Text;

namespace DQN_Env
{
    public class ProfitableDeletion : Move
    {
        public int rt, removedPosition;
        public double actual_obj_change;

        public ProfitableDeletion()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double removed_profit, double lc1, double cc1, int rt, int remPositioned)
        {
            this.moveObjective = mObj;
            this.objectiveChange = -removed_profit;
            this.costChangeRt1 = cc1;
            this.rt = rt;
            this.removedPosition = remPositioned;

            rt1 = rt;
            rt2 = -1;
            profitChangeRt1 = -removed_profit;
            loadChangeRt1 = lc1;
        }

        internal void StoreInformation(double mObj, double cc1, int rt, int remPositioned, double actual_obj_change)
        {
            this.moveObjective = mObj;
            this.costChangeRt1 = cc1;
            this.rt = rt;
            this.removedPosition = remPositioned;
            this.actual_obj_change = actual_obj_change;
        }

        public override void FindBestMove(Solution sol, Model m=null)
        {
            List<Route> routes = sol.routes;
            this.initializeMove();

            double actual_obj_change;

            for (int k = 0; k < routes.Count; k++)
            {
                Route rt = routes[k];


                for (int p = 1; p < rt.nodes.Count - 1; p++)
                {
                    Node removed = rt.nodes[p];
                    Node pred = rt.nodes[p - 1];
                    Node succ = rt.nodes[p + 1];

                    //copied_profits = new List<double> (this.profits);
                    //copied_profits[k] -= removed.profit;
                    //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;

                    //if (rt.profit <= best_infeasible_route_profit)
                    //{
                    //    actual_obj_change = 0;
                    //} else
                    //{
                    //   actual_obj_change = Math.Max(-removed.profit, best_infeasible_route_profit - rt.profit);
                    //}

                    double costChange1 = -pred.cost[removed.ID] - removed.cost[succ.ID];
                    double moveObjective = Model.M * (-removed.profit) - costChange1;
                    //double moveObjective = Model.M * (-removed.profit) + 0.1 * Model.M * actual_obj_change - costChange1;

                    //if (sol.profit - removed.profit < Promises.promises_nodes_ins[removed.ID])
                    //{
                    //    //continue;
                    //}

                    if (moveObjective > this.moveObjective)
                    {
                        this.StoreInformation(moveObjective, removed.profit, (-removed.demand), costChange1, k, p);
                        //this.StoreInformation(moveObjective, costChange1, k, p, actual_obj_change);
                        //ApplyMoufa(sol.DeepCopy(m), m);
                    }

                }
            }
        }

        internal void ApplyMoufa(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;

            Route rt = routes[this.rt];
            Node removed = rt.nodes[this.removedPosition];

            rt.nodes.RemoveAt(this.removedPosition);

            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 - removed.sTime;
            rt.profit -= removed.profit;
            rt.load -= removed.demand;
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;

            List<Tuple<int, int>> deleted = new List<Tuple<int, int>>();
            deleted.Add(Tuple.Create(routes[this.rt].nodes[this.removedPosition - 1].ID, routes[this.rt].nodes[this.removedPosition].ID));
            deleted.Add(Tuple.Create(routes[this.rt].nodes[this.removedPosition].ID, routes[this.rt].nodes[this.removedPosition + 1].ID));
            //Promises.MakePromises(deleted);


            Route rt = routes[this.rt];
            Node removed = rt.nodes[this.removedPosition];

            rt.nodes.RemoveAt(this.removedPosition);

            //sorted insertion
            //sortedUnservedProfitables.Insert(0, removed);
            //sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
            sol.sortedUnservedProfitables.Insert(0, removed);
            sol.sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
            removed.served = false;

            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 - removed.sTime;
            rt.profit -= removed.profit;
            rt.load -= removed.demand;

            sol.updateRouteInformation(this.rt, this.rt);

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //    throw new Exception("Problema grande!!!");
            //}
        }
    }
}

