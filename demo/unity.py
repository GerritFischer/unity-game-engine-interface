# basic imports
import math
import socket
import json
import os
import subprocess
# typing imports
from typing import Any, List
from numpy.typing import NDArray
# framework imports
import numpy as np
from cobel3.interface.simulator.simulator import Simulator, ImageInfo, Pose




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
            with subprocess.Popen([self.executable]) as _:
                pass

        # init sockets
        self.control_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.video_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.data_socket: socket.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        # connect sockets
        sockets_connected: bool = False
        while not sockets_connected:
            try:
                self.connect_socket(self.control_socket, 5000)
                print("Control socket connected!")
                self.connect_socket(self.video_socket, 5001)
                print("Video socket connected!")
                self.connect_socket(self.data_socket, 5002)
                print("Data socket connected!")
                sockets_connected = True
            except (socket.timeout, socket.error, ConnectionRefusedError):
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
        used_socket :                            The socket to read the data from.
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
        self.control_socket.send(json.dumps({'command': 'echo', 'param': [message]}).encode('utf-8'))
        recieved_echo: str = self.control_socket.recv(1024).decode('utf-8')

        return recieved_echo

    def change_scene(self, scene: str) -> None:
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
        self.control_socket.send(json.dumps({'command': 'change_scene', 'param': [scene]}).encode('utf-8'))
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
        self.control_socket.send(json.dumps({'command': 'step_simulation_without_physics', 'param': [x, y, yaw]}).encode('utf-8'))
        self.control_socket.recv(50)

        # retrieve image
        self.control_socket.send(json.dumps({'command': 'get_image', 'param': []}).encode('utf-8'))
        image_bytes: bytes = self.receive_in_chunks(self.video_socket, self.image_info['height'] * self.image_info['width'] * self.image_info['channels'])

        # convert image into right format
        image_data: NDArray = np.frombuffer(image_bytes, dtype=np.uint8)
        image_data = np.reshape(image_data, (self.image_info['height'], self.image_info['width'], self.image_info['channels']))

        self.video_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        # retrieve agent position
        agent_pose: Pose | None = self.get_pose("agent")

        return image_data, np.array(agent_pose)

    def get_illumination(self, light_source: str) -> NDArray:
        '''
        This function retrieves color values of a light source

        Parameters
        ----------
        None

        Returns
        ----------
        colors :            A Array containing the color values in RGB of the parameter object if object not found it will return -1, -1, -1
        '''

        self.control_socket.send(json.dumps({'command': 'get_illumination', "param": [light_source]}).encode('utf-8'))

        illumination_info_json: bytes = self.receive_in_chunks(self.data_socket, 1024)
        illumination_info: dict = json.loads(illumination_info_json.decode("utf-8"))
        colors: List[int] = illumination_info["colors"]
        final_colors: NDArray = (np.array(colors) * 255 + 0.1).astype(int)

        self.data_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        if final_colors.size == 0:
            return np.array({-1, -1, -1})
        return final_colors

    def get_objects(self) -> dict[str, Any]:
        '''
        This function retrieves object information from the current unity scene

        Parameters
        ----------
        None

        Returns
        ----------
        info_dic :            A dictionary containing information about all the objects in the current Unity scene
        '''
        self.control_socket.send(json.dumps({'command': 'get_objects', "param": []}).encode('utf-8'))

        byte_length: bytes = self.data_socket.recv(50)
        self.data_socket.send('AKN'.encode('utf-8'))
        byte_size = int(byte_length[:-16].decode("utf-8"))

        object_info_json: bytes = self.receive_in_chunks(self.data_socket, byte_size)
        object_info: dict = json.loads(object_info_json.decode("utf-8"))

        info_dic: dict[str, Any] = {}
        keys: List[str] = object_info["keys"]
        names: List[str] = object_info["names"]
        values: List[str] = object_info["values"]

        for key, name, value in zip(keys, names, values):
            info_dic[key] = {'name: ' + name + ' type: ' + value}

        self.data_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        return info_dic

    def move_object(self, object_id: str, pose: Pose) -> None:
        '''
        This function propels the simulation. It uses teleportation to guide the agent directly by means of global x, y, yaw values.

        Parameters
        ----------
        object_id :                         The id of the moved object, (currently the name)
        pose :                              The tuple contains the new position and rotation

        Returns
        ----------
        None
        '''

        # invokes move_object
        send_str_move: str = json.dumps({'command': 'move_object', "param": [object_id, pose[0], pose[1],
                                                                        pose[2], pose[3], pose[4], pose[5]]})
        self.control_socket.send(send_str_move.encode('utf-8'))
        self.control_socket.recv(50)

    def stop(self) -> None:
        '''
        This function stops the unity simulator. It does not expect a AKN from the simulator.

        Parameters
        ----------
        None

        Returns
        ----------
        None
        '''
        self.control_socket.send(json.dumps({'command': 'stop', "param": []}).encode('utf-8'))

    def set_illumination(self, light_source: str, color: NDArray) -> None:
        '''
        This function sets the RGB values of a light component connected to a game object.

        Parameters
        ----------
        light_source:                         The name of the object the light source is connected to
        colors:                               An array of RGB values ranging from 0 to 255

        Returns
        ----------
        None
        '''
        self.control_socket.send(json.dumps({'command': 'set_illumination', "param": [light_source, color[0], color[1], color[2]]}).encode('utf-8'))
        self.control_socket.recv(50)

    def get_pose(self, object_id:str)-> Pose | None:
        '''
        This function will return the pose of a given object. If the object name is not valid it will return None.

        Parameters
        ----------
        object_id:                            The name of the object.

        Returns
        ----------
        pose:                                 The pose of the object.
        '''
        self.control_socket.send(json.dumps({'command': 'get_position', "param": [object_id]}).encode('utf-8'))

        pose_info_json: bytes = self.receive_in_chunks(self.data_socket, 1024)
        pose_info: dict = json.loads(pose_info_json.decode("utf-8"))

        pose: Pose = tuple(pose_info["poses"])
        self.data_socket.send('AKN'.encode('utf-8'))
        self.control_socket.recv(50)

        if math.isnan(pose[0]):
            return None

        return pose
