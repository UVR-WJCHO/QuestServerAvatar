import socket
import sys
import threading
import global_vars

class ServerUDP:
    def __init__(self, ip, port, forward_ip, forward_port):
        self.ip = ip
        self.port = port
        self.forward_ip = forward_ip
        self.forward_port = forward_port
        self.running = True  # 프로그램 실행 상태 확인

        try:
            self.recv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.recv_socket.bind((self.ip, self.port))

            self.send_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

            print(f"Listening on {self.ip}:{self.port}")
            print(f"Forwarding received data to {self.forward_ip}:{self.forward_port}")

        except Exception as e:
            print(f"Error initializing server: {e}")
            sys.exit(1)  # 초기화 실패 시 종료

    def process_message(self, message):
        lines = message.split("\n")

        # 뒤에서 두 번째 줄 제거
        if len(lines) > 1:
            log_time = lines[-2]
            del lines[-2]

        modified_message = "\n".join(lines).encode('utf-8')
        return modified_message, log_time

    def start(self):
        while self.running:
            try:
                data, addr = self.recv_socket.recvfrom(65535)
                message = data.decode('utf-8', errors='ignore')
                print(f"Received from {addr}")

                modified_data, log_time = self.process_message(message)
                print("time : ", log_time)

                self.send_socket.sendto(modified_data, (self.forward_ip, self.forward_port))
                print(f"Forwarded to {self.forward_ip}:{self.forward_port}")

            except socket.timeout:
                print("No data received. Server shutting down.")
                self.shutdown()

            except (socket.error, KeyboardInterrupt) as e:
                print(f"Connection lost or interrupted: {e}")
                self.shutdown()

    def shutdown(self):
        self.running = False
        self.recv_socket.close()
        self.send_socket.close()
        print("Server stopped.")
        sys.exit(0)

def wait_for_exit(server):
    input("Press Enter to stop the server...\n")
    server.shutdown()

if __name__ == "__main__":
    server = ServerUDP("0.0.0.0", global_vars.PORT, "127.0.0.1", global_vars.PORT_Unity)
    input_thread = threading.Thread(target=wait_for_exit, args=(server,))
    input_thread.start()

    server.start()