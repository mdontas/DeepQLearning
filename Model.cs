using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DQN_Env
{
    public class Model
    {
        public static readonly double M = Math.Pow(10, 8);

        public string dataset_name;

        public int liter_best;

        public int node_crowd;

        public List<Node> nodes;
        public List<Node> customers; // all nodes except depot

        public int actual_vehicles;
        public int vehicles;
        public double maxLoad;
        public double maxCost;

        public Node depot;


        public double[,] cost_matrix;  // this matrix indicates the cost of moving from a node i to a node j, including service time of j

        public Model()
        {

        }

        public Model(string dataset_name, int availableVehicles, double maxLoad, double maxCost, List<Node> nodes, int liter_best)
        {
            this.dataset_name = dataset_name;
            this.vehicles = availableVehicles;
            this.actual_vehicles = availableVehicles;
            this.maxLoad = maxLoad;
            this.maxCost = maxCost;
            this.nodes = nodes;
            this.liter_best = liter_best;
            this.node_crowd = nodes.Count;
            this.depot = nodes[0];
            this.customers = nodes.GetRange(1, nodes.Count - 1);

            //Pool.poolOfRoutesSize = vehicles * 10;

            GenerateCostDetails();
            //GenerateCostMatrix();
        }

        private void GenerateCostDetails()
        {
            double accCost = 0;
            customers.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                Node c = nodes[i];
                if (c.demand > 0)
                {
                    customers.Add(c);
                }
                c.cost = new List<double>();

                for (int j = 0; j < nodes.Count; j++)
                {
                    Node other = nodes[j];
                    double distFrom_c_to_other = Math.Pow((c.x_coord - other.x_coord), 2) + Math.Pow((c.y_coord - other.y_coord), 2);
                    distFrom_c_to_other = Math.Sqrt(distFrom_c_to_other);
                    c.averageCostToAllNodes += distFrom_c_to_other;
                    c.cost.Add(distFrom_c_to_other);
                    accCost += distFrom_c_to_other;
                }
                c.averageCostToAllNodes = 0.5 * c.averageCostToAllNodes / (double) nodes.Count;
            }
            //threshold = (accCost / (double)(nodes.Count * nodes.Count));
        }

        private void GenerateCostMatrix()
        {
            cost_matrix = new double[node_crowd, node_crowd];
            double service_time;
            for (int i = 0; i < nodes.Count; i++)
            {
                Node n_i = nodes[i];
                for (int j = 0; j < nodes.Count; j++)
                {
                    Node n_j = nodes[j];
                    service_time = i != j ? n_j.sTime : 0;
                    double distFrom_c_to_other = Math.Pow((n_i.x_coord - n_j.x_coord), 2) + Math.Pow((n_i.y_coord - n_j.y_coord), 2);
                    distFrom_c_to_other = Math.Sqrt(distFrom_c_to_other);
                    cost_matrix[i, j] = distFrom_c_to_other + service_time;
                }
            }
        }

        public Model DeleteSolNodesFromDataset(Solution sol)  // generates a new model object with all nodes present in the solution removed
        {
            // create a list containing all nodes present in the solution object
            List<Node> nodes_to_del = sol.routes[0].nodes.GetRange(1, sol.routes[0].nodes.Count - 2);
            for (int i = 1; i < sol.routes.Count; i++)
            {
                foreach (Node n in sol.routes[i].nodes.GetRange(1, sol.routes[i].nodes.Count - 2))
                nodes_to_del.Add(n);
            }

            // create a new Model object where the above nodes will be removed
            Model m = new Model();
            m.dataset_name = this.dataset_name;
            m.vehicles = this.vehicles;
            m.maxLoad = this.maxLoad;
            m.maxCost = this.maxCost;
            m.nodes = this.nodes.FindAll(n => !nodes_to_del.Contains(n));
            m.node_crowd = m.nodes.Count;
            m.depot = nodes[0];
            m.customers = nodes.GetRange(1, nodes.Count - 1);

            return m;
        }

        override
        public string ToString()
        {
            string model = "Dataset: " + dataset_name + "\nNodes: " + node_crowd + "\nVehicles: " + vehicles +
                "\nMax Load per vehicle: " + maxLoad + "\nMax Cost per vehicle: " + maxCost + "\n"
                + "\nBest known objective: " + liter_best + "\n";

            //foreach (Node node in nodes)
            //{
            //    model += node.ToString() + "\n";
            //}

            return model;
        }

        public void PrintCostMatrix()
        {
            for (int i = 0; i < node_crowd; i++)
            {
                for (int j = 0; j < node_crowd; j++)
                {
                    Console.Write(cost_matrix[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}

