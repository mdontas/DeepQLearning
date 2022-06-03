using System;
using System.Collections.Generic;
using System.Text;

namespace DQN_Env
{
    public class ProfitableSwap : Move
    {
        public int rt, removedPosition, sortedPos;
        public double actual_obj_change;

        public ProfitableSwap()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double profit_diff, double lc1, double cc1, int insertedPos, int rt, int remPositioned)
        {
            this.moveObjective = mObj;
            this.objectiveChange = profit_diff;
            this.costChangeRt1 = cc1;
            sortedPos = insertedPos;
            this.rt = rt;
            this.removedPosition = remPositioned;

            rt1 = rt;
            rt2 = -1;
            profitChangeRt1 = profit_diff;
            loadChangeRt1 = lc1;
        }

        internal void StoreInformation(double mObj, double profit_diff, double cc1, int insertedPos, int rt, int remPositioned, double actual_obj_change)
        {
            this.moveObjective = mObj;
            this.objectiveChange = profit_diff;
            this.costChangeRt1 = cc1;
            sortedPos = insertedPos;
            this.rt = rt;
            this.removedPosition = remPositioned;
            this.actual_obj_change = actual_obj_change;
        }

        public override void FindBestMove(Solution sol, Model m)
        {
            bool diversification = false;
            //if (ran.Next() > 0.8 && profitCollected < 0.99 * bestProfit)
            //    diversification = true;

            List<Route> routes = sol.routes;
            List<Tuple<int, int>> toBeCrt = new List<Tuple<int, int>>();

            this.initializeMove();

            double actual_obj_change, profit_diff;

            for (int i = 0; i < sol.sortedUnservedProfitables.Count; i++)
            {
                Node inserted = sol.sortedUnservedProfitables[i];

                for (int k = 0; k < routes.Count; k++)
                {
                    Route rt = routes[k];

                    for (int p = 1; p < rt.nodes.Count - 1; p++)
                    {
                        Node removed = rt.nodes[p];
                        Node pred = rt.nodes[p - 1];
                        Node succ = rt.nodes[p+1];
                        double costChange1 = pred.cost[inserted.ID] + inserted.cost[succ.ID] - pred.cost[removed.ID] - removed.cost[succ.ID];

                        //copied_profits = new List<double>(this.profits);
                        //copied_profits[k] += inserted.profit - removed.profit;
                        //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;

                        if (rt.load + inserted.demand - removed.demand > m.maxLoad)
                        {
                            continue;
                        }
                        if (rt.time + costChange1 + inserted.sTime > m.maxCost)
                        {
                            continue;
                        }

                        //// condition to avoid examining worse moves
                        if (Model.M * (inserted.profit - removed.profit) < this.moveObjective)
                        {
                            continue;
                        }

                        toBeCrt.Clear();
                        toBeCrt.Add(Tuple.Create(pred.ID, inserted.ID));
                        toBeCrt.Add(Tuple.Create(inserted.ID, succ.ID));

                        //profit_diff = inserted.profit - removed.profit;
                        ////int e = 0;
                        ////if (inserted.ID == 333 && removed.ID == 14)
                        ////{
                        ////    e = 1;
                        ////}
                        //if (profit_diff >= 0)
                        //{
                        //    if (rt.profit >= worst_feasible_route_profit)
                        //    {
                        //        actual_obj_change = profit_diff;
                        //    }
                        //    else
                        //    {
                        //        actual_obj_change = Math.Max(rt.profit + profit_diff - worst_feasible_route_profit, 0);
                        //    }
                        //}
                        //else
                        //{
                        //    if (rt.profit <= best_infeasible_route_profit)
                        //    {
                        //        actual_obj_change = 0;
                        //    }
                        //    else
                        //    {
                        //        actual_obj_change = Math.Max(profit_diff, best_infeasible_route_profit - rt.profit);
                        //    }
                        //}

                        double moveObjective = Model.M * (inserted.profit - removed.profit) - costChange1;
                        //double moveObjective = Model.M * (inserted.profit - removed.profit) + 0.1 * Model.M * actual_obj_change - costChange1;

                        //if (!Promises.MoveKeepsPromises(toBeCrt, inserted, inserted.profit - removed.profit, sol.profit))
                        //if (!Promises.MoveKeepsPromises(toBeCrt, inserted, actual_obj_change, sol.actual_profit))
                        //{
                        //    continue;
                        //}

                        //if (sol.profit - removed.profit < Promises.promises_nodes_ins[removed.ID])
                        //{
                        //    //continue;
                        //}

                        //if (diversification)
                        //{
                        //    moveObjective = Model.M * ((inserted.profit / Math.Pow(inserted.demand, 0.5)) - (removed.profit / Math.Pow(removed.demand, 0.5))) - costChange1;
                        //}

                        if (moveObjective > this.moveObjective)
                        {
                            this.StoreInformation(moveObjective, inserted.profit - removed.profit, inserted.demand - removed.demand, costChange1, i, k, p);
                            //this.StoreInformation(moveObjective, inserted.profit - removed.profit, costChange1, i, k, p, actual_obj_change);
                            //ApplyMoufa(sol.DeepCopy(m), m, sortedUnservedProfitables);

                        }

                    }
                }

            }
        }

        internal void ApplyMoufa(Solution sol, Model m, List<Node> sortedUnservedProfitables)
        {
            List<Route> routes = sol.routes;

            Node inserted = sortedUnservedProfitables[this.sortedPos];
            Route rt = routes[this.rt];
            Node removed = rt.nodes[this.removedPosition];

            rt.nodes[this.removedPosition] = inserted;

            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 + inserted.sTime - removed.sTime;
            rt.profit += inserted.profit - removed.profit;
            rt.load += inserted.demand - removed.demand;
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;
            List<Tuple<int, int>> deleted = new List<Tuple<int, int>>();
            deleted.Add(Tuple.Create(routes[this.rt].nodes[this.removedPosition - 1].ID, routes[this.rt].nodes[this.removedPosition].ID));
            deleted.Add(Tuple.Create(routes[this.rt].nodes[this.removedPosition].ID, routes[this.rt].nodes[this.removedPosition + 1].ID));
            //makePromises(deleted);

            Node inserted = sol.sortedUnservedProfitables[this.sortedPos];
            Route rt = routes[this.rt];
            Node removed = rt.nodes[this.removedPosition];

            rt.nodes[this.removedPosition] = inserted;
            sol.sortedUnservedProfitables.RemoveAt(this.sortedPos);

            //sorted insertion
            sol.sortedUnservedProfitables.Insert(0, removed);
            sol.sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));

            inserted.served = true;
            removed.served = false;

            double profit_before = rt.profit;

            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 + inserted.sTime - removed.sTime;
            rt.profit += inserted.profit - removed.profit;
            rt.load += inserted.demand - removed.demand;

            sol.updateRouteInformation(this.rt, this.rt);

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //    throw new Exception("Problema grande!!!");
            //}
        }
    }
}

