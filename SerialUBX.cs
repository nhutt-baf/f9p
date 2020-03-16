using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.InteropServices;

namespace UBXTestings
{
    // MAIN Ublox Serial Class
    //
    //
    class SerialUBX : IDisposable
    {
        // UBX Message Types
        public const byte UBXNAVRELPOSNED = 0x3C;
        public const byte UBXNAVPVT = 0x07;

        // UBX Check Bytes
        public const byte UBXPREAMB1 = 0xB5;
        public const byte UBXPREAMB2 = 0x62;
        public const byte UBXNAVCLASS = 0x01;

        public event EventHandler<UBXReceived> NewUbxReceived;
        private void RaiseNewMessageReceived(UBXMSG umsg)
        {
            var handler = NewUbxReceived;
            if (handler == null) return;
            handler(this, new UBXReceived(umsg));
        }

        public SerialPort port;
        static ManualResetEvent terminateservice = new ManualResetEvent(false);
        static readonly object eventLock = new object();

        public SerialUBX(SerialPort serial)
        {
            port = serial;
            try
            {
                port.DataReceived += Port_DataReceived;
                port.ErrorReceived += Port_ErrorReceived;
                port.Open();
                

            } catch (Exception ex)
            {
                if (port.IsOpen) port.Close();
                
            }
           
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            lock (eventLock)
            {
                MessageBox.Show("ERROR ERROR");
                switch (e.EventType)
                {
                    case SerialError.TXFull:
                        break;
                    case SerialError.Overrun:
                        break;

                }
            }
            
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (eventLock)
            {
                byte[] buffer = new byte[1024];
                switch (e.EventType)
                {
                    case SerialData.Chars:
                        var tport = (SerialPort)sender;
                        int bytestoread = tport.BytesToRead;
                        if (bytestoread != buffer.Length)
                            Array.Resize(ref buffer, bytestoread);
                        tport.Read(buffer, 0, bytestoread);
                        ProcessBytes(buffer);
                        break;
                }
            }
        }

        public bool chksum(ref byte incoming)
        {

            return true;
        }

        private void ProcessBytes(byte[] msg)
        {

            if (msg[0] == UBXPREAMB1 && msg[1] == UBXPREAMB2)
            {
                switch (msg[3])
                {
                    case UBXNAVPVT:
                        List<byte> t = new List<byte>();
                        t.AddRange(msg.ToArray());
                        RaiseNewMessageReceived(new UBXNAVPVT(t.GetRange(0,100).ToArray<byte>()));
                        if(msg.Length > 100)
                        {                          
                            ProcessBytes(t.GetRange(100, t.Count<byte>()-100).ToArray<byte>()); 
                        }
                        break;
                    case UBXNAVRELPOSNED:
                        RaiseNewMessageReceived(new UBXNAVREPRO(msg));
                        break;
                }

            }

        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SerialUBX() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    // UBX-NAV-PVT - 2 Pre Am, 1 Byte Class, 1 Header, 2 Length, 92 PayLoad, 1 CHKA, 1 CHKB (TOTAL 100)
    //
    //
    public class UBXNAVPVT : UBXMSG
    {
        #region public variables
        public double lat {get; set;}
        public double lon { get; set; }
        public double elev { get; set; }
        public fBitFlag fixStatus;
        public double hMSL { get; set; }
        public double hAcc { get; set; }
        public double vAcc { get; set; }
        public double velN { get; set; }
        public double velE { get; set; }
        public double velD { get; set; }
        public double gSpeed { get; set; }
        public double headMot { get; set; }
        public double sAcc { get; set; }
        public double headAcc { get; set; }
        public double pDOP { get; set; }

        public int gnssFixOk { get => fixStatus.gnssFixOk; }
        public int diffSoln { get => fixStatus.diffSoln; }
        public int psmState { get => fixStatus.psmState; }
        public int headVehValid { get => fixStatus.headVehValid; }
        public int carSoln { get => fixStatus.carSoln; } 

        public string typefix { get { string temp; fixtypes.TryGetValue(fixtype, out temp); return temp; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct fBitFlag
        {
            public ushort BitField;

            public byte gnssFixOk
            {
                get { return (byte)((BitField >> 0) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 0) | (value << 0));
                }
            }

            public byte diffSoln
            {
                get { return (byte)((BitField >> 1) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 1) | (value << 1));
                }
            }

            public byte psmState
            {
                get { return (byte)((BitField >> 4) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 4) | (value << 4));
                }
            }

            public byte headVehValid
            {
                get { return (byte)((BitField >> 5) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 5) | (value << 5));
                }
            }

            public byte carSoln
            {
                get { return (byte)((BitField >> 6) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 6) | (value << 6));
                }
            }
        }
               
