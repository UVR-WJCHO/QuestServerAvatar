import socket
import global_vars


class ServerUDP:
    def __init__(self, ip, port, forward_ip, forward_port):
        self.ip = ip
        self.port = port
        self.forward_ip = forward_ip
        self.forward_port = forward_port

        self.recv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.recv_socket.bind((self.ip, self.port))

        self.send_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        print(f"Listening on {self.ip}:{self.port}")
        print(f"Forwarding received data to {self.forward_ip}:{self.forward_port}")

    def start(self):
        while True:
            data, addr = self.recv_socket.recvfrom(65535)  # 최대 65,535 바이트 수신
            message = data.decode('utf-8', errors='ignore')
            print(f"Received from {addr}")#: {message}")
            print(message.split('\n')[-2])

            # 수신한 데이터 그대로 127.0.0.1로 전송
            self.send_socket.sendto(data, (self.forward_ip, self.forward_port))
            print(f"Forwarded to {self.forward_ip}:{self.forward_port}")

if __name__ == "__main__":
    server = ServerUDP("0.0.0.0", global_vars.PORT, "127.0.0.1", global_vars.PORT_Unity)
    server.start()