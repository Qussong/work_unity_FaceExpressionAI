using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using PagingTemplate.Util;
using UnityEngine;


public class SerialManager : MonoSingleton<SerialManager>
{
    private readonly object _lock = new object();
    [Header("Serial Settings")]
    private string _portName;              // 시리얼 포트 이름
    [SerializeField] private int _baudRate = 9600;          // 통신 속도 (bps)

    [Header("Data Settings")]
    [SerializeField] private byte _sendHeader = 0xFA;       // 송신 데이터 헤더
    [SerializeField] private byte _receiveHeader = 0xFA;    // 수신 데이터 헤더
    [SerializeField] private int _sendDataLength;           // 송신 데이터 길이
    [SerializeField] private int _receiveDataLength;        // 수신 데이터 길이
    [Tooltip("ASCII로 입력해야함 (0x31 = 49)")]
    [SerializeField] private byte[] _sendDataArray;         // 송신 데이터

    public event Action<Byte[]> SendDataHandler;            // 데이터 송신 시 호출
    public event Action<Byte[]> ReceiveDataHandler;         // 데이터 수신 시 호출

    private bool _useSerial = true;                    // 시리얼 통신 사용 여부
    private SerialPort _serialPort;
    private readonly Queue<Byte[]> _mainThreadQueue = new Queue<Byte[]>();

    /// <summary>
    /// 프로퍼티
    /// </summary>
    public byte[] SendDataArray
    {
        get 
        {
            if (_sendDataArray == null) return null;
            // 외부 변조 방지
            byte[] copy = new byte[_sendDataArray.Length];
            Array.Copy(_sendDataArray, copy, _sendDataArray.Length);
            return copy;
        }
        set
        {
            if(value == null)
            {
                _sendDataArray = null;
                return;
            }
            _sendDataArray = new byte[value.Length];
            Array.Copy(value, _sendDataArray, value.Length);
        }
    }


    #region 유니티 이벤트 함수

    /// <summary>
    /// Awake
    /// </summary>
    protected override void OnSingletonAwake()
    {
        LoadPortFromCSV();
        if (_useSerial)
            ConnectPort();
        else
            Debug.Log("[SerialManager] 시리얼 통신 비활성화 (bUseSerial=false)");
    }

    /// <summary>config.csv에서 시리얼 포트 이름을 읽어온다</summary>
    private void LoadPortFromCSV()
    {
        var config = CSVParser.Read("config.csv");
        if (config.TryGetValue("SerialPort", out var port))
        {
            _portName = port;
            Debug.Log($"[SerialManager] config.csv에서 포트 로드: {_portName}");
        }
        if (config.TryGetValue("bUseSerial", out var useSerial))
        {
            _useSerial = useSerial.Trim().ToLower() == "true";
            Debug.Log($"[SerialManager] 시리얼 통신 사용 여부: {_useSerial}");
        }
    }

    private void Start()
    {
        if (!_useSerial) return;
        StartCoroutine(ListeningSerialPort());
        ReceiveDataHandler += PrintReceiveData;
        SendDataHandler += SendData;
    }

    private void Update()
    {
        lock(_lock)
        {
            while(_mainThreadQueue.Count > 0)
            {
                Byte[] data = _mainThreadQueue.Dequeue();

                // 헤더가 일치하면 콜백 호출
                if (data[0] == _receiveHeader)
                {
                    ReceiveDataHandler?.Invoke(data);
                }
            }
        }

        // Test
        // if(Input.GetKeyDown(KeyCode.Space))
        // {
        //     SendDataHandler?.Invoke(_sendDataArray);
        // }
    }

    /// <summary>
    /// OnApplicationQuit
    /// </summary>
    protected override void OnSingletonApplicationQuit()
    {
        ClosePort();
    }

    /// <summary>
    /// Destroy
    /// </summary>
    protected override void OnSingletonDestroy()
    {

    }

    #endregion


    #region 내부 호출 함수

    /// <summary>
    /// 시리얼 포트 연결
    /// </summary>
    private void ConnectPort()
    {
        try
        {
            // 시리얼 포트 설정
            _serialPort = new SerialPort(_portName, _baudRate)
            {
                DataBits = 8,           // 데이터 비트: 8비트
                Parity = Parity.None,   // 패리티: 없음
                StopBits = StopBits.One,// 정지 비트: 1
                ReadTimeout = -1        // 읽기 타임아웃: 무한 대기
            };

            _serialPort.Open();
            Log($"Port {_portName} opened successfully");
        }
        catch (Exception e)
        {
            Log($"Failed to open port {_portName}: {e.Message}", ELogType.Error);
        }
    }

    /// <summary>
    /// 시리얼 포트 닫기
    /// </summary>
    private void ClosePort()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            Log($"Port {_portName} closed");
        }
    }

    /// <summary>
    /// 시리얼 포트에서 데이터 수신을 감지하는 코루틴
    /// </summary>
    private IEnumerator ListeningSerialPort()
    {
        Byte[] receiveBuffer = new Byte[_receiveDataLength];

        // 포트가 열려있는 동안 계속 수신 대기
        while (_serialPort != null && _serialPort.IsOpen)
        {
            // 수신 데이터가 있는 경우
            if (_serialPort.BytesToRead > 0)
            {
                try
                {
                    // 수신 바이트 수 제한 (버퍼 크기 초과 방지)
                    int toRead = Math.Min(_serialPort.BytesToRead, receiveBuffer.Length);
                    int bytesRead = _serialPort.Read(receiveBuffer, 0, toRead);

                    // 데이터 길이만큼 읽어온 경우 처리
                    if (bytesRead == _receiveDataLength)
                    {
                        // 데이터 복사 (스레드 안전 보호)
                        Byte[] temp = new Byte[receiveBuffer.Length];
                        Array.Copy(receiveBuffer, temp, receiveBuffer.Length);

                        // 데이터 안전하게 큐에 추가
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
    /// 시리얼 포트 수신데이터 출력
    /// </summary>
    /// <param name="data"> 수신 데이터
    /// 수신 데이터 구조 : [헤더][데이터값]
    /// data[0] : _receiveHeader
    /// data[1] : 수신 데이터1
    /// ...
    /// data[N] : 수신 데이터N
    /// </param>
    private void PrintReceiveData(Byte[] data)
    {
        Log($"header : {data[0]}");
        for (int i = 1; i < _receiveDataLength; ++i)
        {
            Log($"receive[{i}] : {data[i]}");
        }
    }

    #endregion


    #region 외부 호출 함수

    /// <summary>
    /// 시리얼 포트로 데이터 전송
    /// </summary>
    /// <param name="data">전송할 데이터
    /// 송신 데이터 구조 : [헤더][데이터값]
    /// sendBuffer[0] = _sendHeader;
    /// sendBuffer[1] = 송신 데이터1
    /// ....
    /// sendBuffer[N] = 송신 데이터N
    /// </param>
    public void SendData(Byte[] data)
    {
        // 포트 상태 확인
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            Log("Serial port is not open", ELogType.Warning);
            return;
        }

        try
        {
            // 송신용 데이터 버퍼 생성
            byte[] sendBuffer = new byte[_sendDataLength];
            sendBuffer[0] = _sendHeader;
            for(int i = 1; i< _sendDataLength; ++i)
            {
                sendBuffer[i] = data[i - 1];
            }

            // 시리얼 포트로 전송
            _serialPort.Write(sendBuffer, 0, _sendDataLength);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to send data: {e.Message}");
        }
    }

    #endregion


}
