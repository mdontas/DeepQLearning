using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DQN_Env
{
    public abstract class Move
    {
        public double moveObjective;
        public double objectiveChange;
        public int rt1, rt2;
        public double profitChangeRt1, profitChangeRt2;
        public double loadChangeRt1, loadChangeRt2;
        public double costChangeRt1, costChangeRt2;
        public static Route worst_feasible_route;
        public static double worst_feasible_route_profit;
        public static Route best_infeasible_route;
        public static double best_infeasible_route_profit;
        public Solution dummy_sol;

        public void initializeMove()
        {
            this.moveObjective = double.MinValue;
            objectiveChange = -1000;
        }

        public static void PrepareMoveData(Solution sol, Model m)
        {
            List<Route> sorted_routes = new List<Route>(sol.routes.OrderByDescending(x => x.profit));
            //for (int i = 0; i < m.vehicles - m.actual_vehicles - 1; i++)
            //{
            //    copied_routes.Remove(copied_routes.Min());
            //}
            worst_feasible_route = sorted_routes[m.actual_vehicles - 1];
            worst_feasible_route_profit = worst_feasible_route.profit;
            //best_infeasible_route = sorted_routes[m.actual_vehicles];
            //best_infeasible_route_profit = best_infeasible_route.profit;
            best_infeasible_route_profit = 0;
        }

        public void UpdateMoveData()
        {

        }

        public double GetTop2Sum(double a, double b, double c, double d)  // return the sum of the top 2 numbers
        {
            double bigger_in_first_pair = Math.Max(a, b);
            double bigger_in_second_pair = Math.Max(c, d);
            if (bigger_in_first_pair > bigger_in_second_pair)
            {
                return bigger_in_first_pair + Math.Max(a + b - bigger_in_first_pair, bigger_in_second_pair);
            }
            else
            {
                return bigger_in_second_pair + Math.Max(c + d - bigger_in_second_pair, bigger_in_first_pair);
            }
        }

        public float[] StateFeaturesAfterMove(Solution sol)
        {
            //Console.WriteLine(this);
            //Console.WriteLine(this.rt1 + " " + this.rt2 + " " + this.objectiveChange + " profit: " + this.profitChangeRt1 + " " + this.profitChangeRt2 + " | cost: " +
            //    this.costChangeRt1 + " " + this.costChangeRt2 + " | load: " + this.loadChangeRt1 + " " + this.loadChangeRt2);
            float[] features = new float[3 * sol.routes.Count + 1];
            int r = 0;
            foreach (Route route in sol.routes.OrderBy(x => x.cost))
            {
                features[r] = (float)route.profit;
                features[r + 1] = (float)route.load;
                features[r + 2] = (float)route.cost;
                r += 3;
            }
            features[3 * rt1] += (float)profitChangeRt1;
            features[3 * rt1 + 1] += (float)loadChangeRt1;
            features[3 * rt1 + 2] += (float)costChangeRt1;
            if (rt2 != -1)
            {
                features[3 * rt2] += (float)profitChangeRt2;
                features[3 * rt2 + 1] += (float)loadChangeRt2;
                features[3 * rt2 + 2] += (float)costChangeRt2;
            }
            features[features.Length - 1] = (float)objectiveChange;

            return features;
        }

        public abstract void FindBestMove(Solution sol, Model m=null);

        public abstract void ApplyMove(Solution sol, Model m);
    }
}
