# basic imports
import sys
# typing
from typing import Any
# CoBeL-RL
from cobel3.agent import QAgent
from cobel3.policy import EpsilonGreedy
from cobel3.typing import CallbackDict
from cobel3.monitor import EscapeLatencyMonitor
from cobel3.interface import Topology
from cobel3.misc.topology_tools import linear_track, grid, hexagonal, t_maze
from cobel3.interface.topology import Node, NodeID
# unity interface
from unity import UnitySimulator
# framework imports
import pyqtgraph as pg # type: ignore



def simulation_run(topology_nodes: dict[NodeID, Node], staring_nodes: list[NodeID], trials: int = 500, steps: int = 50) -> None:
    '''
    This function represents one simulation run with the Topology interface.

    Parameters
    ----------
    topology_nodes :                    The topology nodes.
    starting_nodes :                    The starting nodes.
    trials :                            The number of training trials.
    steps :                             The maximum number of steps per trial.

    Returns
    ----------
    None
    '''
    # prepare widget
    main_window = pg.GraphicsLayoutWidget(title='Topology Test')
    main_window.show()
    # prepare simulator
    simulator = UnitySimulator("room", running=True, resize=(30, 1))
    # init environment
    modules: dict[str, Any] = {}
    interface = Topology(modules, topology_nodes, staring_nodes, simulator, main_window)
    # init monitor and prepare callbacks
    el_monitor = EscapeLatencyMonitor(trials, steps, main_window)
    callbacks: CallbackDict = {'on_trial_end': [el_monitor.update]}
    # init and train agent
    policy = EpsilonGreedy(0.1)
    agent = QAgent(interface.observation_space, interface.action_space, policy, policy, custom_callbacks=callbacks)
    agent.train(interface, trials, steps, 0)
    # close widget
    main_window.close()
    # stop simulator
    simulator.stop()


if __name__ == '__main__':
    # generate topology
    nodes: dict[NodeID, Node]
    starting_nodes: list[NodeID]
    if len(sys.argv) > 1 and sys.argv[1] == 'grid':
        nodes, starting_nodes = grid(5, (-0.5, 0.5))
    elif len(sys.argv) > 1 and sys.argv[1] == 'hexagonal':
        nodes, starting_nodes = hexagonal(10, (-0.5, 0.5))
    elif len(sys.argv) > 1 and sys.argv[1] == 't_maze':
        nodes, starting_nodes = t_maze(6, 3, 2, 0.1)
    else:
        nodes, starting_nodes = linear_track(10, 2, 0.1, 20, 'right')
    # run
    simulation_run(nodes, starting_nodes)
