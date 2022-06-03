using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DQN_Env
{
    public class _2OptMove : Move
    {
        public int rt1, rt2, node1, node2;
        public double actual_obj_change;
        public bool moufa = false;
        public _2OptMove()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double pc1, double pc2, double lc1, double lc2, double cc1, double cc2, int i, int j, int k, int l)
        {
            this.moveObjective = mObj;
            this.objectiveChange = pc1 + pc2;
            profitChangeRt1 = pc1;
            profitChangeRt2 = pc2;
            loadChangeRt1 = lc1;
            loadChangeRt2 = lc2;
            this.costChangeRt1 = cc1;
            this.costChangeRt2 = cc2;
            rt1 = i;
            node1 = j;
            rt2 = k;
            node2 = l;
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

        public override void FindBestMove(Solution sol, Model m=null)
        {
            List<Route> routes = sol.routes;

            this.initializeMove();

            double actual_obj_change = 0;

            List<Tuple<int, int>> toBeCrt = new List<Tuple<int, int>>();

            for (int i = 0; i < routes.Count; i++)
            {
                Route rt1 = routes[i];
                for (int j = 0; j < rt1.nodes.Count - 1; j++)
                {
                    Node n1 = rt1.nodes[j];
                    Node n1_succ = rt1.nodes[j + 1];

                    for (int k = i; k < routes.Count; k++)
                    {
                        Route rt2 = routes[k];

                        int st = rt1 == rt2 ? j + 2 : 1;

                        for (int l = st; l < rt2.nodes.Count - 1; l++)
                        {
                            Node n2 = rt2.nodes[l];
                            Node n2_succ = rt2.nodes[l + 1];

                            // condition to avoid examining worse moves -- works for extra routes methodology
                            //if (Model.M * Math.Abs(comp1_profit - comp2_profit) < this.moveObjective || (k == i && this.moveObjective >= Model.M))
                            //{
                            //    continue;
                            //}

                            double costChange1, costChange2;
                            //within a route
                            if (i == k)
                            {
                                costChange1 = (n1.cost[n2.ID] + n1_succ.cost[n2_succ.ID]);
                                costChange1 -= (n1.cost[n1_succ.ID] + n2.cost[n2_succ.ID]);
                                costChange2 = 0;
                                toBeCrt.Clear();
                                toBeCrt.Add(Tuple.Create(n1.ID, n2.ID));
                                toBeCrt.Add(Tuple.Create(n1_succ.ID, n2_succ.ID));
                            }
                            else
                            //Inter route
                            {
                                costChange1 = -n1.cost[n1_succ.ID] + n1.cost[n2_succ.ID];
                                costChange2 = -n2.cost[n2_succ.ID] + n2.cost[n1_succ.ID];
                                toBeCrt.Clear();
                                toBeCrt.Add(Tuple.Create(n1.ID, n2_succ.ID));
                                toBeCrt.Add(Tuple.Create(n2.ID, n1_succ.ID));
                            }

                            double comp1_profit = rt1.nodes.GetRange(j + 1, rt1.nodes.Count - (j + 1)).Select(x => x.profit).Sum();
                            double comp2_profit = rt2.nodes.GetRange(l + 1, rt2.nodes.Count - (l + 1)).Select(x => x.profit).Sum();
                            double pc1 = comp2_profit - comp1_profit;
                            double pc2 = comp1_profit - comp2_profit;
                            //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;

                            //if (k == i)
                            //{
                            //    actual_obj_change = 0;
                            //} 
                            //else
                            //{
                            //    actual_obj_change = CalcActualObjChange(rt1.profit, rt2.profit, comp1_profit - comp2_profit);
                            //}

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

                            if (Math.Abs(moveObjective) < 0.001)
                            {
                                continue;
                            }

                            //Move Objective First Easier computations
                            if (moveObjective <= this.moveObjective)
                            {
                                continue;
                            }

                            double lc1 = (rt1.demandTillHere[j] + rt2.load - rt2.demandTillHere[l]) - rt1.load;
                            double lc2 = (rt2.demandTillHere[l] + rt1.load - rt1.demandTillHere[j]) - rt2.load;
                            //CAPACITY CONSTRAINTS
                            if (rt1.demandTillHere[j] + (rt2.load - rt2.demandTillHere[l]) > rt1.maxLoad)
                                continue;
                            if (rt2.demandTillHere[l] + (rt1.load - rt1.demandTillHere[j]) > rt1.maxLoad)
                                continue;

                            //COST CONSTRAINTS
                            if (i == k)
                            {
                                if (rt1.time + costChange1 > rt1.maxCost)
                                    continue;
                            }
                            else
                            {
                                if (rt1.timeTillHere_st_incl[j] + n1.cost[n2_succ.ID] + (rt2.time - rt2.timeTillHere_st_incl[l] - n2.cost[n2_succ.ID]) > rt1.maxCost)
                                    continue;
                                if (rt2.timeTillHere_st_incl[l] + n2.cost[n1_succ.ID] + (rt1.time - rt1.timeTillHere_st_incl[j] - n1.cost[n1_succ.ID]) > rt1.maxCost)
                                    continue;
                            }

                            this.StoreInformation(moveObjective, pc1, pc2, lc1, lc2, costChange1, costChange2, i, j, k, l);
                            //Console.WriteLine(i + " " + j + " " + k + " " + l + " " + costChangeRt1 + " " + costChangeRt2);
                            //this.StoreInformation(moveObjective, costChange1, costChange2, i, j, k, l, actual_obj_change);
                            //moufa = true;
                            //ApplyMove(sol.DeepCopy(m), m, sortedUnservedProfitables);
                            //moufa = false;
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
                        return Math.Max(-swapped_profit, Math.Max(best_infeasible_route_profit, rt1_profit + swapped_profit) - rt2_profit);
                    }
                }
            }
        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;
            List<Tuple<int, int>> deleted = new List<Tuple<int, int>>();
            deleted.Add(Tuple.Create(routes[this.rt1].nodes[this.node1].ID, routes[this.rt1].nodes[this.node1 + 1].ID));
            deleted.Add(Tuple.Create(routes[this.rt2].nodes[this.node2].ID, routes[this.rt2].nodes[this.node2 + 1].ID));
            //Promises.MakePromises(deleted, sol.lengthCovered);

            //Promises.promises_nodes_rel[routes[this.rt1].nodes[node1].ID, rt1] = sol.actual_profit;
            //Promises.promises_nodes_rel[routes[this.rt2].nodes[node2].ID, rt2] = sol.actual_profit;

            // delete this
            double comp1_profit = routes[this.rt1].nodes.GetRange(node1 + 1, routes[this.rt1].nodes.Count - (node1 + 1)).Select(x => x.profit).Sum();
            double comp2_profit = routes[this.rt2].nodes.GetRange(node2 + 1, routes[this.rt2].nodes.Count - (node2 + 1)).Select(x => x.profit).Sum();

            if (this.rt1 != this.rt2)
            {
                double service_times = 0, load = 0;

                Route n_rt1 = new Route(m);
                for (int i = 0; i <= this.node1; i++)
                {
                    n_rt1.nodes.Add(routes[this.rt1].nodes[i]);
                    service_times += routes[this.rt1].nodes[i].sTime;
                    load += routes[this.rt1].nodes[i].demand;
                }
                for (int i = this.node2 + 1; i < routes[this.rt2].nodes.Count; i++)
                {
                    n_rt1.nodes.Add(routes[this.rt2].nodes[i]);
                    service_times += routes[this.rt2].nodes[i].sTime;
                    load += routes[this.rt2].nodes[i].demand;
                }

                n_rt1.cost += routes[this.rt1].cost + costChangeRt1;
                n_rt1.time += routes[this.rt1].cost + costChangeRt1 + service_times;
                n_rt1.load = load;
                n_rt1.profit += routes[this.rt1].profit + comp2_profit - comp1_profit;

                service_times = 0;
                load = 0;

                Route n_rt2 = new Route(m);
                for (int i = 0; i <= this.node2; i++)
                {
                    n_rt2.nodes.Add(routes[this.rt2].nodes[i]);
                    service_times += routes[this.rt2].nodes[i].sTime;
                    load += routes[this.rt2].nodes[i].demand;
                }
                for (int i = this.node1 + 1; i < routes[this.rt1].nodes.Count; i++)
                {
                    n_rt2.nodes.Add(routes[this.rt1].nodes[i]);
                    service_times += routes[this.rt1].nodes[i].sTime;
                    load += routes[this.rt1].nodes[i].demand;
                }

                n_rt2.cost += routes[this.rt2].cost + costChangeRt2;
                n_rt2.time += routes[this.rt2].cost + costChangeRt2 + service_times;
                n_rt2.load = load;
                n_rt2.profit += routes[this.rt2].profit + comp1_profit - comp2_profit;

                routes[this.rt1] = n_rt1;
                routes[this.rt2] = n_rt2;
            }
            else
            {
                Route n_rt1 = new Route(m);
                for (int i = 0; i <= this.node1; i++)
                    n_rt1.nodes.Add(routes[this.rt1].nodes[i]);
                for (int i = this.node2; i > this.node1; i--)
                    n_rt1.nodes.Add(routes[this.rt1].nodes[i]);
                for (int i = this.node2 + 1; i < routes[this.rt1].nodes.Count; i++)
                    n_rt1.nodes.Add(routes[this.rt1].nodes[i]);

                n_rt1.cost = routes[this.rt1].cost;
                n_rt1.time = routes[this.rt1].time;
                n_rt1.load = routes[this.rt1].load;
                n_rt1.profit += routes[this.rt1].profit;

                routes[this.rt1] = n_rt1;
            }

            //routes[this.rt1].profit += comp1_profit - comp2_profit;
            //routes[this.rt2].profit += comp2_profit - comp1_profit;
            if (!moufa)
            {
                sol.updateRouteInformation(this.rt1, this.rt2);
            }

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //    throw new Exception("Problema grande!!!");
            //}
        }
    }
}
