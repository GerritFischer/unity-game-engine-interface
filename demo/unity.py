# basic imports
import socket
import json
import os
import subprocess
import numpy as np
# framework imports
from cobel3.interface.simulator.simulator import Simulator, ImageInfo, Pose
# typing imports
from typing import Any, TypedDict, List
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
                error_msg: str = 'ERROR: Unity executable path was neither set or given as argument!'
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

        # get image info of agent
        self.image_info: ImageInfo = self.get_image_info()

        print("Unity simulator initialized!")

    def receive_in_chunks(self, socket: socket.socket, chunk_size: int) -> bytes:
        '''
        This function reads data in chunks from a socket.

        Parameters
        ----------
        socket :                            The socket to read the data from.
        chunk_size :                        The size of the chunk to read.

        Returns
        ----------
        data :                              The data as a byte string.
        '''
        # define buffer
        data = b''
        # read the data in chunks
        while True:
            data_chunk = socket.recv(chunk_size)
            data += data_chunk
            if data[-16:] == b'!unityservereod!':
                break

        return data[:-16]

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

    def get_image_info(self) -> ImageInfo:
        '''
        This function retrieves image information from the simulation.

        Parameters
        ----------
        None

        Returns
        ----------
        image_info :            A dictionary containing information about the images rendered by Unity (i.e. dimensions, channels, format).
        '''
        # send the request for image information to unity
        self.control_socket.send(json.dumps({'command': 'get_image_info', 'param': []}).encode('utf-8'))
        # retrieve image information
        image_info: ImageInfo = json.loads(self.receive_in_chunks(self.data_socket, 1024).decode('utf-8'))
        self.data_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        return image_info

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
        # send the actuation command to the virtual agent
        send_str = json.dumps({'command': 'step_simulation_without_physics', 'param': [x, y, yaw]})
        self.control_socket.send(send_str.encode('utf-8'))
        self.control_socket.recv(50)

        # retrieve image
        send_str: str = json.dumps({'command': 'get_image', 'param': []})
        self.control_socket.send(send_str.encode('utf-8'))
        image_bytes: bytes = self.receive_in_chunks(self.video_socket, self.image_info['height'] * self.image_info['width'] * self.image_info['channels'])

        # convert image into right format
        image_data: NDArray = np.frombuffer(image_bytes, dtype=np.uint8)
        image_data = np.reshape(image_data, (self.image_info['height'], self.image_info['width'], self.image_info['channels']))

        self.video_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        # TODO: return pose aswell
        return image_data

    # to be implemented
    def get_illumination(self, light_source: str) -> NDArray:
        return super().get_illumination(light_source)

    def get_objects(self) -> dict[str, Any]:

        send_str: str = json.dumps({'command': 'get_objects', "param": []})
        self.control_socket.send(send_str.encode('utf-8'))

        byteLenght = self.data_socket.recv(50)
        self.data_socket.send('AKN'.encode('utf-8'))
        byteSize = int(byteLenght[:-16].decode("utf-8"))

        allInfos: bytes = self.receive_in_chunks(self.data_socket, byteSize)
        allInfos: str = json.loads(allInfos.decode("utf-8"))

        infoDic: dict[str, Any] = {}
        keys: List[str] = allInfos["keys"]
        names: List[str] = allInfos["names"]
        values: List[str] = allInfos["values"]

        for key, name, value in zip(keys, names, values):
            infoDic[key] = {'name: ' + name + ' type: ' + value}

        self.data_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        return infoDic

    def move_object(self, object_id: str, pose) -> None:
        return super().move_object(object_id, pose)

    def set_illumination(self, light_source: str, color: NDArray) -> None:
        return super().set_illumination(light_source, color)

    def stop(self) -> None:
        return super().stop()
