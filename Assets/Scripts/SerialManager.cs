using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

/// <summary>
/// 시리얼 포트 통신 관리 싱글톤
/// - 외부 하드웨어(버튼 컨트롤러 등)와 직렬 통신을 담당한다
/// - 수신 데이터는 스레드 안전 큐를 통해 메인 스레드에서 처리한다
///
/// 데이터 포맷: [헤더 바이트][데이터 바이트...]
///   - 수신: data[0] = _receiveHeader, data[1..N] = 수신 데이터
///   - 송신: sendBuffer[0] = _sendHeader, sendBuffer[1..N] = 송신 데이터
/// </summary>
public class SerialManager : MonoSingleton<SerialManager>
{
    [Header("Serial Settings")]
    [SerializeField] private string _portName;              // 시리얼 포트 이름 (예: COM3)
    [SerializeField] private int _baudRate = 9600;          // 통신 속도 (bps)

    [Header("Data Settings")]
    [SerializeField] private byte _sendHeader = 0xFA;       // 송신 패킷 헤더
    [SerializeField] private byte _receiveHeader = 0xFA;    // 수신 패킷 헤더
    [SerializeField] private int _sendDataLength;           // 송신 패킷 총 길이
    [SerializeField] private int _receiveDataLength;        // 수신 패킷 총 길이
    [Tooltip("ASCII로 입력해야 함 (예: 0x31 = 49)")]
    [SerializeField] private byte[] _sendDataArray;         // 기본 송신 데이터

    /// <summary>데이터 송신 완료 시 이벤트</summary>
    public event Action<Byte[]> SendDataHandler;

    /// <summary>데이터 수신 완료 시 이벤트</summary>
    public event Action<Byte[]> ReceiveDataHandler;

    private SerialPort _serialPort;

    // 수신 스레드에서 메인 스레드로 데이터를 전달하기 위한 스레드 안전 큐
    private readonly Queue<Byte[]> _mainThreadQueue = new Queue<Byte[]>();

    /// <summary>
    /// 송신 데이터 배열 (외부 수정 방어를 위해 복사본 반환/설정)
    /// </summary>
    public byte[] SendDataArray
    {
        get
        {
            if (_sendDataArray == null) return null;
            byte[] copy = new byte[_sendDataArray.Length];
            Array.Copy(_sendDataArray, copy, _sendDataArray.Length);
            return copy;
        }
        set
        {
            if (value == null)
            {
                _sendDataArray = null;
                return;
            }
            _sendDataArray = new byte[value.Length];
            Array.Copy(value, _sendDataArray, value.Length);
        }
    }

    #region Unity 이벤트

    protected override void OnSingletonAwake()
    {
        ConnectPort();
    }

    private void Start()
    {
        // 수신 코루틴 시작
        StartCoroutine(ListeningSerialPort());

        // 기본 핸들러 등록 (수신 로그 출력, 송신 실행)
        ReceiveDataHandler += PrintReceiveData;
        SendDataHandler += SendData;
    }

    private void Update()
    {
        // 수신 스레드가 큐에 담아둔 데이터를 메인 스레드에서 꺼내 처리
        lock (_lock)
        {
            while (_mainThreadQueue.Count > 0)
            {
                Byte[] data = _mainThreadQueue.Dequeue();

                // 헤더가 일치하면 수신 이벤트 발행
                if (data[0] == _receiveHeader)
                    ReceiveDataHandler?.Invoke(data);
            }
        }
    }

    protected override void OnSingletonApplicationQuit()
    {
        ClosePort();
    }

    protected override void OnSingletonDestroy() { }

    #endregion

    #region 내부 처리 메서드

    /// <summary>시리얼 포트를 열고 연결한다</summary>
    private void ConnectPort()
    {
        try
        {
            _serialPort = new SerialPort(_portName, _baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = -1        // 무한 대기 (코루틴에서 BytesToRead로 가드)
            };

            _serialPort.Open();
            Log($"Port {_portName} opened successfully");
        }
        catch (Exception e)
        {
            Log($"Failed to open port {_portName}: {e.Message}", ELogType.Error);
        }
    }

    /// <summary>시리얼 포트를 닫는다</summary>
    private void ClosePort()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            Log($"Port {_portName} closed");
        }
    }

    /// <summary>
    /// 시리얼 포트에서 데이터를 읽어 큐에 담는 코루틴
    /// 포트가 열려있는 동안 지속 실행된다
    /// </summary>
    private IEnumerator ListeningSerialPort()
    {
        Byte[] receiveBuffer = new Byte[_receiveDataLength];

        while (_serialPort != null && _serialPort.IsOpen)
        {
            if (_serialPort.BytesToRead > 0)
            {
                try
                {
                    // 버퍼 크기를 초과하지 않도록 읽을 바이트 수를 제한
                    int toRead = Math.Min(_serialPort.BytesToRead, receiveBuffer.Length);
                    int bytesRead = _serialPort.Read(receiveBuffer, 0, toRead);

                    // 정해진 패킷 길이만큼 읽혔을 때만 처리
                    if (bytesRead == _receiveDataLength)
                    {
                        // 큐 추가 (복사본 사용 - 버퍼 재사용 충돌 방지)
                        Byte[] temp = new Byte[receiveBuffer.Length];
                        Array.Copy(receiveBuffer, temp, receiveBuffer.Length);

                        lock (_lock)
                        {
                            _mainThreadQueue.Enqueue(temp);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log($"Read error: {e.Message}", ELogType.Warning);
                    break;
                }
            }
            else
            {
                // 수신 데이터가 없으면 0.5초 대기
                yield return new WaitForSeconds(0.5f);
            }

            yield return null;
        }
    }

    /// <summary>
    /// 수신 데이터를 로그로 출력하는 기본 핸들러
    /// </summary>
    private void PrintReceiveData(Byte[] data)
    {
        Log($"header : {data[0]}");
        for (int i = 1; i < _receiveDataLength; ++i)
        {
            Log($"receive[{i}] : {data[i]}");
        }
    }

    #endregion

    #region 외부 호출 메서드

    /// <summary>
    /// 시리얼 포트로 데이터를 송신한다
    /// 포맷: [_sendHeader][data[0]]...[data[N]]
    /// </summary>
    public void SendData(Byte[] data)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            Log("Serial port is not open", ELogType.Warning);
            return;
        }

        try
        {
            // 헤더를 앞에 붙여 송신 버퍼 구성
            byte[] sendBuffer = new byte[_sendDataLength];
            sendBuffer[0] = _sendHeader;
            for (int i = 1; i < _sendDataLength; ++i)
            {
                sendBuffer[i] = data[i - 1];
            }

            _serialPort.Write(sendBuffer, 0, _sendDataLength);
        }
        catch (Exception e)
        {
            Log($"Failed to send data: {e.Message}", ELogType.Warning);
        }
    }

    #endregion
}
