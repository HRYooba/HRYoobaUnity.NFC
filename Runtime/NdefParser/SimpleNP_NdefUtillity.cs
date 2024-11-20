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
using System.Collections.Generic;

namespace NdefParser
{
    // Utility関数と定数

    /// <summary>
    /// NDEFの解析等に利用します
    /// </summary>
    public static class NdefUtility
    {

        /// <summary>
        /// 指定したビットが立っているかを取得
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bitIndex"></param>
        /// <returns></returns>
        public static bool GetBit(byte data, byte bitIndex)
        {
            byte baseBit = 0;

            bool bResult = false;

            switch (bitIndex)
            {
                case 0:
                    baseBit = 0x01;
                    break;

                case 1:
                    baseBit = 0x02;
                    break;

                case 2:
                    baseBit = 0x04;
                    break;

                case 3:
                    baseBit = 0x08;
                    break;

                case 4:
                    baseBit = 0x10;
                    break;

                case 5:
                    baseBit = 0x20;
                    break;

                case 6:
                    baseBit = 0x40;
                    break;

                case 7:
                    baseBit = 0x80;
                    break;
            }

            byte bitResult = (byte)(data & baseBit);

            if (bitResult == baseBit)
            {
                bResult = true;
            }


            return bResult;
        }

        /// <summary>
        /// 下位0,1,2ビットの表す数字を結果として取り出す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte GetBitRange_From_2_0(byte data)
        {
            byte result = 0;

            if (GetBit(data, 0)) result += 0x01;

            if (GetBit(data, 1)) result += 0x02;

            if (GetBit(data, 2)) result += 0x04;

            return result;
        }

        /// <summary>
        /// 下位4bitを取り出し 0x00-0x0Fで出力します。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte GetBitRange_Lower4bit(byte data)
        {
            byte result = 0;

            if (GetBit(data, 0)) result += 0x01;

            if (GetBit(data, 1)) result += 0x02;

            if (GetBit(data, 2)) result += 0x04;

            if (GetBit(data, 3)) result += 0x08;


            return result;
        }

        /// <summary>
        /// 上位4bitを取り出し 0x00-0x0Fで出力します。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte GetBitRange_Upper4bit(byte data)
        {
            byte result = 0;

            if (GetBit(data, 4)) result += 0x01;

            if (GetBit(data, 5)) result += 0x02;

            if (GetBit(data, 6)) result += 0x04;

            if (GetBit(data, 7)) result += 0x08;

            return result;
        }

        /// <summary>
        /// 指定したビットを立てます
        /// </summary>
        /// <param name="input"></param>
        /// <param name="bitIndex"></param>
        /// <returns></returns>
        public static byte SetBit(byte input, byte bitIndex)
        {
            byte baseBit = 0;

            switch (bitIndex)
            {
                case 0:
                    baseBit = 0x01;
                    break;

                case 1:
                    baseBit = 0x02;
                    break;

                case 2:
                    baseBit = 0x04;
                    break;

                case 3:
                    baseBit = 0x08;
                    break;

                case 4:
                    baseBit = 0x10;
                    break;

                case 5:
                    baseBit = 0x20;
                    break;

                case 6:
                    baseBit = 0x40;
                    break;

                case 7:
                    baseBit = 0x80;
                    break;
            }

            input = (byte)(input | baseBit);

            return input;
        }

        /// <summary>
        /// 指定した値の0,1,2ビットの数字を入力値に対して設定します
        /// </summary>
        /// <param name="data"></param>
        /// <param name="setValue"></param>
        /// <returns></returns>
        public static byte SetBitRangeFrom_2_0(byte data, byte setValue)
        {
            if (GetBit(setValue, 0)) data = SetBit(data, 0);

            if (GetBit(setValue, 1)) data = SetBit(data, 1);

            if (GetBit(setValue, 2)) data = SetBit(data, 2);

            return data;
        }

