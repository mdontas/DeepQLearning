using System;
using System.Collections.Generic;
using System.Text;

namespace DQN_Env
{
    public class RelocationMove : Move
    {
        public int rtFrom, rtTo, nodeFrom, nodeTo;
        public double actual_obj_change;
        public bool moufa = false;
        public RelocationMove()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double pc1, double pc2, double lc1, double lc2, double cc1, double cc2, int i, int j, int k, int l)
        {
            this.moveObjective = mObj;
            this.objectiveChange = pc1 + pc2;
            this.costChangeRt1 = cc1;
            this.costChangeRt2 = cc2;
            rtFrom = i;
            nodeFrom = j;
            rtTo = k;
            nodeTo = l;

            rt1 = rtFrom;
            rt2 = rtTo;
            profitChangeRt1 = pc1;
            profitChangeRt2 = pc2;
            loadChangeRt1 = lc1;
            loadChangeRt2 = lc2;
        }

        internal void StoreInformation(double mObj, double cc1, double cc2, int i, int j, int k, int l, double actual_obj_change)
        {
            this.moveObjective = mObj;
            this.costChangeRt1 = cc1;
            this.costChangeRt2 = cc2;
            rtFrom = i;
            nodeFrom = j;
            rtTo = k;
            nodeTo = l;
            this.actual_obj_change = actual_obj_change;
        }

        public override void FindBestMove(Solution sol, Model m)
        {
            this.initializeMove();

            double actual_obj_change = 0, rt1_profit, rt2_profit;

            List<Route> routes = sol.routes;
            List<Tuple<int, int>> toBeCrt = new List<Tuple<int, int>>();

            for (int i = 0; i < routes.Count; i++)
            {
                Route rt1 = routes[i];
                rt1_profit = rt1.profit;
                for (int j = 1; j < rt1.nodes.Count - 1; j++)
                {
                    Node n1_pred = rt1.nodes[j-1];
                    Node n1 = rt1.nodes[j];
                    Node n1_succ = rt1.nodes[j+1];

                    for (int k = 0; k < routes.Count; k++)
                    {
                        Route rt2 = routes[k];
                        rt2_profit = rt2.profit;
                        if (rt2.load + n1.demand > m.maxLoad)
                        {
                            continue;
                        }

                        // condition to avoid examining worse moves -- works for extra routes methodology
                        //if (Model.M * n1.profit < this.moveObjective)
                        //{
                        //    continue;
                        //}

                        for (int l = 0; l < rt2.nodes.Count - 1; l++)
                        {
                            Node bef = rt2.nodes[l];
                            Node bef_succ = rt2.nodes[l + 1];

                            if (i == k && (j == l || j == l + 1))
                            {
                                continue;
                            }

                            double costChange1 = n1_pred.cost[n1_succ.ID] - n1_pred.cost[n1.ID] - n1.cost[n1_succ.ID];
                            double costChange2 = bef.cost[n1.ID] + n1.cost[bef_succ.ID] - bef.cost[bef_succ.ID];

                            //copied_profits = new List<double>(this.profits);
                            //copied_profits[i] -= n1.profit;
                            //copied_profits[k] += n1.profit;
                            //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;

                            if (rt1 == rt2)
                            {
                                costChange1 = costChange1 + costChange2;
                                costChange2 = 0;

                                if (rt1.time + costChange1 > m.maxCost)
                                {
                                    continue;
                                }
                            }

                            if (rt2.time + costChange2 + n1.sTime > m.maxCost)
                            {
                                continue;
                            }

                            //if (k == i)
                            //{
                            //    actual_obj_change = 0;
                            //}
                            //else
                            //{
                            //    actual_obj_change = CalcActualObjChange(rt1.profit, rt2.profit, n1.profit);
                            //}

                            double moveObjective = -(costChange1 + costChange2);
                            //double moveObjective = Model.M * actual_obj_change - (costChange1 + costChange2);

                            if (Math.Abs(moveObjective) < 0.0001)
                            {
                                continue;
                            }

                            toBeCrt.Clear();
                            toBeCrt.Add(Tuple.Create(n1_pred.ID, n1_succ.ID));
                            toBeCrt.Add(Tuple.Create(bef.ID, n1.ID));
                            toBeCrt.Add(Tuple.Create(n1.ID, bef_succ.ID));

                            //if (!Promises.MoveKeepsPromisesRouting(toBeCrt, costChange1 + costChange2, sol.lengthCovered))
                            //{
                            //    continue;
                            //}

                            //if (!Promises.MoveKeepsPromisesActualObj(toBeCrt, actual_obj_change, sol.actual_profit))
                            //{
                            //    continue;
                            //}

                            if (moveObjective > this.moveObjective)
                            {
                                double pc1 = -n1.profit;
                                double pc2 = n1.profit;
                                double lc1 = -n1.demand;
                                double lc2 = n1.demand;
                                this.StoreInformation(moveObjective, pc1, pc2, lc1, lc2, costChange1, costChange2, i, j, k, l);
                                //this.StoreInformation(moveObjective, costChange1, costChange2, i, j, k, l, actual_obj_change);
                                //moufa = true;
                                //ApplyMove(sol.DeepCopy(m), m, sortedUnservedProfitables);
                                //moufa = false;
                            }
                        }
                    }
                }
            }
        }

