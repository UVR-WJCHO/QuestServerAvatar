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
time.sleep(0.5)
exit()