        private byte[] rawmsg { get { return _rawmsg; } set {
                _rawmsg = value;

                lat = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 24 + offset)) * Math.Pow(10, -7);
                lon = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 28 + offset)) * Math.Pow(10, -7);
                elev = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 32 + offset)) / 1000;
                fixtype = Convert.ToInt16(_rawmsg[20 + 6]);
                fixStatus = new fBitFlag(); fixStatus.BitField = _rawmsg[21 + 6];
                hMSL = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 36 + offset)) / 1000;
                hAcc = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 40 + offset)) / 1000;
                vAcc = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 44 + offset)) / 1000;
                velN = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 48 + offset)) / 1000;
                velE = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 52 + offset)) / 1000;
                velD = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 56 + offset)) / 1000;
                gSpeed = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 60 + offset)) / 1000;
                headMot = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 64 + offset)) * Math.Pow(10, -5);
                sAcc = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 68 + offset)) / 1000;
                headAcc = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 72 + offset)) * Math.Pow(10, -5);
                pDOP = Convert.ToDouble(BitConverter.ToUInt16(_rawmsg, 78 + offset));


            } }
                
        private byte[] _rawmsg;
        #endregion

        #region private variables
        private int fixtype;
        #endregion

        public UBXNAVPVT(byte[] msg) 
        {
            rawmsg = msg;
        }        


    }

    // UBX-NAV-RELPOSNED - 2 Pre Am, 1 Byte Class, 1 Header, 2 Length, 64 PayLoad, 1 CHKA, 1 CHKB (TOTAL 72)
    //
    // All values stored in native mm but returned as meters. 
    public class UBXNAVREPRO : UBXMSG
    {
        private byte[] rawmsg { get { return _rawmsg; } set {
                _rawmsg = value;

                relPosN = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 8 + offset));
                relPosE = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 12 + offset));
                relPosD = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 16 + offset));
                relPosLength = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 20 + offset));
                relPosHeading = Convert.ToDouble(BitConverter.ToInt32(_rawmsg, 24 + offset));

                relPosHPN = Convert.ToDouble((int)_rawmsg[32 + offset]);
                relPosHPD = Convert.ToDouble((int)_rawmsg[34 + offset]);
                relPosHPE = Convert.ToDouble((int)_rawmsg[33 + offset]);
                relPosHPLength = Convert.ToDouble((int)_rawmsg[35 + offset]);

                accN = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 36 + offset));
                accE = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 40 + offset));
                accD = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 44 + offset));

                accLength = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 48 + offset));
                accHeading = Convert.ToDouble(BitConverter.ToUInt32(_rawmsg, 52 + offset));

                fixStatus = new fBitFlag();


            } }

        private fBitFlag fixStatus
        {
            get => _fixStatus;
            set
            {
                _fixStatus = value;
                _fixStatus.BitField = _rawmsg[60 + offset];
            }
        }
        private fBitFlag _fixStatus;

        private byte[] _rawmsg; 

        public double relPosN
        {
            get => _relPosN / 100;
            set { _relPosN = value; }
        }            
        private double _relPosN;

        public double relPosE { get => _relPosE / 100;
            set { _relPosE = value; }
        }        
        private double _relPosE;

        public double relPosD { get => _relPosD / 100;
        set { _relPosD = value; }
        }
        private double _relPosD;

        public double relPosLength
        {
            get { return _relPosLength / 100; }
            set
            {
                _relPosLength = value;
            }
        }
        private double _relPosLength;

        private double _relPosHeading; 
        public double relPosHeading { get => _relPosHeading * Math.Pow(10, -5);
            set
            {
                _relPosHeading = value;
            }
        }

        public double relPosHPN { get => _relPosHPN / 1000;
        set { _relPosHPN = value; }
        }
        private double _relPosHPN;

        private double _relPosHPE;
        public double relPosHPE { get => _relPosHPE / 1000; set { _relPosHPE = value; } }

        private double _relPosHPD;
        public double relPosHPD { get => _relPosHPD / 1000; set { _relPosHPD = value; } }

        private double _relPosHPLength;
        public double relPosHPLength { get => _relPosHPLength / 1000; set { _relPosHPLength = value; } }

        private double _accN;
        public double accN { get => _accN / 1000; set { _accN = value; } }

        private double _accE;
        public double accE { get => _accE / 1000; set { _accE = value; } }

        private double _accD;
        public double accD { get => _accD / 1000; set { _accD = value; } }

        private double _accLength;
        public double accLength { get => _accLength / 1000; set { _accLength = value; } }

        private double _accHeading;
        public double accHeading { get => _accHeading * Math.Pow(10, -5); set { _accHeading = value; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct fBitFlag
        {
            public ushort BitField;

            public byte gnssFixOk
            {
                get { return (byte)((BitField >> 0) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 0) | (value << 0));
                }
            }

            public byte diffSoln
            {
                get { return (byte)((BitField >> 1) & 1); }
                set
                {
                    BitField = (ushort)(BitField & ~(1 << 1) | (value << 1));
                }
            }

            public byte relPosValid
            {
                get { return (byte)((BitField >> 2) & 1); } 
            }

            public byte isMoving
            {
                get { return (byte)((BitField >> 5) & 1); }
    
            }

            public byte carrSoln
            {
                get { return (byte)((BitField >> 4) & 1); }

            }

            public byte refPosMiss
            {
                get => (byte)((BitField >> 6) & 1);
            }

            public byte refObsMiss
            {
                get => (byte)((BitField >> 7) & 1);
            }

            public byte relPosHeadingValid
            {
                get => (byte)((BitField >> 8) & 1);
            }

            public byte relPosNormalized
            {
                get => (byte)((BitField >> 9) & 1);
            }

        }


        public UBXNAVREPRO(byte[] msg)  { rawmsg = msg;   }

    }

    // Main Class Structure for UBX Messages
    //
    public class UBXMSG
    {
        public  Dictionary<int, string> fixtypes = new Dictionary<int, string>() { { 0, "No Fix" },
        { 1, "Dead Reckoning" },
            {2, "2d Fix" },
            {3, "3d Fix" },
            {4, "GNSS Fix + Dead Reckoning" },
            {5, "Time Fix Only" } };
        public const int offset = 6;

    }

 
    class UBXReceived : EventArgs
    {

        private readonly UBXMSG _NewUbxReceived;

        public UBXReceived(UBXMSG ubxmsg)
        {
            _NewUbxReceived = ubxmsg;
        }

        public UBXMSG NewUbxReceived
        {
            get { return _NewUbxReceived; }
        }

    }

}
