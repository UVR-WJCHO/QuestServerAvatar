# udp_sender.py
import socket
import time

target_ip = "216.165.95.147"
target_port = 8111

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

try:
    while True:
        dummy_data = f"Sending hello to IP: {target_ip} / Port: {target_port}".encode()

        sock.sendto(dummy_data, (target_ip, target_port))
        print(f"Sent: {dummy_data}")
        time.sleep(1)
except KeyboardInterrupt:
    print("Sender stopped.")
finally:
    sock.close()