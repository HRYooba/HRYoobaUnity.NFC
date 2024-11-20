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

namespace NdefParser
{

    /// <summary>
    /// Ndef Recordをバイト配列から計算します or Ndef Recordから文字列データを取得します
    /// </summary>
    public class NdefRecord
    {

        private bool _messageBegin = false;
        private bool _messageEnd = false;
        private bool _chunkFlag = false;
        private bool _shortRecord = false;
        private TNF _typeNameFormat = TNF.Empty;
        private byte _typeLength;
        private byte[] _type;
        private long _payLoadLength;
        private long _payLoadLengthMax = 256;
        private long _id = 1;
        private bool _idLength = false;

        private WKT_RTD _recordTypeDefinition = WKT_RTD.None;

        private byte[] _payLoad;

        private byte[] _rawData = null;

        private bool _initialize = false;

        private bool _implement = true;

        private string _uri = string.Empty;

        /// <summary>
        /// 解析用コンストラクタ
        /// </summary>
        /// <param name="rawData"></param>
        public NdefRecord(byte[] rawData)
        {
            _rawData = rawData;

            try
            {
                // ヘッダー部分のデータ取得
                byte firstByte = _rawData[0];

                _messageBegin = NdefUtility.GetBit(firstByte, (byte)MessageRecordHearder.BitPos_MessageBegin_MB);
                _messageEnd = NdefUtility.GetBit(firstByte, (byte)MessageRecordHearder.BitPos_MessageEnd_ME);
                _chunkFlag = NdefUtility.GetBit(firstByte, (byte)MessageRecordHearder.BitPos_ChunkFlag_CF);
                _shortRecord = NdefUtility.GetBit(firstByte, (byte)MessageRecordHearder.BitPos_ShortRecord_SR);
                _idLength = NdefUtility.GetBit(firstByte, (byte)MessageRecordHearder.BitPos_IdLength_IL);

                _typeNameFormat = (TNF)NdefUtility.GetBitRange_From_2_0(firstByte);

                if (!_shortRecord)
                {
                    _implement = false;
                    return;
                }

                // タイプとデータ長の取得（ペイロード長の取得）
                _typeLength = _rawData[1];
                _payLoadLength = _rawData[2];


                // データタイプの取得
                switch (_rawData[3])
                {
                    case (byte)WKT_RTD.TEXT:
                        _recordTypeDefinition = WKT_RTD.TEXT;
                        break;
                    case (byte)WKT_RTD.URI:
                        _recordTypeDefinition = WKT_RTD.URI;
                        break;
                    case (byte)WKT_RTD.SmartPoster1:
                        _recordTypeDefinition = WKT_RTD.SmartPoster1;
                        break;
                    case (byte)WKT_RTD.SmartPoster2:
                        _recordTypeDefinition = WKT_RTD.SmartPoster2;
                        break;
                }

                // データのコピー
                if (_payLoadLength == _rawData.Length - 4)
                {
                    _payLoad = new byte[_payLoadLength];

                    for (int i = 0; i < _payLoad.Length; ++i)
                    {
                        _payLoad[i] = _rawData[i + 4];
                    }
                }
                else
                {
                    _implement = false;
                    return;
                }

                // データの変換
                if (WKT_RTD.URI == _recordTypeDefinition)
                {
                    _uri = NdefUtility.GetUriFieldString(_payLoad);
                }
                else
                {
                    _implement = false;
                    return;
                }


                _initialize = true;

            }
            catch (Exception ex)
            {
                string str = ex.Message;
                _initialize = false;
            }
        }

