# framework imports
from unity import UnitySimulator
# from matplotlib import pyplot as plt
# from matplotlib.animation import FuncAnimation
from PyQt6.QtCore import QTimer
from PyQt6.QtGui import QPainter, QImage
from PyQt6.QtWidgets import QApplication, QWidget
import sys
import math


def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_1')

    class ImageWindow(QWidget):
        # window class to display image from simulator
        def __init__(self):
            super().__init__()

            self.update_qimage()

            # Set up a timer to update the pixmap every 33 ms (30 frames per second)
            self.timer = QTimer(self)
            self.timer.timeout.connect(self.update_qimage)
            self.timer.start(33)

            # Set window properties
            self.setWindowTitle("Image demo")
            self.setGeometry(100, 100, 1024, 256)
            print(simulator.echo("Simulator Echo"))
            print(simulator.get_objects())

        def paintEvent(self, event):
            # Paint the qimage on the window
            painter = QPainter(self)
            painter.drawImage(0, 0, self.qimage)

        def update_qimage(self):
            # prints can be used to verify agent behaviour

            pose, arr = simulator.move_agent(0, 0, 1)
            #print("agent pose:" + str(pose))
            simulator.move_object("test_0", (-3, 0, 0, 1, 1, 1))
            simulator.set_illumination("Directional Light", [math.floor(pose[4] / 360 * 255.0), 0, 0])
            #print("current illumination " + str(simulator.get_illumination("Directional Light")))
            # generate qimage
            self.qimage = QImage(arr.data, arr.shape[1], arr.shape[0], arr.shape[1] * 3, QImage.Format.Format_RGB888)
            # trigger a repaint
            self.update()

# Create the application and run the main window
    app = QApplication(sys.argv)
    window = ImageWindow()
    window.show()
    app.exec()


if __name__ == '__main__':
    single_run()
