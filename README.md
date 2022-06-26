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
  6. [2opt](https://en.wikipedia.org/wiki/2-opt) move
  7. Execution of a [TSP heuristic](http://webhotel4.ruc.dk/~keld/research/LKH/) on every solution route

The way DQL pursues to increase the quality of the algorithm is by tackling the
problem of optimallly selecting among the available operators at each iteration.
In other words, instead of implementing a meta-heuristic responsible for balancing the operators, we attempt to train a model that will recognize the adequate course of actions
on each solution state with a view of eventually achieving the biggest profit possible.

### DQL Methodology

DQL is a value function method, meaning that it tries to estimate the expected future reward of an action executed on a certain state. It uses a neural network as a value function approximator, which is trained via the following loss function:

$$L_i(θ_i)= (y_i - Q(s_i,a_i;θ_i))^2$$ where $ y_i = r_i + γ*maxQ_{target}(s_{i+1};θ_t)$

Two important notes here:
* The target $y$ depicts the expected rewards of a (state, action) pair as the sum of the immediate reward of the action and the highest expected
value after following the optimal policy at every next state, where the latter is discounted by a fator of $γ$.
* The next state q-values are not obtained from the trainable network; another model, the target network (with the same architecture as the original) is used to compute the max q-value for the target $y$. The only difference with the original is that this network has more stable weights, which are updated from the original, though at a slower pace using the formula:
$$θ_t = kθ + (1-k)θ_t$$

### Learning Scheme

### Experience Replay

In order to improve the efficacy as well as the efficiency of the learning process, the idea of experience replay is implemented. According to it, the examples ran at each iteration are stored in a buffer, from where a sample is taken in order to calculate the gradients and update the weights. The replay buffer follows the **SARS** logic (state, action, rewards, next state). In a nutshell, the overall process has the following steps:
1. At the beginning of each episode, initialize the buffer and get the intitial states.
2. At every iteration generate a SARS sequence for every state implementing following **exploration strategy** and store the experiences in the buffer. Also update the target network weights by the aforementioned formula.
3. At the end of each episode, draw a random sample from the buffer and use it to train the model (***Note***: the buffer contains only the raw values of the examples, so q-values will be calculated again based on current networks' weights - which is the whole point anyway).

### Exploration Strategy

### Network Architecture

The network layers are as follows:
* **Input layer**:
  * Size: $(|V|, 10)$, where $V$ stands for the vehicles available (specified by instance data). For each vehicle we keep track of 10 features:
    * profit
    * profit per node
    * variance of profit per node
    * load
    * load per node
    * variance of load per node
    * cost
    * cost per node
    * variance of cost per node
    * nodes 
  * These features are scaled down using instance-specific constants that specify their limits, so that they obtain values at the [0, 1] scale (for instance, cost-related features are scaled down by the Tmax constraint which sets the max route cost).
* **1st hidden layer**:
  * 32 neurons
  * ReLu activation 
* **Flattened layer**:
  * $10|V|$ neurons
* **Dropout layer**:
  * 20% dropout
* **2nd hidden layer**:
  * $(10|V| + A) / 2$ neurons
  * ReLu activation
* **Output layer**:
  * $A$ neurons (as many as the actions)
  * Linear activation (rewards can be negative - when the action generates an infeasible solution, so we don't want to constrain the output)

<!-- Regarding the hidden layers, I'm considering the implementation of LeakyReLu activations instead of plain ReLu, since it will provide the flexibility of having negative outputs in those layers as well. The drawback with this idea is that it will possibly cause the outputs to deviate and thus slow down learning.

Another concern has to do with the scale difference between inputs and outputs. Inputs have a scale of $i * 10^2$, $i \epsilon (0,10)$, whereas outputs are in most cases around $i * 10^0$ (except for when the action is infeasible, in which case the reward is placed at -1000). I believe that lowering the scale of the inputs to match that of the outputs will accelerate learning, therefore I am about to implement one of the following alternatives:
1. Normalize state features before entering the network.
2. Implicilty lower their scale by applying batch normalization inside the network.
3. Initialize weights in such a way so that output values are in the required scale.

I am currently implementing the last idea, because I recon that the scale of the features has a meaning in the solution space and maybe I shouldn't distort it. I would appreciate your take on these ideas or others if I'm missing any. -->

### Network Update

As explained above, during each exploration step only one action is executed. This implies that, albeit the network has predicted the q-values for every state-action pair, the target value corresponds to a single pair at each step and therefore we are able to compute the loss only on the respective pair. However, we must also be consistent with the network architecture and assure that the target values have the same shape as the vectors of predicted values. Hence, we will transform the target value into a vector at the following way:

For an $A$ number of available actions the predicted q-values will be:
$$ y_{pred}=\begin{bmatrix} q_0 & q_1 & ... & q_{A-2} & q_{A-1} \end{bmatrix} $$

Let's consider $a$ to be the selected action on a certain step and $y$ the respective target reward. We construct the $A$-length vector of target values using the below function:

$$\begin{equation}
 q^{*}(i) =
  \begin{cases}
  e^{y} ,& i = a \\
  0 ,& otherwise
  \end{cases}
\end{equation}$$

This will result to a target vector as below:
$$ y_{target}=\begin{bmatrix} 0 & 0 & ... & e^{y} & ... & 0 & 0 \end{bmatrix} $$

Having formed the above vectors, we can compute the loss defined in \ref{loss} 
by applying a series of calculations:

$$\begin{equation}
L_i(\theta_i)= [\ln(y_{target} * e^{-y_{pred}})]^2
\end{equation}$$

This way, when we perform a matrix multiplication between $y_{target}$ and $e^{-y_{pred}}$, the resulting value will be $e^{y-q_a}$, since all other terms of the sum will be 0. From there, we apply the natural logarithm to remove the exponential and then by squaring the difference we are left with the calculation that we aimed for at the first place. The formulation delineated above facilitates the process of calculating the loss as well as the gradients of the network, since the whole procedure is being represented as a series of fuction applications and algebraic operations.

<!-- As explained above, the target $y$ is calculated on a specific state-action pair. This means that for every example in the buffer, we can compare and calculate the loss only on one action's q-value. However, in order to be consistent with the network architecture, we transform $y$ into a vector at the following way:

Let's consider an example which refers to action 3 with $y=10$. Predicted q-values are:
$$ pred=\begin{bmatrix} 0 & 3 & 6 & 4 & 0 & 1 & 2 \end{bmatrix} $$

Target q_values will be:
$$ y=\begin{bmatrix} 0 & 3 & 6 & \color{Green} 10 & 0 & 1 & 2 \end{bmatrix} $$

This way, when we apply the loss $(y - pred)^2$, the network will effectively calculate the difference between "real" and predicted values that corresponds to the calculation proposed in theory and update its weights accordingly.
In practice we are working with mutiple examples on each update, where all the losses are averaged to produce the batch loss. -->
