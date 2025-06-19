# pc_python_server.py
import socket
import threading
import time # 시간 기록을 위해 추가

# 클라이언트 연결을 처리하는 함수
def handle_client_connection(client_socket, client_address):
    print(f"[연결됨] {client_address} 에서 연결되었습니다.")
    try:
        while True:
            actual_message = input("Quest 3로 보낼 메시지를 입력하세요 (종료하려면 'exit' 입력): ")
            if actual_message.lower() == 'exit':
                print(f"[연결 종료] {client_address} 와의 연결을 종료합니다.")
                # 클라이언트가 특정 종료 신호를 기대한다면 여기서 보낼 수 있습니다.
                # 예: client_socket.sendall("SERVER_SHUTDOWN_Signal".encode('utf-8'))
                break
            
            if not actual_message: # 빈 메시지는 보내지 않음 (선택 사항)
                print("메시지가 비어있어 전송하지 않습니다.")
                continue

            # 메시지 전송 직전 시간 기록 (UTC Epoch 초 단위 float)
            send_timestamp = time.time() 
            
            # 타임스탬프와 실제 메시지를 구분자(|)와 함께 문자열로 만듦
            message_with_timestamp = f"{send_timestamp}|{actual_message}"
            
            client_socket.sendall(message_with_timestamp.encode('utf-8'))
            print(f"[전송됨] {client_address}로 '{actual_message}' (타임스탬프 포함) 전송 완료")

    except socket.error as e:
        print(f"[소켓 오류] {client_address} 와 통신 중 오류 발생: {e}")
    except EOFError: # input() 에서 Ctrl+D 등으로 EOF 발생 시
        print(f"[입력 오류] 입력 스트림이 닫혔습니다. 클라이언트 {client_address} 연결이 끊어졌을 수 있습니다.")
    except Exception as e:
        print(f"[예외 발생] {client_address} 처리 중 예외: {e}")
    finally:
        print(f"[연결 해제] {client_address} 연결이 해제되었습니다.")
        client_socket.close()

# 서버를 시작하는 함수
def start_server(host='0.0.0.0', port=8080):
    # IPv4, TCP 소켓 생성
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    # 주소 재사용 옵션 설정 (서버가 비정상 종료 후 바로 재시작 시 유용)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    
    try:
        server_socket.bind((host, port))
        server_socket.listen(1) # 한 번에 하나의 연결만 허용 (간단한 예제용)
        print(f"[대기중] 서버가 {host}:{port} 에서 연결을 기다리고 있습니다...")

        while True: # 여러 클라이언트의 연결을 순차적으로 받을 수 있도록 루프
            try:
                client_socket, client_address = server_socket.accept()
                # 이 예제에서는 한 번에 한 클라이언트만 처리합니다.
                # handle_client_connection이 완료될 때까지 다음 accept는 대기합니다.
                handle_client_connection(client_socket, client_address)
            except socket.error as e:
                print(f"[Accept 오류] 연결 수립 중 오류: {e}")
                # 오류 발생 시 잠시 후 다시 시도하거나, 특정 조건에서 서버를 종료할 수 있습니다.
            except KeyboardInterrupt: # Ctrl+C 입력 시
                print("\n[서버 종료] 사용자에 의해 서버가 종료됩니다...")
                break # 메인 루프 종료

    except socket.error as e:
        print(f"[바인딩 오류] 포트 {port}에 바인딩할 수 없습니다: {e}. 해당 포트가 이미 사용 중인지 확인하세요.")
    except Exception as e:
        print(f"[서버 오류] 예기치 않은 오류 발생: {e}")
    finally:
        print("[소켓 닫음] 서버 소켓을 닫습니다.")
        server_socket.close()

if __name__ == "__main__":
    # PC의 IP 주소를 사용합니다. '0.0.0.0'은 모든 사용 가능한 인터페이스에서 수신 대기합니다.
    # Quest 3 클라이언트에서는 이 PC의 로컬 네트워크 IP 주소를 사용해야 합니다.
    HOST_IP = '143.248.100.184' 
    PORT_NUMBER = 8080  # Quest 3 클라이언트 스크립트의 포트와 동일해야 합니다.
    
    # PC의 로컬 IP 확인 방법 (참고용):
    # Windows: cmd에서 'ipconfig' 입력 후 'IPv4 주소' 확인
    # macOS/Linux: 터미널에서 'ifconfig' 또는 'ip addr' 입력
    
    start_server(HOST_IP, PORT_NUMBER)