# Internally used, don't mind this.
KILL_THREADS = False

# Toggle this in order to view how your WebCam is being interpreted (reduces performance).
DEBUG = True 

# Change UDP connection settings (must match Unity side)
USE_LEGACY_PIPES = False # Only supported on Windows (if True, use NamedPipes rather than UDP sockets)

"""
HOST, PORT for main_kinect.py 
- HOST must be fixed ip
- PORT must be allowed in ict.kaist.ac.kr for external connection
"""
HOST = '192.168.0.4' # n5 2268 pc2 : 192.168.0.5 / n5 2325 server : 210.117.228.110
PORT = 52733   # test port for internal connection

"""
Port for local connection (server - Quest)
- must be same with Port in Unity Pipeserver.cs
"""
PORT_Unity = 52734



# Settings do not universally apply, not all WebCams support all frame rates and resolutions
CAM_INDEX = 0 # OpenCV2 webcam index, try changing for using another (ex: external) webcam.
USE_CUSTOM_CAM_SETTINGS = False
FPS = 60
WIDTH = 320
HEIGHT = 240

# [0, 2] Higher numbers are more precise, but also cost more performance. The demo video used 2 (good environment is more important).
MODEL_COMPLEXITY = 2