        /// <summary>
        /// バイト配列作成用コンストラクタ 
        /// </summary>
        /// <param name="uri">uriにはなるべくプロトコルをいれておくこと</param>
        /// <param name="includeProtocolStr"></param>
        /// <param name="uriIdentifierCode"></param>
        /// <param name="wkt"></param>
        public NdefRecord(string uri,
                           UriIdentifierCode uriIdentifierCode = UriIdentifierCode.UnAbridgedURI,
                           WKT_RTD wkt = WKT_RTD.URI)
        {
            try
            {
                _messageBegin = true;
                _messageEnd = true;
                _chunkFlag = false;
                _shortRecord = true;

                _typeLength = 1;

                _typeNameFormat = TNF.NfcForumWellKnownType;


                _recordTypeDefinition = wkt;


                string strWithoutIdentifiercode = string.Empty;
                string bufPayload = string.Empty;


                bool includeProtocol = false;

                if (uriIdentifierCode != UriIdentifierCode.UnAbridgedURI)
                {
                    includeProtocol = NdefUtility.IsIncludedProtorol(uriIdentifierCode);
                }


                if (includeProtocol)
                {
                    // uriの先頭から合致するIdentifierタイプを取得します。
                    uriIdentifierCode = NdefUtility.GetUriIdentifierCode(uri);

                    // プロトコルを削除します。
                    int remLen = NdefUtility.GetUriIdentifierCodeString((byte)uriIdentifierCode).Length;

                    strWithoutIdentifiercode = uri.Remove(0, remLen);

                    _payLoadLength = strWithoutIdentifiercode.Length + 1;

                    bufPayload = strWithoutIdentifiercode;
                }
                else
                {
                    _payLoadLength = uri.Length + 1;

                    bufPayload = uri;
                }

                _payLoad = new byte[_payLoadLength];


                // 出力データに項目を設定していきます
                _rawData = new byte[_payLoadLength + 4];


                _rawData[0] = SetHearderByte();
                _rawData[1] = _typeLength;
                _rawData[2] = (byte)_payLoadLength;
                _rawData[3] = (byte)_recordTypeDefinition;

                // payLoadを書き込みます
                _payLoad[0] = (byte)uriIdentifierCode;

                byte[] buf = System.Text.Encoding.UTF8.GetBytes(bufPayload);

                for (int i = 0; i < buf.Length; ++i)
                {
                    _payLoad[i + 1] = buf[i];
                }

                for (int i = 0; i < _payLoadLength; ++i)
                {
                    _rawData[i + 4] = _payLoad[i];
                }

            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }

        private byte SetHearderByte()
        {
            byte result = 0x00;

            if (_messageBegin)
            {
                result = NdefUtility.SetBit(result, (byte)MessageRecordHearder.BitPos_MessageBegin_MB);
            }

            if (_messageEnd)
            {
                result = NdefUtility.SetBit(result, (byte)MessageRecordHearder.BitPos_MessageEnd_ME);
            }

            if (_chunkFlag)
            {
                result = NdefUtility.SetBit(result, (byte)MessageRecordHearder.BitPos_ChunkFlag_CF);
            }

            if (_shortRecord)
            {
                result = NdefUtility.SetBit(result, (byte)MessageRecordHearder.BitPos_ShortRecord_SR);
            }

            result = NdefUtility.SetBitRangeFrom_2_0(result, (byte)_typeNameFormat);


            return result;
        }

        /// <summary>
        /// 初期化成功失敗
        /// </summary>
        public bool Initialize
        {
            get { return _initialize; }
        }

        public bool MessageBegin_MB
        {
            get { return _messageBegin; }
        }

        public bool MessageEnd_ME
        {
            get { return _messageEnd; }
        }

        public bool ChunkFlag_CF
        {
            get { return _chunkFlag; }
        }

        public bool ShortRecord_SR
        {
            get { return _shortRecord; }
        }

        public bool IdLength_IL
        {
            get { return _idLength; }
        }

        public TNF TypeNameFormat_TNF
        {
            get { return _typeNameFormat; }
        }

        public byte TypeLength
        {
            get { return _typeLength; }
        }

        public WKT_RTD RecordTypeDefinition
        {
            get { return _recordTypeDefinition; }
        }


        public long PayLoadLength
        {
            get { return _payLoadLength; }
        }

        public long PayLoadLengthMax
        {
            get { return _payLoadLengthMax; }
        }

        public long ID
        {
            get { return _id; }
        }


        public byte[] Type
        {
            get { return _type; }
        }


        public byte[] PayLoad
        {
            get { return _payLoad; }
        }

        public string URI
        {
            get { return _uri; }
        }

        public byte[] RawData
        {
            get { return _rawData; }
        }

    }


}
