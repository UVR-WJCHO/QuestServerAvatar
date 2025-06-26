# udp_receiver.py
import socket

listen_ip = "0.0.0.0"
listen_port = 8111

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((listen_ip, listen_port))

print(f"Listening on {listen_ip}:{listen_port}...")

try:
    while True:
        data, addr = sock.recvfrom(1024)  # 최대 1024 바이트 수신
        print(f"Received from {addr}: {data}")
except KeyboardInterrupt:
    print("Receiver stopped.")
finally:
    sock.close()