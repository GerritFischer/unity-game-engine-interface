#============================================================================
# NOTE: THIS FILE IS JUST FOR DEBUGGING AND TESTING AND WILL BE DELETED LATER
# =========================================================================== 
# framework imports
from unity import UnitySimulator
# from matplotlib import pyplot as plt
# from matplotlib.animation import FuncAnimation
from PyQt6.QtCore import QTimer
from PyQt6.QtGui import QPainter, QImage
from PyQt6.QtWidgets import QApplication, QWidget
import sys


def single_run() -> None:
    '''
    This method performs a single run.
    In this case it is used for debugging and testing
    '''
    # init simulator
    simulator = UnitySimulator(scene_name='debug_1', running=True)
    print(simulator.echo("Test"))

    '''
    # OLD MATPLOTLIBCODE
    # Create the plot
    fig, ax = plt.subplots()
    im = ax.imshow(data, interpolation='nearest')
    def update(frame: int) -> np.ndarray:
        """
        This function will be called at each animation frame to update the image.
        """
        # Move the agent and get the new image data
        data = simulator.move_agent(0, 0, 0.01)

        # If data is an RGB image, keep only the RGB channels
        if len(data.shape) == 3:  # Assuming it's an RGB image
            data = data[:, :, :3]  # Keep only the first 3 channels (RGB)

        # Update the image with the new data
        im.set_data(data)
        print(f"Drawn frame {frame}")

        return [im]  # Return the updated image

    # Create an animation with 10 frames, updating every 0.1 seconds
    ani = FuncAnimation(fig, update, frames=1000, interval=32, blit=True)

    # Display the plot
    plt.show()
    '''
    class ImageWindow(QWidget):
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

        def paintEvent(self, event):
            # Paint the qimage on the window
            painter = QPainter(self)
            painter.drawImage(0, 0, self.qimage)

        def update_qimage(self):
            arr = simulator.move_agent(0, 0, 0.01)
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
