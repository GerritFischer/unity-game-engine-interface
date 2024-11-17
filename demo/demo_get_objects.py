# framework imports
from unity import UnitySimulator
import time

# currently only for testing will be removed later 
def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_1', running=True)
    test = simulator.get_objects()
    print(test)
    
    
    
if __name__ == '__main__':
    single_run()

