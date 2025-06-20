#pipe server
from body import BodyThread
import time
import struct
import global_vars
from sys import exit

thread = BodyThread()
thread.start()

try:
    input("Press any key to stop the thread and exit...\n")
finally:
    print("Stopping thread...")
    global_vars.KILL_THREADS = True
    thread.stop()  # BodyThread 클래스에 stop 메서드가 정의되어 있어야 함

