using System;
using System.Runtime.InteropServices;

/*
参考: https://www.softech.co.jp/mm_240605_tr.htm
*/

namespace HRYooba.NFC.Internal
{
    static public class NfcApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SCARD_IO_REQUEST
        {
            public int dwProtocol;
            public int cbPciLength;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SCARD_READERSTATE
        {
            public string szReader;
            public IntPtr pvUserData;
            public UInt32 dwCurrentState;
            public UInt32 dwEventState;
            public UInt32 cbAtr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] rgbAtr;
        }

        [DllImport("winscard.dll")]
        public static extern uint SCardEstablishContext(int dwScope, int pvReserved1, int pvReserved2, ref IntPtr phContext);

        [DllImport("winscard.dll")]
        public static extern uint SCardReleaseContext(IntPtr hContext);

        [DllImport("winscard.dll", EntryPoint = "SCardListReadersW", CharSet = CharSet.Unicode)]
        public static extern uint SCardListReaders(IntPtr hContext, byte[] mszGroups, byte[] mszReaders, ref int pcchReaders);

        [DllImport("winscard.dll", EntryPoint = "SCardGetStatusChangeW", CharSet = CharSet.Unicode)]
        public static extern uint SCardGetStatusChange(IntPtr hContext, int dwTimeout, [In, Out] SCARD_READERSTATE[] rgReaderStates, int cReaders);

        [DllImport("winscard.dll", EntryPoint = "SCardConnectW", CharSet = CharSet.Unicode)]
        public static extern uint SCardConnect(IntPtr hContext, string szReader, int dwShareMode, int dwPreferredProtocols, ref IntPtr phCard, ref IntPtr pdwActiveProtocol);

        [DllImport("winscard.dll")]
        public static extern uint SCardDisconnect(IntPtr hCard, uint dwDisposition);

        [DllImport("winscard.dll")]
        public static extern uint SCardTransmit(IntPtr hCard, IntPtr pioSendRequest, byte[] SendBuff, int SendBuffLen, ref SCARD_IO_REQUEST pioRecvRequest,
                byte[] RecvBuff, ref int RecvBuffLen);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        public static extern void FreeLibrary(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr handle, string procName);

        //--------------------------------------------------------------------------
        // 定数
        //--------------------------------------------------------------------------
        public const uint SCARD_S_SUCCESS = 0;

        public const uint SCARD_E_NO_SMARTCARD = 0x8010000C;
        public const uint SCARD_E_CANT_DISPOSE = 0x8010000E;

        public const int SCARD_SCOPE_USER = 0;
        public const int SCARD_SCOPE_TERMINAL = 1;
        public const int SCARD_SCOPE_SYSTEM = 2;

        public const int SCARD_STATE_UNAWARE = 0x00;
        public const int SCARD_STATE_IGNORE = 0x01;
        public const int SCARD_STATE_CHANGED = 0x02;
        public const int SCARD_STATE_UNKNOWN = 0x04;
        public const int SCARD_STATE_UNAVAILABLE = 0x08;
        public const int SCARD_STATE_EMPTY = 0x10;
        public const int SCARD_STATE_PRESENT = 0x20;
        public const int SCARD_STATE_ATRMATCH = 0x40;
        public const int SCARD_STATE_EXCLUSIVE = 0x80;
        public const int SCARD_STATE_INUSE = 0x100;
        public const int SCARD_STATE_MUTE = 0x200;
        public const int SCARD_STATE_UNPOWERED = 0x400;

        public const int SCARD_SHARE_EXCLUSIVE = 1;
        public const int SCARD_SHARE_SHARED = 2;
        public const int SCARD_SHARE_DIRECT = 3;

        public const int SCARD_PROTOCOL_UNDEFINED = 0x00;
        public const int SCARD_PROTOCOL_T0 = 0x01;
        public const int SCARD_PROTOCOL_T1 = 0x02;
        public const int SCARD_PROTOCOL_RAW = 0x10000;
    }
}