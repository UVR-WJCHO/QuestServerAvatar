#pipe server
from body import BodyThread
import time
import struct
import global_vars
from sys import exit

thread = BodyThread()
thread.start()

i = input()
print("Exiting…")
try:
    thread.socket.shutdown(socket.SHUT_RDWR)  # 양방향 연결 종료
except OSError:
    pass  # 이미 닫힌 경우 무시
thread.socket.close()
global_vars.KILL_THREADS = True
time.sleep(0.5)
exit()