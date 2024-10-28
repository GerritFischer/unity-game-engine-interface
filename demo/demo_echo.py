# framework imports
from unity import UnitySimulator


def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator('debug_scene')
    #print(simulator.echo("Test"))

    while True:
        # keep connection open
        pass


if __name__ == '__main__':
    single_run()
