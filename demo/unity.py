# basic imports
import socket
import json
import os
import subprocess
# framework imports
from cobel3.interface.simulator.simulator import Simulator, ImageInfo, Pose
# typing imports
from typing import Any, TypedDict
from numpy.typing import NDArray


class UnitySimulator(Simulator):
    def __init__(self, scene_name: str, executable: None | str = None, running: bool = False) -> None:
        '''
        The Unity interface class.
        This class connects to the Unity enviroment and controls the flow of
        commands/data to/from CoBeL-RL

        Parameters
        ----------
        scene_name:         The name of the Unity scene that will be loaded.
        executable:         The path to the Unity executable.

        Returns
        ----------
        None
        '''

        # ensure that the simulator executable was provided
        if not running:
            if executable is None:
                error_msg = 'ERROR: Unity executable path was neither set or given as argument!'
                assert 'UNITY_EXECUTABLE' in os.environ, error_msg
                self.executable = os.environ['UNITY_EXECUTABLE']
            else:
                self.executable = executable
            subprocess.Popen([self.executable])

        # init sockets
        self.control_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.video_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.data_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        # connect sockets
        socketsConnected: bool = False
        while not socketsConnected:
            try:
                self.connect_socket(self.control_socket, 5000)
                print("Control socket connected!")
                self.connect_socket(self.video_socket, 5001)
                print("Video socket connected!")
                self.connect_socket(self.data_socket, 5002)
                print("Data socket connected!")
                socketsConnected = True
            except Exception:
                # if socket connects fails (120s), print error, retry
                print("Couldn't connect to unity simulator, retrying...")

        # load scene
        self.change_scene(scene_name)
        print("Unity simulator initialized!")

    def echo(self, message: str) -> str:
        '''
        This function sends a message to the unity simulator and
        recieves the echo

        Parameters
        ----------
        message:            The message to be sent.

        Returns
        ----------
        recieved_echo:       The echo that came back from the simulator
        '''
        # convert message into formatted json command and send it
        send_str: str = json.dumps({'command': 'echo', 'param': [message]})
        self.control_socket.send(send_str.encode('utf-8'))
        recieved_echo: str = self.control_socket.recv(1024).decode('utf-8')

        return recieved_echo

    def change_scene(self, scene_name: str) -> None:
        '''
        This function sends the path to a scene to the simulator
        and the command to change to this scene.

        Parameters
        ----------
        scene:            The path to the scene to change to.

        Returns
        ----------
        None
        '''
        # convert scene path into formatted json command and send it
        send_str: str = json.dumps({'command': 'change_scene', 'param': [scene_name]})
        self.control_socket.send(send_str.encode('utf-8'))
        self.control_socket.recv(50)

    def move_agent(self, x: float, y: float, yaw: float) -> tuple[NDArray, NDArray]:
        '''
        This function propels the simulation. It uses teleportation to guide the agent directly by means of global x, y, yaw values.
        
        Parameters
        ----------
        x :                                 The global x position to teleport to.
        y :                                 The global y position to teleport to.
        yaw :                               The global yaw value to teleport to.
        
        Returns
        ----------
        pose_data :                         The pose observation received from the simulation.
        image_data :                        The image observation received from the simulation.
        '''
        # send the actuation command to the virtual robot/agent
        send_str = json.dumps({'command': 'step_simulation_without_physics', 'param': [x, y, yaw]})
        self.control_socket.send(send_str.encode('utf-8'))
        self.control_socket.recv(50)
        
        pass
    # to be implemented
    def get_illumination(self, light_source: str) -> NDArray:
        return super().get_illumination(light_source)

    def get_image_info(self) -> ImageInfo:
        return super().get_image_info()

    def get_objects(self) -> dict[str, Any]:
        return super().get_objects()

    def move_object(self, object_id: str, pose) -> None:
        return super().move_object(object_id, pose)

    def set_illumination(self, light_source: str, color: NDArray) -> None:
        return super().set_illumination(light_source, color)

    def stop(self) -> None:
        return super().stop()
