using System.Collections.Generic;
using System;

namespace DQN_Env
{
    public class Route
    {
        public List<Node> nodes;
        public double maxLoad;
        public double maxCost;
        public double load;
        public double cost; // the total distance of the route
        public double time; // the total distance of the route plus the service time of each customer
        public double profit;
        public double objective;  // M * profit - cost
        public List<double> demandTillHere;
        public List<double> timeTillHere_st_incl;
        //internal double objective;

        public Route()
        {
            this.nodes = new List<Node>();
            this.demandTillHere = new List<double>();
            this.timeTillHere_st_incl = new List<double>();
        }
        public Route(Model m)
        {
            this.maxLoad = m.maxLoad;
            this.maxCost = m.maxCost;
            this.nodes = new List<Node>();
            this.demandTillHere = new List<double>();
            this.timeTillHere_st_incl = new List<double>();
        }

        public Route(List<Node> nodes, Model m)
        {
            this.maxLoad = m.maxLoad;
            this.maxCost = m.maxCost;
            this.demandTillHere = new List<double>() { 0.0};
            this.timeTillHere_st_incl = new List<double>() { 0.0};
            this.nodes = nodes;
            for (int i = 1; i < nodes.Count; i++)
            {
                // update load
                this.load += nodes[i].demand;
                demandTillHere.Add(nodes[i].demand);
                //update cost
                this.cost += nodes[i - 1].cost[nodes[i].ID];
                // update time
                this.time += nodes[i - 1].cost[nodes[i].ID] + nodes[i].sTime;
                timeTillHere_st_incl.Add(nodes[i - 1].cost[nodes[i].ID] + nodes[i].sTime);
                // update profit
                this.profit += nodes[i].profit;
                this.objective = Model.M * profit - cost;
            }
        }

        override public string ToString()
        {
            string route = "[" + nodes[0].ID;
            foreach (Node node in nodes.GetRange(1, nodes.Count - 1))
            {
                //route += " -> " + node.ID;
                route += " " + node.ID;
            }
            route += "] profit: " + profit + " load: " + load + " time: " + time +  "\n";
            //route += "]\nload: " + load + "\ntime: " + time + "\nprofit: " + profit + "\n";

            return route;
        }


        public double HowSimilarTo(Route r2) // returns true if more than 80% of the nodes of the two routes are the same
        {
            HashSet<Node> this_nodes = new HashSet<Node>(this.nodes);
            HashSet<Node> r2_nodes = new HashSet<Node>(r2.nodes);
            HashSet<Node> set_intersection = new HashSet<Node>(this_nodes);

            set_intersection.IntersectWith(r2_nodes);  // perform the A Π B node operation
     
            double similarityPercentage =  ((double)set_intersection.Count / Math.Min(this_nodes.Count, r2_nodes.Count)) * 100;
            return similarityPercentage;
        }

        // check if there is any route in the pool similar to the candidate (more than 80% of the nodes are the same)
        // returns the index of the similar route with the lowest profit, -1 if no similar route is found
        public int CheckForSimilarityInPool()
        {
            double maxSimilarityPercentage = -1;
            int mostSimilarIndex = -1;
            IList<Route> poolRoutesList = new List<Route>();  // just to mute error messages
            //IList<Route> poolRoutesList = Pool.poolOfRoutes.Values;
            for (int i = 0; i < poolRoutesList.Count; i++)
            {
                Route routeToCheck = poolRoutesList[i];
                double simPercentage = this.HowSimilarTo(routeToCheck);
                if (simPercentage > maxSimilarityPercentage)
                {
                    mostSimilarIndex = i;
                    maxSimilarityPercentage = simPercentage;
                }
            }
            if (maxSimilarityPercentage > 80)
            {
                return mostSimilarIndex;
            }
            return -1;
        }
    }
}
