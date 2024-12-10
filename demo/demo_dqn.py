# basic imports
import sys
from typing import Any
# frameworks
import numpy as np
import pyqtgraph as pg
import torch
from torch import nn
from torch.nn import functional as F
# CoBeL-RL
from cobel3.agent import DQN
from cobel3.policy import EpsilonGreedy
from cobel3.typing import CallbackDict
from cobel3.network import FlexibleTorchNetwork
from cobel3.monitor import EscapeLatencyMonitor
from cobel3.interface import Topology
from cobel3.misc.topology_tools import linear_track, grid, hexagonal, t_maze
from cobel3.interface.topology import Node, NodeID
from unity import UnitySimulator



class Model(torch.nn.Module):

    def __init__(self, input_size: int | tuple[int, ...], output_size: int):
        super().__init__()
        input_units = input_size if isinstance(input_size, int) else int(np.prod(input_size))
        self.layer_dense_1 = nn.Linear(in_features=input_units, out_features=64)
        self.layer_dense_2 = nn.Linear(in_features=64, out_features=64)
        self.layer_output = nn.Linear(in_features=64, out_features=output_size)
        self.double()

    def forward(self, layer_input: torch.Tensor) -> torch.Tensor:
        x = torch.reshape(layer_input, (len(layer_input), -1))
        x = x.double() / 255.
        x = self.layer_dense_1(x)
        x = F.relu(x)
        x = self.layer_dense_2(x)
        x = F.relu(x)
        x = self.layer_output(x)

        return x


def simulation_run(topology_nodes: dict[NodeID, Node], staring_nodes: list[NodeID], trials_train: int = 300, trials_test: int = 200, steps: int = 100) -> None:
    '''
    This function represents one simulation run with the Topology interface.

    Parameters
    ----------
    nodes :                             The topology nodes.
    starting_nodes :                    The starting nodes.
    trials_train :                      The number of training trials.
    trials_test :                       The number of test trials.
    steps :                             The maximum number of steps per trial.

    Returns
    ----------
    None
    '''
    trials = trials_train + trials_test
    # prepare widget
    main_window = pg.GraphicsLayoutWidget(title='DQN Demo')
    main_window.show()
    # prepare simulator
    simulator = UnitySimulator('room', resize=(30, 1))
    # init environment
    modules: dict[str, Any] = {}
    interface = Topology(modules, topology_nodes, staring_nodes, simulator, main_window)
    # init monitor and prepare callbacks
    el_monitor = EscapeLatencyMonitor(trials, steps, main_window)
    callbacks: CallbackDict = {'on_trial_end': [el_monitor.update]}
    # init and train agent
    policies = (EpsilonGreedy(0.3), EpsilonGreedy(0.)) #policy and policy_test
    assert isinstance(interface.observation_space.shape, tuple)
    model = FlexibleTorchNetwork(Model(interface.observation_space.shape, 4))
    agent = DQN(interface.observation_space, interface.action_space, policies[0], model, policy_test=policies[1], custom_callbacks=callbacks)
    agent.train(interface, trials_train, steps)
    agent.test(interface, trials_test, steps)
    # close widget
    main_window.close()
    # stop simulator
    simulator.stop()


if __name__ == '__main__':
    torch.set_num_threads(1)
    # generate topology
    nodes: dict[NodeID, Node]
    starting_nodes: list[NodeID]
    if len(sys.argv) > 1 and sys.argv[1] == 'grid':
        nodes, starting_nodes = grid(5, (-5.0, 5.0))
    elif len(sys.argv) > 1 and sys.argv[1] == 'hexagonal':
        nodes, starting_nodes = hexagonal(10, (-5.0, 5.0))
    elif len(sys.argv) > 1 and sys.argv[1] == 't_maze':
        nodes, starting_nodes = t_maze(9, 4, 2, 0.5)
    else:
        nodes, starting_nodes = linear_track(11, 2, 0.5, 20, 'right')
    # run
    simulation_run(nodes, starting_nodes)