        internal double CalcActualObjChange(double rt1_profit, double rt2_profit, double swapped_profit)
        {
            if (rt1_profit <= best_infeasible_route_profit)
            {
                if (rt2_profit >= worst_feasible_route_profit)
                {
                    return swapped_profit;
                }
                else
                {
                    return Math.Max(rt2_profit + swapped_profit - worst_feasible_route_profit, 0);
                }
            }
            else
            {
                if (rt2_profit >= worst_feasible_route_profit)
                {
                    return Math.Max(best_infeasible_route_profit - (rt1_profit - swapped_profit), 0);
                }
                else
                {

                    return GetTop2Sum(rt1_profit - swapped_profit, rt2_profit + swapped_profit, worst_feasible_route_profit, best_infeasible_route_profit) - 
                        worst_feasible_route_profit - rt1_profit;
                    
                    double new_top2_sum = rt1_profit + rt2_profit + worst_feasible_route_profit -
                        Math.Min(Math.Min(rt1_profit - swapped_profit, rt2_profit + swapped_profit), worst_feasible_route_profit);
                    return new_top2_sum - worst_feasible_route_profit - rt1_profit; 
                    
                        
                   //return Math.Max(-swapped_profit, worst_feasible_route_profit - rt1_profit) +
                        //Math.Max(rt2_profit + swapped_profit - worst_feasible_route_profit, 0);

                    //return Math.Max(-swapped_profit, Math.Max(best_infeasible_route_profit, rt2_profit + swapped_profit) - rt1_profit);
                }
            }
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;
            List<Tuple<int, int>> deleted = new List<Tuple<int, int>>();
            deleted.Add(Tuple.Create(routes[this.rtFrom].nodes[this.nodeFrom - 1].ID, routes[this.rtFrom].nodes[this.nodeFrom].ID));
            deleted.Add(Tuple.Create(routes[this.rtFrom].nodes[this.nodeFrom].ID, routes[this.rtFrom].nodes[this.nodeFrom + 1].ID));
            deleted.Add(Tuple.Create(routes[this.rtTo].nodes[this.nodeTo].ID, routes[this.rtTo].nodes[this.nodeTo + 1].ID));
            //Promises.MakePromises(deleted, sol.lengthCovered);


            Node relocated = routes[this.rtFrom].nodes[this.nodeFrom];
            //Promises.promises_nodes_rel[relocated.ID, this.rtFrom] = sol.actual_profit;

            routes[this.rtFrom].nodes.RemoveAt(this.nodeFrom);

            if (this.rtFrom == this.rtTo && this.nodeTo > this.nodeFrom)
                routes[this.rtTo].nodes.Insert(this.nodeTo, relocated);
            else
                routes[this.rtTo].nodes.Insert(this.nodeTo + 1, relocated);

            if (this.rtFrom != this.rtTo)
            {
                routes[this.rtFrom].cost = routes[this.rtFrom].cost + this.costChangeRt1;
                routes[this.rtFrom].time = routes[this.rtFrom].cost + this.costChangeRt1 - relocated.sTime;
                routes[this.rtFrom].load = routes[this.rtFrom].load - relocated.demand;

                routes[this.rtTo].cost = routes[this.rtTo].cost + this.costChangeRt2;
                routes[this.rtTo].time = routes[this.rtTo].cost + this.costChangeRt2 + relocated.sTime;
                routes[this.rtTo].load = routes[this.rtTo].load + relocated.demand;
            }
            else
            {
                routes[this.rtFrom].cost = routes[this.rtFrom].cost + this.costChangeRt1;
                routes[this.rtFrom].time = routes[this.rtFrom].cost + this.costChangeRt1;
            }

            routes[this.rtFrom].profit -= relocated.profit;
            routes[this.rtTo].profit += relocated.profit;
            if (!moufa)
            {
                sol.updateRouteInformation(this.rtFrom, this.rtTo);
            }

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //    throw new Exception("Problema grande!!!");
            //}
        }
    }
}

