using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using HRYooba.NFC.Internal;

/*
参考: https://www.softech.co.jp/mm_240605_tr.htm
*/

namespace HRYooba.NFC
{
    public class NfcReader : IDisposable
    {
        // device
        private readonly int _deviceIndex = 0;
        private string _readerName = "";
        private IntPtr _hContext = IntPtr.Zero;
        private IntPtr _hCard = IntPtr.Zero;
        private IntPtr _activeProtocol = IntPtr.Zero;
        private NfcApi.SCARD_READERSTATE[] _readerStates;

        // logic
        private bool _isCardPresent = false;

        // thread
        private readonly object _lock = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();

        // event
        private readonly Subject<Unit> _onCardDetectedSubject = new();
        private readonly Subject<Unit> _onCardRemovedSubject = new();

        // property
        /// <summary>
        /// Device Name
        /// </summary>
        public string ReaderName => _readerName;

        /// <summary>
        /// カードがタッチされているか
        /// </summary>
        public bool IsCardPresent
        {
            get
            {
                lock (_lock)
                {
                    return _isCardPresent;
                }
            }
        }

        /// <summary>
        /// Detected Event
        /// </summary>
        public Observable<Unit> OnCardDetectedObservable => _onCardDetectedSubject.ObserveOnMainThread();

        /// <summary>
        /// Removed Event
        /// </summary>
        public Observable<Unit> OnCardRemovedObservable => _onCardRemovedSubject.ObserveOnMainThread();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <exception cref="Exception"></exception>
        public NfcReader(int deviceIndex = 0)
        {
            _deviceIndex = deviceIndex;

            if (!EstablishContext())
                throw new Exception("[NfcReader] Failed : Establishing Context");
            if (!SelectReader())
                throw new Exception("[NfcReader] Failed : Selecting Device");

            Task.Run(() => RunAsync(cancellationTokenSource.Token));
        }

