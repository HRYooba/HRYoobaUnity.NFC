using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PCSC;
using PCSC.Monitoring;
using PCSC.Iso7816;
using NdefParser;
using R3;

namespace HRYooba.NFC
{
    public class NfcReader : IDisposable
    {
        private readonly int _deviceIndex;
        private ISCardContext _context;
        private ISCardMonitor _monitor;
        private SCRState _currentState = SCRState.Empty;
        private string _currentCardIDm = string.Empty;

        private readonly Subject<string> _onCardDetectedSubject = new();
        private readonly Subject<string> _onCardRemovedSubject = new();

        /// <summary>
        /// Name of the reader.
        /// </summary>
        public string ReaderName { get; private set; }

        /// <summary>
        /// カードがタッチされているかどうか
        /// </summary>
        public bool IsCardPresent { get; private set; }

        /// <summary>
        /// カードがタッチされたときのイベント
        /// </summary>
        public Observable<string> OnCardDetectedObservable => _onCardDetectedSubject;

        /// <summary>
        /// カードが離されたときのイベント
        /// </summary>
        public Observable<string> OnCardRemovedObservable => _onCardRemovedSubject;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceIndex"></param>
        public NfcReader(int deviceIndex = 0)
        {
            _deviceIndex = deviceIndex;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_context != null)
            {
                _context.Cancel();
                _context.Dispose();
            }

            if (_monitor != null)
            {
                _monitor.StatusChanged -= OnStatusChanged;
                _monitor.Cancel();
                _monitor.Dispose();
            }

            _onCardDetectedSubject.Dispose();
            _onCardRemovedSubject.Dispose();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            // Context
            var contextFactory = ContextFactory.Instance;
            _context = contextFactory.Establish(SCardScope.System);

            // ReaderName
            var readerNames = _context.GetReaders();
            if (readerNames.Length == 0)
            {
                throw new InvalidOperationException("[NfcReader] No readers found.");
            }
            if (_deviceIndex >= readerNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(_deviceIndex), _deviceIndex, $"[NfcReader] Device index must be less than {readerNames.Length}.");
            }
            ReaderName = readerNames[_deviceIndex];