        /// <summary>
        /// byte配列の URI を文字列化します
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetUriFieldString(byte[] data)
        {
            if (data == null) return string.Empty;
            if (data.Length == 0) return string.Empty;

            string result = string.Empty;


            try
            {

                // データの型を取得
                byte firstByte = data[0];

                string uri = GetUriIdentifierCodeString(firstByte);

                byte[] dataTmp = new byte[data.Length - 1];

                for (int i = 1; i < data.Length; ++i)
                {
                    dataTmp[i - 1] = data[i];
                }

                //https://www.ipentec.com/document/csharp-bytearray-to-string
                //UTF エンコード
                string text = System.Text.Encoding.UTF8.GetString(dataTmp);

                result = uri + text;
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }


            return result;
        }

        /// <summary>
        /// Identifireに対応した文字列を取得する
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static string GetUriIdentifierCodeString(byte identifier)
        {
            UriIdentifierCode code = (UriIdentifierCode)identifier;

            string result = string.Empty;

            switch (code)
            {
                case UriIdentifierCode.UnAbridgedURI:
                    result = string.Empty;
                    break;
                case UriIdentifierCode.HttpWWW:
                    result = @"http://www.";
                    break;
                case UriIdentifierCode.HttpsWWW:
                    result = @"https://www.";
                    break;
                case UriIdentifierCode.Http:
                    result = @"http://";
                    break;
                case UriIdentifierCode.Https:
                    result = @"https://";
                    break;
                case UriIdentifierCode.Tel:
                    result = "tel:";
                    break;
                case UriIdentifierCode.MailTo:
                    result = "mailto:";
                    break;
                case UriIdentifierCode.FtpAnonymous:
                    result = @"ftp://anonymous:anonymous@";
                    break;
                case UriIdentifierCode.FtpFtp:
                    result = @"ftp://ftp.";
                    break;
                case UriIdentifierCode.Fpts:
                    result = @"ftps://";
                    break;
                case UriIdentifierCode.Sftp:
                    result = @"sfp://";
                    break;
                case UriIdentifierCode.Smb:
                    result = @"smb://";
                    break;
                case UriIdentifierCode.Nfs:
                    result = @"nfs://";
                    break;
                case UriIdentifierCode.Ftp:
                    result = @"ftp://";
                    break;
                case UriIdentifierCode.Dav:
                    result = @"dav://";
                    break;
                case UriIdentifierCode.News:
                    result = @"news://";
                    break;
                case UriIdentifierCode.Telnet:
                    result = @"telnet://";
                    break;
                case UriIdentifierCode.Imap:
                    result = @"imap:";
                    break;
                case UriIdentifierCode.Rtsp:
                    result = @"rstp://";
                    break;
                case UriIdentifierCode.Urn:
                    result = "urn:";
                    break;
                case UriIdentifierCode.Pop:
                    result = "pop:";
                    break;
                case UriIdentifierCode.Sip:
                    result = "sip:";
                    break;
                case UriIdentifierCode.Sips:
                    result = "sips";
                    break;
                case UriIdentifierCode.Tftp:
                    result = "tftp:";
                    break;
                case UriIdentifierCode.Btspp:
                    result = @"btspp://";
                    break;
                case UriIdentifierCode.Btl2Cap:
                    result = @"btl2cap://";
                    break;
                case UriIdentifierCode.Btgoep:
                    result = @"btgoep://";
                    break;
                case UriIdentifierCode.Tcpobex:
                    result = @"tcpobex:// ";
                    break;
                case UriIdentifierCode.Irdaobex:
                    result = @"irdaobex://";
                    break;
                case UriIdentifierCode.File:
                    result = @"file://";
                    break;
                case UriIdentifierCode.Urn_epc_id:
                    result = "urn:epc:id:";
                    break;
                case UriIdentifierCode.Urn_epc_tag:
                    result = "urn:epc:tag:";
                    break;
                case UriIdentifierCode.Urn_epc_pat:
                    result = "urn:epc:pat:";
                    break;
                case UriIdentifierCode.Urn_epc_raw:
                    result = "urn:epc:raw:";
                    break;
                case UriIdentifierCode.Urn_epc:
                    result = "urn:epc:";
                    break;
                case UriIdentifierCode.Urn_nfc:
                    result = "urn:nfc:";
                    break;

                default:
                    if ((UriIdentifierCode.RFU_Start <= code) && (code <= UriIdentifierCode.RFU_End))
                    {
                        result = "RFU";
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 指定したURIの先頭にどのIdentifierコードが含まれるかを返す（部分一致ではなく、長いものの一致をみる）
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static UriIdentifierCode GetUriIdentifierCode(string uri)
        {
            UriIdentifierCode result = UriIdentifierCode.UnAbridgedURI;

            try
            {
                byte[] search = System.Text.Encoding.UTF8.GetBytes(uri);

                List<UriIdentifierCode> candidate = new List<UriIdentifierCode>();

                foreach (UriIdentifierCode value in Enum.GetValues(typeof(UriIdentifierCode)))
                {
                    byte[] identifier = GetUriIdentifierBuffer((byte)value);

                    int identifierCount = 0;


                    //ゼロ配列は何もしない
                    if (identifier.Length == 0 || search.Length == 0)
                    {
                        continue;
                    }

                    //配列の小さい方でサーチする
                    int searchRange = 0;


                    //検索対象のほうが小さい場合は、検索しない
                    if (search.Length < identifier.Length)
                    {
                        continue;
                    }
                    else
                    {
                        //検索対象が大きい場合のみ、検索を行うものとする
                        searchRange = identifier.Length;
                    }

                    //検査対象とIdentifierの文字列が合致するものを探す
                    for (int i = 0; i < searchRange; ++i)
                    {
                        if (search[i] == identifier[i])
                        {
                            ++identifierCount;
                        }
                    }

                    //合致したものがあったら抜ける
                    if (identifierCount == identifier.Length)
                    {
                        candidate.Add(value);
                    }
                }

                //結果リストのサイズで決勝を行う
                if (candidate.Count == 1)
                {
                    result = candidate[0];
                }
                else if (candidate.Count == 0)
                {
                    return UriIdentifierCode.UnAbridgedURI;
                }
                else
                {
                    int maxLength = 0;
                    UriIdentifierCode maxIdentifier = UriIdentifierCode.UnAbridgedURI;

                    //よりサイズの大きいものを一致したものとみなす
                    foreach (UriIdentifierCode code in candidate)
                    {
                        string candidateStr = GetUriIdentifierCodeString((byte)code);

                        int strLength = candidateStr.Length;

                        if (strLength > maxLength)
                        {
                            maxLength = strLength;
                            maxIdentifier = code;
                        }
                    }

                    result = maxIdentifier;
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 指定したByteに対応するByte配列を取得します
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static byte[] GetUriIdentifierBuffer(byte identifier)
        {
            byte[] result = null;

            try
            {
                string str = GetUriIdentifierCodeString(identifier);

                result = System.Text.Encoding.UTF8.GetBytes(str);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                result = null;
            }

            return result;
        }

        /// <summary>
        /// プロトコルのなかにプロトコル名が含まれているか
        /// </summary>
        /// <param name="uriIdentifier"></param>
        /// <returns></returns>
        public static bool IsIncludedProtorol(UriIdentifierCode uriIdentifier)
        {
            bool result = false;

            switch (uriIdentifier)
            {
                case UriIdentifierCode.UnAbridgedURI:
                case UriIdentifierCode.Tel:
                case UriIdentifierCode.MailTo:
                case UriIdentifierCode.RFU_Start:
                case UriIdentifierCode.RFU_End:
                    result = false;
                    break;

                case UriIdentifierCode.HttpWWW:
                case UriIdentifierCode.HttpsWWW:
                case UriIdentifierCode.Http:
                case UriIdentifierCode.Https:
                case UriIdentifierCode.FtpAnonymous:
                case UriIdentifierCode.FtpFtp:
                case UriIdentifierCode.Fpts:
                case UriIdentifierCode.Sftp:
                case UriIdentifierCode.Smb:
                case UriIdentifierCode.Nfs:
                case UriIdentifierCode.Ftp:
                case UriIdentifierCode.Dav:
                case UriIdentifierCode.News:
                case UriIdentifierCode.Telnet:
                case UriIdentifierCode.Imap:
                case UriIdentifierCode.Rtsp:
                case UriIdentifierCode.Urn:
                case UriIdentifierCode.Pop:
                case UriIdentifierCode.Sip:
                case UriIdentifierCode.Sips:
                case UriIdentifierCode.Tftp:
                case UriIdentifierCode.Btspp:
                case UriIdentifierCode.Btl2Cap:
                case UriIdentifierCode.Btgoep:
                case UriIdentifierCode.Tcpobex:
                case UriIdentifierCode.Irdaobex:
                case UriIdentifierCode.File:
                case UriIdentifierCode.Urn_epc_id:
                case UriIdentifierCode.Urn_epc_tag:
                case UriIdentifierCode.Urn_epc_pat:
                case UriIdentifierCode.Urn_epc_raw:
                case UriIdentifierCode.Urn_epc:
                case UriIdentifierCode.Urn_nfc:
                    result = true;
                    break;
            }


            return result;
        }
    }

    /// <summary>
    /// NFCのタイプ
    /// </summary>
    public enum NfcType
    {
        Type_None = 0,

        Type1_Tag = 1,

        Type2_Tag = 2,
    }

    /// <summary>
    /// Table 2: Defined TLV blocks 参照　(Type2 仕様書参考)
    /// </summary>
    public enum TLVBlock
    {
        NullTLV          = 0x00,

        LockControlTLV   = 0x01,

        MemoryControlTLV = 0x02,

        NdefMessage_TLV  = 0x03,

        Proprietary_TLV  = 0xFD,

        Terminator_TLV   = 0xFE,
    }

    /// <summary>
    /// NDF仕様書 3.2 Record Layout (3.2.1-3.2.6 参照)
    /// </summary>
    public enum MessageRecordHearder
    {
        BitPos_MessageBegin_MB = 7,
        BitPos_MessageEnd_ME   = 6,
        BitPos_ChunkFlag_CF    = 5,
        BitPos_ShortRecord_SR  = 4,
        BitPos_IdLength_IL     = 3,
        BitPos_TNF_Start       = 2,
        BitPos_TNF_Middle      = 1,
        BitPos_TNE_End         = 0
    }

    /// <summary>
    /// NDF仕様書 3.2.6 TNF (Type Name Format) 参照
    /// </summary>
    public enum TNF
    {
        Empty                       = 0,
        NfcForumWellKnownType       = 1,
        MediaTypeAsDefinedRFC2046   = 2,
        AbsoluteUriAsDefinedRFC3986 = 3,
        NfcForumExternalType        = 4,
        Unknown                     = 5,
        Unchanged                   = 6,
        Reserved                    = 7,
    }

    /// <summary>
    /// Well-Known-Type (Record Type Definition)
    ///  http://y-anz-m.blogspot.com/2011/01/ndef.html
    /// </summary>
    public enum WKT_RTD
    {
        None = 0x00,

        TEXT = 0x54,

        /// <summary>
        /// URI,URL
        /// </summary>
        URI = 0x55,

        SmartPoster1 = 0x53,

        SmartPoster2 = 0x70,
    }


    /// <summary>
    /// NDF仕様書 3.2.2 URI Identifier Code 参照
    /// </summary>         
    public enum UriIdentifierCode
    {
        UnAbridgedURI = 0,
        HttpWWW       = 1,
        HttpsWWW      = 2,
        Http          = 3,
        Https         = 4,
        Tel           = 5,
        MailTo        = 6,
        FtpAnonymous  = 7,
        FtpFtp        = 8,
        Fpts          = 9,
        Sftp          = 10,
        Smb           = 11,
        Nfs           = 12,
        Ftp           = 13,
        Dav           = 14,
        News          = 15,
        Telnet        = 16,
        Imap          = 17,
        Rtsp          = 18,
        Urn           = 19,
        Pop           = 20,
        Sip           = 21,
        Sips          = 22,
        Tftp          = 23,
        Btspp         = 24,
        Btl2Cap       = 25,
        Btgoep        = 26,
        Tcpobex       = 27,
        Irdaobex      = 28,
        File          = 29,
        Urn_epc_id    = 30,
        Urn_epc_tag   = 31,
        Urn_epc_pat   = 32,
        Urn_epc_raw   = 33,
        Urn_epc       = 34,
        Urn_nfc       = 35, 
        RFU_Start     = 36,
        RFU_End       = 255,

    }

}
