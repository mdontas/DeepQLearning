using System;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace DQN_Env
{
    public class DQN_Env
    {
        static Random ran;
        static Model m;
        static List<Move> actions;
        static List<Solution> initial_states;
        static List<Solution> batch_states;

        [DllExport("initializeInstance", CallingConvention = CallingConvention.Cdecl)]
        public static void InitializeInstance(string dataset, int seed)
        {
            ran = new Random(seed);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            m = Reader.ReadProblemFile(dataset);

            actions = new List<Move> { new ProfitableInsertion(), new ProfitableDeletion(), new ProfitableSwap(),
            new SwapMove(), new RelocationMove(), new _2OptMove(), new TspMove()};
        }

        [DllExport("initializeEnv", CallingConvention = CallingConvention.Cdecl)]
        public static void InitializeEnv(int batch_size)
        {
            // create initial states to where states will be reset after an episode has ended
            initial_states = new List<Solution>();
            for (int i = 0; i < batch_size; i++)
            {
                initial_states.Add(Construction.ConstructSol(m, ran));
            }

            // insert to states their initial values
            batch_states = new List<Solution>();
            //foreach (Solution sol in initial_states)
            //{
            //    batch_states.Add(sol.DeepCopy(m));
            //}
            ////ResetEnv();  // TRY TO CALL THIS USING DLL CALL
        }

        //[DllImport("Actor.dll", EntryPoint = "envReset", CallingConvention = CallingConvention.Cdecl)]
        //public static extern void ResetEnv();

        [DllExport("envReset", CallingConvention = CallingConvention.Cdecl)]
        public static void Env_Reset()  // return to the initial batch state
        {
            batch_states.Clear();
            foreach (Solution sol in initial_states)
            {
                batch_states.Add(sol.DeepCopy(m));
            }
        }

        [DllExport("getStateFeatures", CallingConvention = CallingConvention.Cdecl)]
        public static void Get_State_Features(ref object features_ref)
        {
            float[,] features = new float[batch_states.Count, 3 * batch_states[0].routes.Count];
            Solution state;
            for (int i = 0; i < batch_states.Count; i++)
            {
                state = batch_states[i];
                int r = 0;
                foreach (Route route in state.routes.OrderBy(x => x.cost))
                {
                    features[i, r] = (float)route.profit;
                    features[i, r+1] = (float)route.load;
                    features[i, r+2] = (float)route.cost;
                    r += 3;
                }
            }
            features_ref = features;
        }

        [DllExport("previewActions", CallingConvention = CallingConvention.Cdecl)]
        public static void Preview_Actions(ref object rewards_ref)
        {
            float[] rewards = new float[batch_states.Count * actions.Count * (3 * batch_states[0].routes.Count + 1)];  // table size: |states| X |actions| X (|features| + 1)
            try
            {
                float[] action_rewards = new float[3 * batch_states[0].routes.Count + 1];
                Solution state;
                Move action;
                int i = 0;
                for (int s = 0; s < batch_states.Count; s++)
                {
                    state = batch_states[s];
                    for (int j = 0; j < actions.Count; j++)
                    {
                        action = actions[j];
                        action.FindBestMove(state, m);
                        if (action.GetType() == typeof(InsDelMove) || action.GetType() == typeof(TspMove))
                        {
                            action_rewards = action.StateFeaturesAfterMove(action.dummy_sol);
                        }
                        else
                        {
                            action_rewards = action.StateFeaturesAfterMove(state);
                        }
                        foreach (var n in action_rewards)
                        {
                            rewards[i] = (float)n;
                            i += 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            rewards_ref = rewards;
        }

        [DllExport("execute", CallingConvention = CallingConvention.Cdecl)]
        public static void Execute(string batch_actions_as_str, ref object rewards_ref)
        // execute action on state and get reward and next state
        {
            //Move math = actions[actions.Count - 1];
            //float[] rewards = new float[batch_states.Count];
            //int[] batch_actions = batch_actions_as_str.Split(',').Select(x => Int32.Parse(x.Trim())).ToArray<int>();
            //for (int i = 0; i < batch_actions.Length; i++)
            //{
            //    math.FindBestMove(batch_states[i]);
            //    math.ApplyMove(batch_states[i], m);
            //    rewards[i] = (float)(math.objectiveChange * batch_states[i].profit - math.costChangeRt1 - math.costChangeRt2);
            //}

            int[] batch_actions = batch_actions_as_str.Split(',').Select(x => Int32.Parse(x.Trim())).ToArray<int>();
            float[] rewards = new float[batch_states.Count];
            Move action;
            Solution state;
            int i = 0;
            foreach (int a in (int[])batch_actions)
            {
                // get selected action and find best move
                action = actions[a];
                state = batch_states[i];
                action.FindBestMove(state, m);

                double reward = action.objectiveChange;  // reward when move is infeasible
                //double reward = 0;
                if (action.moveObjective > double.MinValue)
                {
                    action.ApplyMove(state, m);
                    reward = action.objectiveChange;
                    //reward = action.objectiveChange * state.profit - action.costChangeRt1 - action.costChangeRt2;
                    //reward = state.profit;
                }

                rewards[i] = (float)reward;
                i += 1;
            }
            rewards_ref = rewards;
        }

        [DllExport("previewActionsTest", CallingConvention = CallingConvention.Cdecl)]
        public static float[] Preview_ActionsTest()
        {
            float[] rewards = new float[batch_states.Count * actions.Count * (3 * batch_states[0].routes.Count + 1)];  // table size: |states| X |actions| X (|features| + 1)
            try
            {
                float[] action_rewards = new float[3 * batch_states[0].routes.Count + 1];
                Solution state;
                Move action;
                int i = 0;
                for (int s = 0; s < batch_states.Count; s++)
                {
                    state = batch_states[s];
                    for (int j = 0; j < actions.Count; j++)
                    {
                        action = actions[j];
                        action.FindBestMove(state, m);
                        if (action.GetType() == typeof(InsDelMove) || action.GetType() == typeof(TspMove))
                        {
                            action_rewards = action.StateFeaturesAfterMove(action.dummy_sol);
                        }
                        else
                        {
                            action_rewards = action.StateFeaturesAfterMove(state);
                        }
                        foreach (var n in action_rewards)
                        {
                            rewards[i] = (float)n;
                            i += 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return rewards;
        }

        [DllExport("executeTest", CallingConvention = CallingConvention.Cdecl)]
        public static float[] Execute_Test(int[] batch_actions)
        // execute action on state and get reward and next state
        {
            //Move math = actions[actions.Count - 1];
            //float[] rewards = new float[batch_states.Count];
            //for (int i = 0; i < batch_actions.Length; i++)
            //{
            //    math.FindBestMove(batch_states[i]);
            //    math.ApplyMove(batch_states[i], m);
            //    rewards[i] = (float)(math.objectiveChange * batch_states[i].profit - math.costChangeRt1 - math.costChangeRt2);
            //}

            float[] rewards = new float[batch_states.Count];
            Move action;
            Solution state;
            int i = 0;
            foreach (int a in (int[])batch_actions)
            {
                // get selected action and find best move
                action = actions[a];
                state = batch_states[i];
                action.FindBestMove(state, m);

                double reward = float.MinValue;
                if (action.moveObjective > double.MinValue)
                {
                    action.ApplyMove(state, m);
                    reward = action.objectiveChange * state.profit - action.costChangeRt1 - action.costChangeRt2;
                    //Console.WriteLine(reward + " | " + action.objectiveChange + " " + state.profit + " " + action.costChangeRt1 + " " + action.costChangeRt2);
                }

                rewards[i] = (float)reward;
                i += 1;

                //Console.WriteLine(batch_states[i - 1].Validate(m));
            }
            return rewards;
        }
    }
}
