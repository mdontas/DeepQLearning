# %%

from ctypes import *
import clr

from comtypes.automation import VARIANT
import numpy as np
import tensorflow as tf
import tensorflow.keras as keras
import tensorflow.keras.layers as layers
from tensorflow.keras import initializers

from matplotlib import pyplot as plt
import seaborn as sns
import pandas as pd

dll_location = 'dll\\net48\\DQN_Env.dll'
actor = cdll.LoadLibrary(dll_location)
# clr.AddReference("python_dlls\\Gurobi95.NET.dll")

# %%

# hyper-parameters
exploration_rate = 0.1
learning_rate = 0.01
gamma = 0.3
episodes = 10
episode_duration = 1000
replay_buffer_size = 10
w_update_freq = 5
k = 0.1

# specify arg types of each exported function
actor.initializeInstance.argtypes = [c_char_p, c_int]
actor.initializeEnv.argtypes = [c_int]
actor.getStateFeatures.argtypes = [POINTER(VARIANT)]
actor.execute.argtypes = [POINTER(VARIANT), POINTER(VARIANT)]
actor.execute.argtypes = [c_char_p, POINTER(VARIANT)]
actor.previewActions.argtypes = [POINTER(VARIANT)]

# %%

# build the neural network
def DQN(feature_dim, action_dim):
    input_shape = feature_dim
    vehicles = input_shape / 3
    output_shape = action_dim

    weight_mean = 0.01 * vehicles/(input_shape)
    initializer = initializers.RandomNormal(mean=0.005, stddev=0.001, seed=seed)

    inputs = layers.Input(shape=(input_shape,))
    hidden1 = layers.Dense(32, activation="relu",
        kernel_initializer=initializer)(inputs)
    hidden2 = layers.Dense(16, activation="relu",
        kernel_initializer=initializer)(hidden1)
    outputs = layers.Dense(output_shape, activation="linear",
        # output_shape, activation=keras.layers.LeakyReLU(alpha=0.01),
        kernel_initializer=initializer)(hidden2)

    return keras.Model(inputs=inputs, outputs=outputs)


def createExperiences(actor, target_dqn, state_dim, feature_dim, action_dim, ran):
    # apply an e-greedy criterion for selecting actions
    epsilon = ran.rand()
    if epsilon <= exploration_rate:
        actions = [ran.choice(action_dim) for _ in range(state_dim)]
    else: 
        # dummy execute actions on states to get immediate rewards and
        # next state features (all in a flattened array)
        preview_rewards = VARIANT()
        actor.previewActions(preview_rewards)
        preview_rewards = np.reshape(preview_rewards.value, (state_dim, action_dim, feature_dim + 1))
        imm_rewards = preview_rewards[:, :, -1]
        next_state_features = preview_rewards[:, :, :-1]

        # for all states, find the action with the biggest E|R| = r + maxQ*(s')
        all_features = tf.constant([y for x in next_state_features for y in x])
        max_q_per_action = np.reshape(np.max(target_dqn(all_features), axis=1), (state_dim, action_dim))
        q_star = imm_rewards + max_q_per_action
        actions = np.argmax(q_star, axis=1)
        # chosen_next_states = [next_state_features[a, actions[a]] for a in range(len(actions))]

    return actions


def compute_loss(sample, sample_size, dqn, target_dqn):
    s_features = tf.constant(np.reshape(np.concatenate(sample[:, 0]), (sample_size, feature_dim)))
    actions = sample[:, 1]
    rewards = sample[:, 2]
    next_s_features = tf.constant(np.reshape(np.concatenate(sample[:, 3]), (sample_size, feature_dim)))
    
    # compute q_values for current state and next state
    q_values = dqn(s_features)
    q_star_values = np.max(target_dqn(next_s_features), axis=1)

    # Compute target for batch -> R + γ * Q*(s')
    y = tf.tensor_scatter_nd_update(tf.constant(q_values),
                    indices=[[a, actions[a]] for a in range(len(actions))],
                    updates=rewards + gamma * q_star_values)

    # loss value -> [Qtarget - Q(s,a)]^2
    return q_values, q_star_values, tf.reduce_mean(tf.square(y - q_values), axis=0)


# %%

# problem parameters
dataset = c_char_p(b"Datasets\\LargeScale CTOP\\all_sets\\b9_505_6_150_200.txt")
state_dim_param = c_int(2)
seed_param = c_int(1)
state_dim = state_dim_param.value
seed = seed_param.value
action_dim = 7
feature_dim = 18
tf.random.set_seed(seed)
ran = np.random.RandomState(seed)

actor.initializeInstance(dataset, seed_param)
dqn = DQN(feature_dim, action_dim)  # trainable model
dqn.summary()
target_dqn = DQN(feature_dim, action_dim)  # non-trainable model used for loss evaluation
opt = tf.keras.optimizers.Adam(learning_rate=learning_rate)


# %%


actor.initializeEnv(state_dim_param)  # run constructive to get initial states
for ep in range(episodes):
    # initialize buffer
    replay_buffer = []
    # set states back to the initial at the beginning of each episode
    actor.envReset()  
    print('------------ Episode', ep, '--------------')
    # get initial states' features
    next_states = VARIANT()
    actor.getStateFeatures(next_states)
    next_states = tf.constant(next_states.value)
    for i in range(episode_duration):
        with tf.GradientTape() as tape:
            features = tf.constant(next_states)
            actions = createExperiences(actor, target_dqn, state_dim,
                                                feature_dim, action_dim, ran)

            # execute chosen actions and get rewards
            actions_param = c_char_p(bytes(str(list(actions))[1:-1], encoding='UTF-8'))
            rewards = VARIANT()
            actor.execute(actions_param, rewards)
            rewards = rewards.value

            next_states = VARIANT()
            actor.getStateFeatures(next_states)
            next_states = tf.constant(next_states.value)

            # add experiences to the replay buffer
            for s in range(state_dim):
                replay_buffer.append([features[s], actions[s], rewards[s], next_states[s]])

            if i % w_update_freq == 0:  # upgrade weights by calculating gradients on random samples of buffer
                sample_size = min(replay_buffer_size, len(replay_buffer))
                ran.shuffle(replay_buffer)
                mini_batch = np.array(replay_buffer[:sample_size])
                
                q_values, q_star_values, loss_value = compute_loss(mini_batch, sample_size, dqn, target_dqn)
                # loss_value = compute_loss(mini_batch, sample_size, dqn, target_dqn)

                # Compute gradients
                grads = tape.gradient(loss_value, dqn.trainable_variables)

                # Apply gradients to update network weights
                opt.apply_gradients(zip(grads, dqn.trainable_variables))
        
        # slowly transition the target network to the trained dqn
        trained_weights = k * np.array(dqn.get_weights())
        target_weights = (1-k) * np.array(target_dqn.get_weights())
        target_dqn.set_weights(np.add(trained_weights, target_weights))

        # print for each state its profit along with the action about to be executed
        # and also the q values following the action execution
        features_array = np.array(features)
        print(i, [(sum(features_array[j][::3]),
                        actions[j])for j in range(len(features_array))], dqn(features))

k = 1
# %%
