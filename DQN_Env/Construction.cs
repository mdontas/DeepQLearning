using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DQN_Env
{
    public class Construction_Move
    {
        public int ni = -1;
        public int ir = -1;
        public int ip = -1;
        public double objective;

        public Construction_Move(int ni, int ir, int ip, double objective)
        {
            this.ni = ni;
            this.ir = ir;
            this.ip = ip;
            this.objective = objective;
        }
    }

    public class Construction
    {
        public static int rcl_size = 3;

        public static Solution ConstructSol(Model m, Random ran)
        {
            Solution sol = new Solution(m);  // initialize solution object
            List<Route> routes = sol.routes;

            List<Node> unservedProfitableNodes = new List<Node>();

            List<Construction_Move> top_moves = new List<Construction_Move>();  // keep top |rcl_size| moves in each iteration


            foreach (Node n in m.nodes)
            {
                n.served = false;
                if (n.depot == false)
                    unservedProfitableNodes.Add(n);
            }

            bool feasibleInsertionExists;

            do
            {
                feasibleInsertionExists = false;
                int index = ran.Next(0, unservedProfitableNodes.Count);
                for (int i = 0; i < unservedProfitableNodes.Count; i++)
                {
                    index++;
                    if (index == unservedProfitableNodes.Count)
                    {
                        index = 0;
                    }
                    Node n = unservedProfitableNodes[index];
                    for (int j = 0; j < routes.Count; j++)
                    {
                        Route rt = routes[j];
                        if (isCompatible(n, rt))
                        {
                            for (int k = 0; k < rt.nodes.Count - 1; k++)
                            {
                                Node n1 = rt.nodes[k];
                                Node n2 = rt.nodes[k + 1];
                                if (insertionIsFeasible(n, rt, k) == true)
                                {
                                    feasibleInsertionExists = true;
                                    double costChange = n1.cost[n.ID] + n.cost[n2.ID] - n1.cost[n2.ID];
                                    double profitChange = n.profit;
                                    double insertionObjective = Model.M * profitChange - costChange;
                                    if (top_moves.Count < rcl_size)
                                    {
                                        top_moves.Add(new Construction_Move(index, j, k, insertionObjective));
                                        if (top_moves.Count == rcl_size)
                                        {
                                            //var x = top_moves.OrderBy(x => x.objective).ToList();
                                            top_moves = (List<Construction_Move>) top_moves.OrderBy(x => x.objective).ToList();
                                        }
                                    }
                                    else if (insertionObjective > top_moves[0].objective)
                                    {
                                        AddAndReplace(top_moves, new Construction_Move(index, j, k, insertionObjective));
                                    }
                                }
                            }
                        }
                    }
                }
                if (feasibleInsertionExists)
                {
                    Construction_Move selected_move = top_moves[ran.Next(top_moves.Count)];
                    Node n = unservedProfitableNodes[selected_move.ni];
                    n.served = true;
                    Route rt = routes[selected_move.ir];
                    rt.nodes.Insert(selected_move.ip + 1, n);
                    sol.updateRouteInformation(selected_move.ir, selected_move.ir);
                    unservedProfitableNodes.RemoveAt(selected_move.ni);
                }
                top_moves.Clear();
            }
            while (unservedProfitableNodes.Count > 0 && feasibleInsertionExists == true);

            sol.time = routes.Select(x => x.cost).Sum();
            sol.profit = routes.Select(x => x.profit).Sum();
            sol.sortedUnservedProfitables = unservedProfitableNodes;
            sol.sortedUnservedProfitables.Sort((a, b) => (-a.profit).CompareTo(-b.profit));
            //sol.Validate(m);
            return sol;
        }

        private static bool insertionIsFeasible(Node n, Route rt, int k)
        {
            if (!insertionCapacity(n, rt, k))
            {
                return false;
            }

            if (!insertionTotalTime(n, rt, k))
            {
                return false;
            }

            return true;
        }

        private static bool insertionTotalTime(Node n, Route rt, int k)
        {

            double costRemoved = rt.nodes[k].cost[rt.nodes[k + 1].ID];
            double costAdded = rt.nodes[k].cost[n.ID];
            costAdded += n.cost[rt.nodes[k + 1].ID];
            costAdded += n.sTime;

            if (rt.time + costAdded - costRemoved > rt.maxCost)
            {
                return false;
            }

            return true;
        }

        private static bool insertionCapacity(Node n, Route rt, int k)
        {
            if (rt.load + n.take > rt.maxLoad)
            {
                return false;
            }
            return true;
        }

        private static bool isCompatible(Node n, Route rt)
        {
            return true;
        }

        private static void AddAndReplace(List<Construction_Move> moves, Construction_Move move_to_add)
            // replace the move in place 0 and put the move to add in the correct place according to its objective
        {
            moves.RemoveAt(0);
            int place_at = 0;
            while (move_to_add.objective > moves[place_at].objective)
            {
                place_at++;
                if (place_at == moves.Count)
                    break;
            }
            moves.Insert(place_at, move_to_add);
        }
    }
}