        ~NfcReader()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!ReleaseContext())
                throw new Exception("[NfcReader] Failed : Releasing Context");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            _onCardDetectedSubject.Dispose();
            _onCardRemovedSubject.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!GetReaderStatus())
                        throw new Exception("[NfcReader] Failed : Waiting Status Change");

                    lock (_lock)
                    {
                        if ((_readerStates[_deviceIndex].dwEventState & NfcApi.SCARD_STATE_PRESENT) == NfcApi.SCARD_STATE_PRESENT)
                        {
                            if (!_isCardPresent)
                            {
                                _isCardPresent = true;
                                _onCardDetectedSubject.OnNext(Unit.Default);
                            }
                        }
                        else
                        {
                            if (_isCardPresent)
                            {
                                _isCardPresent = false;
                                _onCardRemovedSubject.OnNext(Unit.Default);
                            }
                        }
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// IDm(UID)を読み取る
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ReadIDmAsync(CancellationToken cancellationToken)
        {
            var (recvBuffer, recvLength) = await ReadAsync(APDUCommand.IDmCommand, 0, 255, 64, cancellationToken);
            return BitConverter.ToString(recvBuffer, 0, recvLength - 2);
        }

        /// <summary>
        /// 指定したコマンドを送信し、受信したデータを返す
        /// </summary>
        /// <param name="command"></param>
        /// <param name="dwProtocol"></param>
        /// <param name="cbPciLength"></param>
        /// <param name="maxRecvLength"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(byte[] RecvBuffer, int RecvLength)> ReadAsync(byte[] command, int dwProtocol, int cbPciLength, int maxRecvLength, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        lock (_lock)
                        {
                            if (_isCardPresent) break;
                        }
                    }

                    if (!ConnectCard())
                        throw new Exception("[NfcReader] Failed : Connecting NfcApi");
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException();

                    var ioRequest = new NfcApi.SCARD_IO_REQUEST { dwProtocol = dwProtocol, cbPciLength = cbPciLength };
                    if (!TransmitReadCommand(command, ioRequest, maxRecvLength, out byte[] recvBuffer, out int recvLength))
                        throw new Exception("[NfcReader] Failed : Transmitting Read Command");
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException();

                    if (!DisconnectCard())
                        throw new Exception("[NfcReader] Failed : Disconnecting NfcApi");
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException();

                    return (recvBuffer, recvLength);

                }
                catch (Exception e)
                {
                    throw e;
                }
            });
        }

        private bool EstablishContext()
        {
            // リソースマネージャに接続しハンドルを取得する
            var retCode = NfcApi.SCardEstablishContext(NfcApi.SCARD_SCOPE_SYSTEM, 0, 0, ref _hContext);

            if (retCode != NfcApi.SCARD_S_SUCCESS)
            {
                _hContext = IntPtr.Zero;
                return false;
            }

            return true;
        }

        private bool SelectReader()
        {
            // 使用可能なカードリーダーの数を得る
            int readerCount = 0;
            {
                uint retCode = NfcApi.SCardListReaders(_hContext, null, null, ref readerCount);

                if (retCode != NfcApi.SCARD_S_SUCCESS)
                {
                    return false;
                }
            }

            // カードリーダーの一覧を得る
            byte[] readerData = new byte[readerCount * 2];
            {
                uint retCode = NfcApi.SCardListReaders(_hContext, null, readerData, ref readerCount);

                if (retCode != NfcApi.SCARD_S_SUCCESS)
                {
                    return false;
                }
            }

            // カードリーダーの一覧のうち先頭のカードリーダーのみを取り出す
            string readersDataText = Encoding.Unicode.GetString(readerData);
            var readerNames = readersDataText.Split('\0').Where(name => !string.IsNullOrEmpty(name)).ToArray();
            _readerName = readerNames[_deviceIndex];

            // カードリーダーの状態の初期化を行う
            {
                _readerStates = new NfcApi.SCARD_READERSTATE[readerNames.Length];
                _readerStates[_deviceIndex].dwCurrentState = NfcApi.SCARD_STATE_UNAWARE;
                _readerStates[_deviceIndex].szReader = _readerName;
                uint retCode = NfcApi.SCardGetStatusChange(_hContext, 100, _readerStates, _readerStates.Length);

                if (retCode != NfcApi.SCARD_S_SUCCESS)
                {
                    return false;
                }
            }

            return true;
        }

        private bool GetReaderStatus()
        {
            uint retCode = NfcApi.SCardGetStatusChange(_hContext, 100, _readerStates, _readerStates.Length);
            return retCode == NfcApi.SCARD_S_SUCCESS;
        }

        public bool ConnectCard()
        {
            return ConnectCard(false);
        }

        private bool ConnectCard(bool isRetry)
        {
            uint retCode = NfcApi.SCardConnect(_hContext, _readerName, NfcApi.SCARD_SHARE_SHARED,
                NfcApi.SCARD_PROTOCOL_T0 | NfcApi.SCARD_PROTOCOL_T1, ref _hCard, ref _activeProtocol);

            // 接続に成功
            if (retCode == NfcApi.SCARD_S_SUCCESS)
            {
                return true;
            }
            // カードリーダーなし
            else if (retCode == NfcApi.SCARD_E_NO_SMARTCARD && !isRetry)
            {
                SelectReader();
                return ConnectCard(true);
            }
            else
            {
                return false;
            }
        }

        public bool DisconnectCard()
        {
            uint retCode = NfcApi.SCardDisconnect(_hCard, NfcApi.SCARD_E_CANT_DISPOSE);
            return retCode == NfcApi.SCARD_S_SUCCESS;
        }

        private bool ReleaseContext()
        {
            uint retCode = NfcApi.SCardReleaseContext(_hContext);
            return retCode == NfcApi.SCARD_S_SUCCESS;
        }

        public bool TransmitReadCommand(byte[] sendBuffer, NfcApi.SCARD_IO_REQUEST ioRequest, int maxRecvLength, out byte[] recvBuffer, out int recvLength)
        {
            recvBuffer = new byte[maxRecvLength];
            recvLength = recvBuffer.Length;

            // 読み取り実行
            {
                IntPtr SCARD_PCI_T1 = GetPciT1();
                uint retCode = NfcApi.SCardTransmit(_hCard, SCARD_PCI_T1, sendBuffer, sendBuffer.Length,
                    ref ioRequest, recvBuffer, ref recvLength);

                if (retCode != NfcApi.SCARD_S_SUCCESS)
                {
                    return false;
                }
            }

            return true;
        }

        private IntPtr GetPciT1()
        {
            IntPtr handle = NfcApi.LoadLibrary("Winscard.dll");
            IntPtr pci = NfcApi.GetProcAddress(handle, "g_rgSCardT1Pci");
            NfcApi.FreeLibrary(handle);
            return pci;
        }
    }
}