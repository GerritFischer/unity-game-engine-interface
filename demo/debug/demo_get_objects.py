# framework imports
from unity import UnitySimulator
import time
import numpy as np

# currently only for testing will be removed later 
def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_scene', running=True)
    arr = np.array([255, 187, 0])
    print(type(arr[1]))
    arr2 = np.array([0, 255, 10])
    simulator.set_illumination("Directional Light", arr)
    k = simulator.get_illumination("Directional Light")
    print("this is")
    print(k)
    print(k[1])
    simulator.set_illumination("test_0", arr)
    simulator.set_illumination("Directional Light", arr2)
    print("stop")
    d = simulator.get_illumination("test_0")
    v = simulator.get_illumination("Directional Light")
    print("this is")
    print(v)
    print(d)
    
    
print("laallaal")
if __name__ == '__main__':
    single_run()
print("test333")
