# framework imports
from unity import UnitySimulator


def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_1', running=True)
    print(simulator.echo("Test"))

    print("Press ANY key to exit...")
    input()


if __name__ == '__main__':
    single_run()