            // Reader Monitor
            var monitorFactory = MonitorFactory.Instance;
            _monitor = monitorFactory.Create(SCardScope.System);
            _monitor.StatusChanged += OnStatusChanged;
            _monitor.Start(ReaderName);
        }

        /// <summary>
        /// Read IDm(UID)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ReadIDmAsync(CancellationToken cancellationToken)
        {
            while (!IsCardPresent)
            {
                await Task.Delay(100, cancellationToken);
            }

            return _currentCardIDm;
        }

        /// <summary>
        /// Read Binary data (NTAG 213 pageByte=4, pageCount=45)
        /// </summary>
        /// <param name="pageByte">byte/page</param>
        /// <param name="pageCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadBinaryAsync(int pageByte, int pageCount, CancellationToken cancellationToken)
        {
            while (!IsCardPresent)
            {
                await Task.Delay(100, cancellationToken);
            }

            var binary = ReadBinary(pageByte, pageCount);
            return binary;
        }

        /// <summary>
        /// Read URL (NTAG 213 pageByte=4, pageCount=45)
        /// </summary>
        /// <param name="pageByte">byte/page</param>
        /// <param name="pageCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ReadUrlAsync(int pageByte, int pageCount, CancellationToken cancellationToken)
        {
            while (!IsCardPresent)
            {
                await Task.Delay(100, cancellationToken);
            }

            var url = ReadUrl(pageByte, pageCount);
            return url;
        }

        /// <summary>
        /// Read TechnologyType
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TechnologyType> ReadTechnologyTypeAsync(CancellationToken cancellationToken)
        {
            while (!IsCardPresent)
            {
                await Task.Delay(100, cancellationToken);
            }

            var technologyType = ReadTechnologyType();
            return technologyType;
        }

        private string ReadIDm()
        {
            using var reader = _context.ConnectReader(ReaderName, SCardShareMode.Shared, SCardProtocol.Any);

            var apdu = new CommandApdu(IsoCase.Case2Short, reader.Protocol)
            {
                CLA = 0xFF,
                Instruction = InstructionCode.GetData,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0
            };

            var responseApdu = Transmit(reader, apdu);
            return responseApdu.HasData ? BitConverter.ToString(responseApdu.GetData()) : string.Empty;
        }

        private TechnologyType ReadTechnologyType()
        {
            using var reader = _context.ConnectReader(ReaderName, SCardShareMode.Shared, SCardProtocol.Any);

            var apdu = new CommandApdu(IsoCase.Case2Short, reader.Protocol)
            {
                CLA = 0xFF,
                Instruction = InstructionCode.GetData,
                P1 = 0xF3,
                P2 = 0x00,
                Le = 0
            };

            var responseApdu = Transmit(reader, apdu);
            if (!responseApdu.HasData)
            {
                return TechnologyType.None;
            }

            var data = responseApdu.GetData();
            var technologyType = (TechnologyType)data[0];
            return technologyType;
        }

        private byte[] ReadBinary(int pageByte, int pageCount)
        {
            var binary = new List<byte>();
            var loopCount = pageCount / pageByte + 1;
            var maxLength = pageByte * pageCount;

            using var reader = _context.ConnectReader(ReaderName, SCardShareMode.Shared, SCardProtocol.Any);

            for (var i = 0; i < loopCount; i++)
            {
                var apdu = new CommandApdu(IsoCase.Case2Short, reader.Protocol)
                {
                    CLA = 0xFF,
                    Instruction = InstructionCode.ReadBinary,
                    P1 = 0x00,
                    P2 = (byte)(i * pageByte),
                    Le = 0
                };

                var responseApdu = Transmit(reader, apdu);
                if (responseApdu.HasData)
                {
                    binary.AddRange(responseApdu.GetData());
                }
            }

            if (binary.Count > maxLength)
            {
                binary.RemoveRange(maxLength, binary.Count - maxLength);
            }

            return binary.ToArray();
        }

        private string ReadUrl(int pageByte, int pageCount)
        {
            var binary = ReadBinary(pageByte, pageCount);
            var ndefMessage = GetNDEFMessage(binary);
            var ndefTLV = new NdefTLV(ndefMessage);

            return ndefTLV.record.URI;
        }

        private ResponseApdu Transmit(ICardReader reader, CommandApdu apdu)
        {
            using (reader.Transaction(SCardReaderDisposition.Leave))
            {
                var sendPci = SCardPCI.GetPci(reader.Protocol);
                var receivePci = new SCardPCI(); // IO returned protocol control information.

                var receiveBuffer = new byte[256];
                var command = apdu.ToArray();

                var bytesReceived = reader.Transmit(
                    sendPci, // Protocol Control Information (T0, T1 or Raw)
                    command, // command APDU
                    command.Length,
                    receivePci, // returning Protocol Control Information
                    receiveBuffer,
                    receiveBuffer.Length); // data buffer

                var responseApdu = new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, reader.Protocol);
                return responseApdu;
            }
        }

        private byte[] GetNDEFMessage(byte[] binary)
        {
            // 先頭16byteはNFC Forum Type 2 Tagのヘッダ情報
            var buffer = new byte[binary.Length - 16];
            Array.Copy(binary, 16, buffer, 0, buffer.Length);

            // NDEF Messageの終端を検索
            var index = Array.LastIndexOf(buffer, (byte)TLVBlock.Terminator_TLV);
            if (index < 0)
            {
                return null;
            }

            // NDEF Messageの長さを取得
            var ndefMessageLength = index + 1;

            // NDEF Messageを取得
            var ndefMessage = new byte[ndefMessageLength];
            Array.Copy(buffer, 0, ndefMessage, 0, ndefMessageLength);

            return ndefMessage;
        }

        private void OnStatusChanged(object sender, StatusChangeEventArgs args)
        {
            var isInUse = (args.NewState & SCRState.InUse) == SCRState.InUse;
            if (isInUse) return;

            switch (args.NewState)
            {
                case SCRState.Empty:
                    if (_currentState == SCRState.Empty) return;

                    IsCardPresent = false;
                    _onCardRemovedSubject.OnNext(_currentCardIDm);
                    _currentCardIDm = string.Empty;
                    break;

                case SCRState.Present:
                    if (_currentState == SCRState.Present) return;

                    IsCardPresent = true;
                    _currentCardIDm = ReadIDm();
                    _onCardDetectedSubject.OnNext(_currentCardIDm);
                    break;
            }

            _currentState = args.NewState;
        }
    }
    
    /// <summary>
    /// TechnologyType
    /// </summary>
    public enum TechnologyType
    {
        None = 0x00,
        TypeA = 0x01, // ISO/IEC 14443A
        TypeB = 0x02, // ISO/IEC 14443B
        TypeF = 0x04 // JIS X 6319-4, FeliCa

        /* https://qiita.com/gpsnmeajp/items/d4810b175189609494ac
            CARD_TYPE_UNKNOWN    0x00
            CARD_TYPE_ISO14443A  0x01
            CARD_TYPE_ISO14443B  0x02
            CARD_TYPE_PICOPASSB  0x03
            CARD_TYPE_FELICA     0x04
            CARD_TYPE_NFC_TYPE_1 0x05
            CARD_TYPE_MIFARE_EC  0x06
            CARD_TYPE_ISO14443A_4A  0x07
            CARD_TYPE_ISO14443B_4B  0x08
            CARD_TYPE_TYPE_A_NFC_DEP  0x09
            CARD_TYPE_FELICA_NFC_DEP  0x0A
        */
    }
}