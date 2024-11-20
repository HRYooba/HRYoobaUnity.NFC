////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// SimpleNdefParser Class and Test
//
// This classes and Tests are released under by MIT License
//
// Copyright 2019 Office-Fun.com(M.Sonobe)
//
// https://opensource.org/licenses/mit-license.php
// 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Linq;

namespace NdefParser
{
    /// <summary>
    /// Ndef TLVをバイト配列から計算します or Ndef TLVから文字列データを取得します
    /// </summary>
    public class NdefTLV
    {
        private NfcType _nfcType;
        private TLVBlock _tlvType;
        private int _tlvLength;

        public NfcType NfcTYPE
        {
            get { return _nfcType; }
        }

        public TLVBlock TlvType
        {
            get { return _tlvType; }
        }

        public int TlvLength
        {
            get { return _tlvLength; }
        }

        /// <summary>
        /// 解析用 Ndef TLVコンストラクタ
        /// </summary>
        /// <param name="rawData"></param>
        public NdefTLV(byte[] rawData)
        {
            RawDataForTLV = rawData;

            // 配列チェック
            if (rawData == null || rawData.Length == 0)
            {
                ParseError = true;
                return;
            }


            byte lastByte = rawData[rawData.Length - 1];

            if ((byte)TLVBlock.Terminator_TLV != lastByte)
            {
                ParseError = true;
                return;
            }


            // TLV部分の配列を解釈する
            byte firstByte = rawData[0];

            switch (firstByte)
            {
                case (byte)TLVBlock.LockControlTLV:

                    {
                        _tlvType = TLVBlock.LockControlTLV;

                        byte secondByte = rawData[1];

                        //バイトオフセットの計算
                        byte v_first = rawData[2];

                        byte v_second = rawData[3];

                        byte v_third = rawData[4];


                        byte pageAddress = NdefUtility.GetBitRange_Upper4bit(v_first);

                        byte bytesOffset = NdefUtility.GetBitRange_Lower4bit(v_first);

                        int size = 0;

                        if (v_second != 0)
                        {
                            size = v_second + 1;
                        }
                        else
                        {
                            size = 256;
                        }


                        byte bytesPerPage = NdefUtility.GetBitRange_Lower4bit(v_third);

                        byte bytesLockedPerLockBit = NdefUtility.GetBitRange_Upper4bit(v_third);

                        byte byteAdders = (byte)((pageAddress * Math.Pow(2, bytesPerPage)) + bytesOffset);


                        byte ndefT = rawData[5];

                        //データの切り取り
                        int messageSize = rawData[6];

                        byte[] buf = new byte[messageSize];

                        Array.Copy(rawData, 7, buf, 0, messageSize);

                        record = new NdefRecord(buf);

                    }

                    break;

                case (byte)TLVBlock.NdefMessage_TLV:
                    {
                        _tlvType = TLVBlock.NdefMessage_TLV;

                        byte messageSize = rawData[1];

                        //データの切り取り
                        byte[] buf = new byte[messageSize];

                        Array.Copy(rawData, 2, buf, 0, messageSize);

                        record = new NdefRecord(buf);
                    }

                    break;

                default:
                    ParseError = true;
                    break;
            }

        }

        public NdefTLV(string uri,
                       UriIdentifierCode code,
                       TLVBlock tLVBlock = TLVBlock.NdefMessage_TLV)
        {
            record = new NdefRecord(uri ,code);

            if (record.RawData == null)
            {
                ParseError = true;
                return;
            }

            if (record.RawData.Length == 0)
            {
                ParseError = true;
                return;
            }

            try
            {
                byte[] buf = null;

                byte size = (byte)record.RawData.Length;

                switch (tLVBlock)
                {
                    case TLVBlock.LockControlTLV:

                        buf = new byte[] { (byte)TLVBlock.LockControlTLV,
                                        0x03,
                                        0xA0, 0xC0, 0x34,
                                        (byte)TLVBlock.NdefMessage_TLV,size};

                        break;

                    case TLVBlock.NdefMessage_TLV:

                        buf = new byte[] { (byte)TLVBlock.NdefMessage_TLV, size };

                        break;
                }

                if (buf == null)
                {
                    ParseError = true;
                    return;
                }

                RawDataForTLV = buf.Concat(record.RawData).ToArray();

                byte[] endbuf = new byte[] { (byte)TLVBlock.Terminator_TLV };

                RawDataForTLV = RawDataForTLV.Concat(endbuf).ToArray();


            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }

        public byte[] RawDataForTLV;

        public NdefRecord record;

        public string DataParsed;


        public bool ParseError = false;
    }
}