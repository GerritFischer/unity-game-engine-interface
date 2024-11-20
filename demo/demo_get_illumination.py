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
    simulator = UnitySimulator(scene_name='debug_scene', running=True)
    s = simulator.get_illumination("Directional Light")
    d = simulator.get_illumination("test_0")
    k = simulator.get_illumination("Directional Light")
    print(s)
    print(type(s))
    print(d)
    print(k)
    
    
if __name__ == '__main__':
    single_run()

