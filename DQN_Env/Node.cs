using System.Collections.Generic;
using System;

namespace DQN_Env
{
    public class Node
    {
        public static int count = 0;

        public int ID, IDP, IDR, IDdepot, tIter, elite, present;
        public int ID_math;  // a modified node id for datasets with removed nodes - only used for math functions
        public double x_coord, y_coord, averageCostToAllNodes, promise;
        public double take, send, record, label;
        public double demand, sTime, profit, costRemoval;
        public bool depot, served;
        internal List<double> cost;
        public int timesInBest, timesRemoved;
        public int timesPresent;
        public int timesInNewBest;

        public Node(double x_coord, double y_coord, double take, double sTime, double profit, bool isDepot)
        {
            this.x_coord = x_coord;
            this.y_coord = y_coord;
            this.take = take;
            this.demand = take;
            this.sTime = sTime;
            this.profit = profit;
            this.depot = isDepot;
            this.ID = count;
            count++;
        }

        public Node()
        {
        }

        public bool InSol(Solution sol)
        {
            foreach (Route rt in sol.routes)
            {
                if (rt.nodes.Contains(this))
                {
                    return true;
                }
            }
            return false;
        }

        override
        public string ToString()
        {
            return "ID:" + ID + " | (x,y):(" + x_coord + "," + y_coord + ") | profit " + profit + " | service time " + sTime + " | demand " + demand;
        }
    }


}
