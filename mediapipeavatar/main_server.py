import socket
import global_vars


class ServerUDP:
    def __init__(self, ip, port, forward_ip, forward_port):
        self.ip = ip
        self.port = port
        self.forward_ip = forward_ip
        self.forward_port = forward_port

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
        # 줄 단위로 분리
        lines = message.split("\n")

        # 뒤에서 두 번째 줄 제거
        if len(lines) > 1:
            log_time = lines[-2]
            del lines[-2]  # 뒤에서 두 번째 줄 삭제

        # 수정된 메시지 재조합 및 UTF-8 인코딩
        modified_message = "\n".join(lines).encode('utf-8')
        return modified_message, log_time



    def start(self):
        while True:
            try:
                data, addr = self.recv_socket.recvfrom(65535)  # 최대 65,535 바이트 수신
                message = data.decode('utf-8', errors='ignore')
                print(f"Received from {addr}")#: {message}")

                modified_data, log_time = self.process_message(message)
                print("time : ", log_time)

                # 수신한 데이터 그대로 127.0.0.1로 전송
                self.send_socket.sendto(modified_data, (self.forward_ip, self.forward_port))
                print(f"Forwarded to {self.forward_ip}:{self.forward_port}")

            except socket.timeout:
                print("No data received. Server shutting down.")
                sys.exit(1)  # 타임아웃 발생 시 종료

            except (socket.error, KeyboardInterrupt) as e:
                print(f"Connection lost or interrupted: {e}")
                sys.exit(1)  # 연결 끊김 발생 시 종료


if __name__ == "__main__":
    server = ServerUDP("0.0.0.0", global_vars.PORT, "127.0.0.1", global_vars.PORT_Unity)
    server.start()