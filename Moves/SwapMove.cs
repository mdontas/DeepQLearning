using System;
using System.Collections.Generic;
using System.Text;

namespace DQN_Env
{
    public class SwapMove : Move
    {
        public int rt1, rt2, node1, node2;
        public double actual_obj_change;
        public bool moufa = false;

        public SwapMove()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double pc1, double pc2, double lc1, double lc2, double cc1, double cc2, int i, int j, int k, int l)
        {
            this.moveObjective = mObj;
            this.objectiveChange = pc1 + pc2;
            this.costChangeRt1 = cc1;
            this.costChangeRt2 = cc2;
            rt1 = i;
            node1 = j;
            rt2 = k;
            node2 = l;

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
            rt1 = i;
            node1 = j;
            rt2 = k;
            node2 = l;
            this.actual_obj_change = actual_obj_change;
        }

        public override void FindBestMove(Solution sol, Model m)
        {
            this.initializeMove();

            double actual_obj_change = 0;

            List<Route> routes = sol.routes;
            List<Tuple<int, int>> toBeCrt = new List<Tuple<int, int>>();

            for (int i = 0; i < routes.Count; i++)
            {
                Route rt1 = routes[i];
                for (int j = 1; j < rt1.nodes.Count - 1; j++)
                {
                    Node n1_pred = rt1.nodes[j - 1];
                    Node n1 = rt1.nodes[j];
                    Node n1_succ = rt1.nodes[j + 1];

                    for (int k = i; k < routes.Count; k++)
                    {
                        Route rt2 = routes[k];

                        int st = rt1 == rt2 ? j + 1 : 1;

                        for (int l = st; l < rt2.nodes.Count - 1; l++)
                        {
                            Node n2_pred = rt2.nodes[l - 1];
                            Node n2 = rt2.nodes[l];
                            Node n2_succ = rt2.nodes[l + 1];

                            if (rt1.load + n2.demand - n1.demand > m.maxLoad)
                                continue;
                            if (rt2.load + n1.demand - n2.demand > m.maxLoad)
                                continue;

                            // condition to avoid examining worse moves -- works for extra routes methodology
                            //if (Model.M * Math.Abs(n1.profit - n2.profit) < this.moveObjective || (k == i && this.moveObjective >= Model.M))
                            //{
                            //    continue;
                            //}

                            double costChange1, costChange2;

                            if (i == k && j == l - 1)
                            {
                                costChange1 = -n1_pred.cost[n1.ID] - n1.cost[n2.ID] - n2.cost[n2_succ.ID];
                                costChange1 += n1_pred.cost[n2.ID] + n2.cost[n1.ID] + n1.cost[n2_succ.ID];
                                costChange2 = 0;

                                toBeCrt.Clear();
                                toBeCrt.Add(Tuple.Create(n1_pred.ID, n2.ID));
                                toBeCrt.Add(Tuple.Create(n2.ID, n1.ID));
                                toBeCrt.Add(Tuple.Create(n1.ID, n2_succ.ID));
                            }
                            else
                            {
                                costChange1 = -n1_pred.cost[n1.ID] - n1.cost[n1_succ.ID];
                                costChange1 += n1_pred.cost[n2.ID] + n2.cost[n1_succ.ID];

                                costChange2 = -n2_pred.cost[n2.ID] - n2.cost[n2_succ.ID];
                                costChange2 += n2_pred.cost[n1.ID] + n1.cost[n2_succ.ID];

                                toBeCrt.Clear();
                                toBeCrt.Add(Tuple.Create(n1_pred.ID, n2.ID));
                                toBeCrt.Add(Tuple.Create(n2.ID, n1_succ.ID));
                                toBeCrt.Add(Tuple.Create(n2_pred.ID, n1.ID));
                                toBeCrt.Add(Tuple.Create(n1.ID, n2_succ.ID));

                                if (rt1 == rt2)
                                {
                                    costChange1 += costChange2;
                                    costChange2 = 0;
                                }
                            }

                            //copied_profits = new List<double>(this.profits);
                            //copied_profits[i] += n2.profit - n1.profit;
                            //copied_profits[k] += n1.profit - n2.profit;
                            //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;

                            if (rt1 == rt2)
                            {
                                if (rt1.time + costChange1 > m.maxCost)
                                    continue;
                                actual_obj_change = 0;
                            }
                            else
                            {
                                if (rt1.time + costChange1 - n1.sTime + n2.sTime > m.maxCost)
                                    continue;

                                if (rt2.time + costChange2 - n2.sTime + n1.sTime > m.maxCost)
                                    continue;
                                actual_obj_change = CalcActualObjChange(rt1.profit, rt2.profit, n1.profit - n2.profit);
                            }


                            double moveObjective = -(costChange1 + costChange2);
                            //double moveObjective = Model.M * actual_obj_change - (costChange1 + costChange2);

                            //if (!Promises.MoveKeepsPromisesRouting(toBeCrt, costChange1 + costChange2, sol.lengthCovered))
                            //{
                            //    continue;
                            //}

                            //if (!Promises.MoveKeepsPromisesActualObj(toBeCrt, actual_obj_change, sol.actual_profit))
                            //{
                            //    continue;
                            //}

                            if (Math.Abs(moveObjective) < 0.0001)
                            {
                                continue;
                            }

                            if (moveObjective > this.moveObjective)
                            {
                                double pc1 = n2.profit -n1.profit;
                                double pc2 = -pc1;
                                double lc1 = n2.demand - n1.demand;
                                double lc2 = -lc1;
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
            if (swapped_profit > 0)
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

                        return Math.Max(-swapped_profit, Math.Max(best_infeasible_route_profit, rt2_profit + swapped_profit) - rt1_profit);
                    }
                }
            }
            else
            {
                swapped_profit *= -1;
                if (rt2_profit <= best_infeasible_route_profit)
                {
                    if (rt1_profit >= worst_feasible_route_profit)
                    {
                        return swapped_profit;
                    }
                    else
                    {
                        return Math.Max(rt1_profit + swapped_profit - worst_feasible_route_profit, 0);
                    }
                }
                else
                {
                    if (rt1_profit >= worst_feasible_route_profit)
                    {
                        return Math.Max(best_infeasible_route_profit - (rt2_profit - swapped_profit), 0);
                    }
                    else
                    {
                        return GetTop2Sum(rt1_profit + swapped_profit, rt2_profit - swapped_profit, worst_feasible_route_profit, best_infeasible_route_profit) -
                            worst_feasible_route_profit - rt2_profit;

                        double new_top2_sum = rt1_profit + rt2_profit + worst_feasible_route_profit -
                            Math.Min(Math.Min(rt1_profit + swapped_profit, rt2_profit - swapped_profit), worst_feasible_route_profit);
                        return new_top2_sum - worst_feasible_route_profit - rt2_profit;

                        return Math.Max(-swapped_profit, Math.Max(best_infeasible_route_profit, rt1_profit + swapped_profit) - rt2_profit);
                    }
                }
            }
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;
            List<Tuple<int, int>> deleted = new List<Tuple<int, int>>();

            if (this.rt1 == this.rt2 && this.node1 == this.node2 - 1)
            {
                deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1 - 1].ID, routes[this.rt1].nodes[this.node1].ID));
                deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1].ID, routes[this.rt1].nodes[this.node1 + 1].ID));
                deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1 + 1].ID, routes[this.rt1].nodes[this.node1 + 2].ID));
            }
            else
            {
                deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1 - 1].ID, routes[this.rt1].nodes[this.node1].ID));
                deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1].ID, routes[this.rt1].nodes[this.node1 + 1].ID));
                deleted.Add(Tuple.Create(routes[this.rt2].nodes[this.node2 - 1].ID, routes[this.rt2].nodes[this.node2].ID));
                deleted.Add(Tuple.Create(routes[this.rt2].nodes[this.node2].ID, routes[this.rt2].nodes[this.node2 + 1].ID));
            }
            //Promises.MakePromises(deleted, sol.actual_profit);
            //Promises.MakePromises(deleted, sol.lengthCovered);


            Node a = routes[this.rt1].nodes[this.node1];
            Node b = routes[this.rt2].nodes[this.node2];
            routes[this.rt1].nodes[this.node1] = b;
            routes[this.rt2].nodes[this.node2] = a;
            //Promises.promises_nodes_rel[a.ID, rt1] = sol.actual_profit;
            //Promises.promises_nodes_rel[b.ID, rt2] = sol.actual_profit;

            if (this.rt1 != this.rt2)
            {
                routes[this.rt1].load += b.demand - a.demand;
                routes[this.rt2].load += a.demand - b.demand;
                routes[this.rt1].cost += this.costChangeRt1;
                routes[this.rt2].cost += this.costChangeRt2;
                routes[this.rt1].time += this.costChangeRt1 + b.sTime - a.sTime;
                routes[this.rt2].time += this.costChangeRt2 + a.sTime - b.sTime;
            }
            else
            {
                routes[this.rt1].cost += this.costChangeRt1;
                routes[this.rt1].time += this.costChangeRt1 + b.sTime - a.sTime;
            }

            routes[this.rt1].profit += b.profit - a.profit;
            routes[this.rt2].profit += a.profit - b.profit;
            if (!moufa)
            {
                sol.updateRouteInformation(this.rt1, this.rt2);
            }

            //Console.WriteLine("rt1: " + rt1 + " " + node1 + " a: " + a.ID);
            //Console.WriteLine("rt2: " + rt2 + " " + node2 + " b: " + b.ID);

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //    throw new Exception("Problema grande!!!");
            //}
        }
    }
}

