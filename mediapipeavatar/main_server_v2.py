import socket
import sys
import threading
import global_vars


forward_ip, forward_port = None, None

class ServerUDP:
    def __init__(self, ip, port):
        self.ip = ip
        self.port = port
        # self.forward_ip = forward_ip
        # self.forward_port = forward_port
        self.running = True  # 프로그램 실행 상태 확인

        try:
            self.recv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.recv_socket.bind((self.ip, self.port))

            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.bind((global_vars.HOST, 8080))
            self.server_socket.listen(1)

            self.send_socket = None

            print(f"Listening on {self.ip}:{self.port}")
            # print(f"Forwarding received data to {self.forward_ip}:{self.forward_port}")

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

                if self.send_socket:
                    self.send_socket.sendall(modified_data)
                    print("sending ...")
                    # print(f"Forwarded to {self.forward_ip}:{self.forward_port}")

            except socket.timeout:
                print("No data received. Server shutting down.")
                self.shutdown()

            except (socket.error, KeyboardInterrupt) as e:
                print(f"Connection lost or interrupted: {e}")
                self.shutdown()

    def shutdown(self):
        self.running = False
        self.recv_socket.close()
        if self.send_socket:
            self.send_socket.close()
        print("Server stopped.")
        sys.exit(0)

    def accept_connection(self):
        """클라이언트 연결을 수락"""
        while self.running:
            try:
                self.send_socket, client_addr = self.server_socket.accept()
                print(f"Client connected from {client_addr}")
            except socket.error as e:
                print(f"Error accepting connection: {e}")
                self.shutdown()


def wait_for_exit(server):
    input("Press Enter to stop the server...\n")
    server.shutdown()

if __name__ == "__main__":
    server = ServerUDP("0.0.0.0", global_vars.PORT)

    # 클라이언트 연결 수락을 별도 스레드에서 실행
    accept_thread = threading.Thread(target=server.accept_connection)
    accept_thread.start()

    input_thread = threading.Thread(target=wait_for_exit, args=(server,))
    input_thread.start()

    server.start()