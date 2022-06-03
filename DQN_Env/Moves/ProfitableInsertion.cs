using System;
using System.Collections.Generic;
using System.Text;

namespace DQN_Env
{
    public class ProfitableInsertion : Move
    {
        public int rt, insPos, sortedPos;
        public double actual_obj_change;

        public ProfitableInsertion()
        {
            this.moveObjective = double.MinValue;
        }

        internal void StoreInformation(double mObj, double inserted_profit, double lc1, double cc1, int insertedPos, int rt, int insPos)
        {
            this.moveObjective = mObj;
            this.objectiveChange = inserted_profit;
            this.costChangeRt1 = cc1;
            sortedPos = insertedPos;
            this.rt = rt;
            this.insPos = insPos;

            rt1 = rt;
            rt2 = -1;
            profitChangeRt1 = inserted_profit;
            loadChangeRt1 = lc1;
        }

        internal void StoreInformation(double mObj, double cc1, int insertedPos, int rt, int insPos, double actual_obj_change)
        {
            this.moveObjective = mObj;
            this.costChangeRt1 = cc1;
            sortedPos = insertedPos;
            this.rt = rt;
            this.insPos = insPos;
            this.actual_obj_change = actual_obj_change;
        }

        public override void FindBestMove(Solution sol, Model m)
        {
            bool diversification = false;
            //if (ran.Next() > 0.8 && profitCollected < 0.97 * bestProfit)
            //    diversification = true;

            List<Route> routes = sol.routes;
            List<Tuple<int, int>> toBeCrt = new List<Tuple<int, int>>();

            this.initializeMove();
            
            double actual_obj_change = 0;

            for (int i = 0; i < sol.sortedUnservedProfitables.Count; i++)
            {
                Node inserted = sol.sortedUnservedProfitables[i];

                //if identified break

                if (Model.M * inserted.profit < this.moveObjective)
                {
                    return;
                }

                for (int k = 0; k < routes.Count; k++)
                {
                    Route rt = routes[k];
                    if (rt.load + inserted.demand > m.maxLoad)
                    {
                        continue;
                    }

                    //triangular inequality
                    if (rt.time + inserted.sTime > m.maxCost)
                    {
                        continue;
                    }

                    //break

                    //if (rt.profit >= worst_feasible_route_profit)
                    //{
                    //    actual_obj_change = inserted.profit;
                    //} else
                    //{
                    //    actual_obj_change = Math.Max(rt.profit + inserted.profit - worst_feasible_route_profit, 0);
                    //}

                    //copied_profits = new List<double>(this.profits);
                    //copied_profits[k] += inserted.profit;
                    //actual_obj_change = Actual_Measures.GetTopN(copied_profits, m.actual_vehicles) - sol.actual_profit;


                    for (int p = 0; p < rt.nodes.Count - 1; p++)
                    {

                        Node n1 = rt.nodes[p];
                        Node succ = rt.nodes[p + 1];

                        double costChange1 = n1.cost[inserted.ID] + inserted.cost[succ.ID] - n1.cost[succ.ID];

                        if (rt.time + costChange1 + inserted.sTime > m.maxCost)
                        {
                            continue;
                        }

                        double moveObjective = Model.M * inserted.profit - costChange1;
                        //double moveObjective = Model.M * inserted.profit + 0.1 * Model.M * actual_obj_change - costChange1;

                        toBeCrt.Clear();
                        toBeCrt.Add(Tuple.Create(n1.ID, inserted.ID));
                        toBeCrt.Add(Tuple.Create(inserted.ID, succ.ID));

                        //if (!Promises.MoveKeepsPromises(toBeCrt, inserted, inserted.profit, sol.profit))
                        //if (!Promises.MoveKeepsPromises(toBeCrt, inserted, actual_obj_change, sol.actual_profit))
                        //{
                        //    continue;
                        //}

                        if (moveObjective > this.moveObjective)
                        {
                            this.StoreInformation(moveObjective, inserted.profit, (-inserted.demand), costChange1, i, k, p);
                            //this.StoreInformation(moveObjective, costChange1, i, k, p, actual_obj_change);
                            //ApplyMoufa(sol.DeepCopy(m), m, sortedUnservedProfitables);
                        }
                    }
                }
            }
        }

        internal void ApplyMoufa(Solution sol, Model m, List<Node> sortedUnservedProfitables)
        {
            List<Route> routes = sol.routes;
            Node ins = sortedUnservedProfitables[sortedPos];
            Route rt = routes[this.rt];
            rt.nodes.Insert(this.insPos + 1, ins);
            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 + ins.sTime;
            rt.load += ins.demand;
            rt.profit += ins.profit;

        }

        public override void ApplyMove(Solution sol, Model m)
        {
            List<Route> routes = sol.routes;
            Node ins = sol.sortedUnservedProfitables[sortedPos];
            Route rt = routes[this.rt];
            ins.served = true;
            rt.nodes.Insert(this.insPos + 1, ins);
            sol.sortedUnservedProfitables.RemoveAt(this.sortedPos);
            rt.cost += this.costChangeRt1;
            rt.time += this.costChangeRt1 + ins.sTime;
            rt.load += ins.demand;
            sol.updateRouteInformation(this.rt, this.rt);

            //double actual_profit = Actual_Measures.Calc_Actual_Objective(sol, m.actual_vehicles);
            //if (actual_obj_change != actual_profit - sol.actual_profit)
            //{
            //     throw new Exception("Problema grande!!!");
            //}
        }
    }
}

