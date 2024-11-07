# framework imports
from unity import UnitySimulator
import time


def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_1', running=True)
    print(simulator.echo("Test"))

    for i in range(3):
        time.sleep(1)
        simulator.change_scene("debug_2")
        time.sleep(1)
        simulator.change_scene("debug_3")
        time.sleep(1)
        simulator.change_scene("debug_1")

    print("Press ANY key to exit...")
    input()


if __name__ == '__main__':
    single_run()
