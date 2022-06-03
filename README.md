# Deep Q Learning algorithm for the Capacitated Team Orienteering Problem
### Overview

This project applies the Deep Q Learning methodology (greatly influenced by this [paper](https://www.cs.toronto.edu/%7Evmnih/docs/dqn.pdf)) in order to enhance the performance
of an algorithm used to solve the [CTOP](https://link.springer.com/article/10.1057/palgrave.jors.2602603). 

The said algorithm consists of the following main components:
* A construction heuristic used to generate a feasible solution to the problem
* A handful of solution improvement heuristics called **operators** which manage to either increment a given solution's profit or reduce its total cost. These include:
  1. Insertion of a node to the solution
  2. Deletion of a node from the solution
  3. Swap between an inner node (part of the current solution) and an outer
  4. Swap between two inner nodes
  5. Relocation of an inner node to another position in the solution
  6. 2opt move
  7. Execution of [a TSP heuristic](http://webhotel4.ruc.dk/~keld/research/LKH/) on every solution route

The way DQL pursues to increase the quality of the algorithm is by tackling the
problem of optimallly selecting among the available operators at each iteration.
In other words, instead of implementing a meta-heuristic responsible for balancing the operators, we attempt to train a model that will recognize the adequate course of actions
on each solution state with a view of eventually achieving the biggest profit possible.

### DQL Methodology

DQL is a value function method, meaning that it tries to estimate the expected future reward of an action executed on a certain state. It uses a neural network as a value function approximator, which is trained via the following loss function:

$$L_i(θ_i)= (y_i - Q(s_i,a_i;θ_i))^2$$ where $ y_i = r + γ*maxQ_{target}(s_{i+1};θ_t)$

Two important notes here:
* The target $y$ depicts the expected rewards of a (state, action) pair as the sum of the immediate reward of the action and the highest expected
value after following any action at the next state, where the latter is discounted by a fator of $γ$.
* The next state q values are not obtained from the trainable network; another network, the target network (with the same architecture as the original) is used to compute the max q value for the target $y$. The only difference with the original is that this network has more stable weights, which are updated from the original, though at a slower pace using the formula:
$$θ_t = kθ + (1-k)θ_t$$



