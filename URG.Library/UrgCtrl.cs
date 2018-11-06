using System;
using System.IO.Ports;

namespace URG.Library {
    /// <summary>
    /// Class to control Hokuyo URG-04LX laser sensor.
    /// </summary>
    public class UrgCtrl {
        private string error_message_ = "no connection.";

        private SerialPort con_ = new SerialPort();

        private int distance_min_;

        private int distance_max_;

        private int area_min_;

        private int area_max_ = 1;

        private int area_front_;

        private int area_total_ = 1;

        private int scan_rpm_ = 1;

        private int last_timestamp_;

        /// <summary>
        /// The maximum buffer size of Hokuyo data. (int)
        /// </summary>
        public int MaxBufferSize {
            get {
                return this.area_max_ + 1;
            }
        }

        /// <summary>
        /// The maximun detection range of Hokuyo sensor in mm. (int)
        /// </summary>
        public int MaxDistance {
            get {
                return this.distance_max_;
            }
        }

        /// <summary>
        /// The minimum detection range of Hokuyo sensor in mm. (int)
        /// </summary>
        public int MinDistance {
            get {
                return this.distance_min_;
            }
        }

        /// <summary>
        /// The standard scan speed of Hokuyo sensor in msec. (int)
        /// </summary>
        public int ScanMsec {
            get {
                return this.scan_rpm_ / 6 + 1;
            }
        }

        /// <summary>
        /// Class to control Hokuyo URG-04LX laser sensor.
        /// </summary>
        public UrgCtrl() {
        }

        /// <summary>
        /// To capture data from Hokuyo sensor. 
        /// </summary>
        /// <param name="data">Data array return from Hokuyo sensor.
        /// User need to specify an array for the function to modify. For example:
        /// int[] data = new int[MaxBufferSize].(int)</param>
        /// <returns>Length of data array. (int)</returns>
        public int Capture(int[] data) {
            this.con_.WriteLine(string.Format("GD{0:d4}{1:d4}01", this.area_min_, this.area_max_));
            int num = 0;
            try {
                int num1 = 0;
                string str = "";
                while (true) {
                    string str1 = this.con_.ReadLine();
                    if (str1.Length == 0) {
                        break;
                    }
                    if (num1 == 2) {
                        this.last_timestamp_ = this.decode(str1, 0, 4);
                    }
                    if (num1 >= 3) {
                        str = string.Concat(str, str1);
                        int length = str.Length - 1;
                        int num2 = 0;
                        for (int i = 0; i < length - 2; i += 3) {
                            int num3 = this.decode(str, i, 3);
                            num2 = i;
                            data[num] = num3;
                            num++;
                        }
                        int num4 = length - (num2 + 3);
                        str = str.Substring(num2 + 3, num4);
                    }
                    num1++;
                }
            } catch (TimeoutException timeoutException) {
                Console.WriteLine("TimeoutException");
            }
            return num;
        }

        /// <summary>
        /// To connect to Hokuyo sensor.
        /// </summary>
        /// <param name="ComPort">The Com Port connected to Hokuyo sensor. (int)</param>
        /// <param name="baudrate">Baudrate for communication between sensor and computer,3 baudrate 
        /// available: 19200, 57600 and 115200, the default baudrate for Hokuyo URG-04LX is 19200. (int)</param>
        /// <returns></returns>
        public bool Connect(int ComPort, int baudrate) {
            string str = string.Concat("COM", Convert.ToString(ComPort));
            this.con_.PortName = str;
            this.con_.BaudRate = baudrate;
            this.con_.Open();
            this.con_.DiscardInBuffer();
            this.con_.DiscardOutBuffer();
            int[] numArray = new int[] { 19200, 115200, 38400 };
            int num = 0;
            if (num < (int)numArray.Length) {
                int num1 = numArray[num];
                this.sendCommand("SCIP2.0", 200, true);
            }
            this.receivePP();
            this.sendCommand("BM", 200, true);
            return true;
        }

        private int decode(string bytes, int offset, int length) {
            int num = 0;
            for (int i = 0; i < length; i++) {
                num <<= 6;
                num &= -64;
                num = num | bytes[offset + i] - 48;
            }
            return num;
        }

        /// <summary>
        /// To disconnect from Hokuyo sensor.
        /// </summary>
        public void Disconnect() {
            this.con_.Close();
        }

        private int getSubValue(string line) {
            int length = line.Length;
            return Convert.ToInt32(line.Substring(5, length - 5 - 2));
        }

        /// <summary>
        /// The time stamp from Hokuyo sensor.
        /// </summary>
        /// <returns>Time stamp from Hokuyo sensor. (int)</returns>
        public int GetTimestamp() {
            return this.last_timestamp_;
        }

        /// <summary>
        /// To get version information for Hokuyo sensor.
        /// </summary>
        /// <returns>Return sensor version details such as serial number, firmware version in 6 lines.For example: 
        /// Line 1 = V V [LF] 0 0 P [LF], 
        /// line 2 = VEND: Hokuyo Automatic Co., Ltd;,
        /// line 3 = PROD: SOKUIKI Sensor URG-04LX;,
        /// line 4 = FIRM: 3.0.00, 06/10/05;
        /// line 5 = PROT: SCIP 2.0;,
        /// line 6 = SERI: H0508486;. 
        /// (string)</returns>
        public string[] GetVersionInformation() {
            this.sendCommand("VV", 200, false);
            string[] strArrays = new string[5];
            int num = 0;
            while (true) {
                try {
                    while (true) {
                        Label0:
                        string str = this.con_.ReadLine();
                        if (str.Length == 0) {
                            break;
                        }
                        strArrays[num] = str;
                        num++;
                        goto Label0;
                    }
                    break;
                } catch (TimeoutException timeoutException) {
                }
            }
            return strArrays;
        }

        /// <summary>
        /// To convert the data received from Hokuyo from index to radian. 
        /// </summary>
        /// <param name="index">The index to convert to radian, 0-725.(int)</param>
        /// <returns>Radian after convert.(double)</returns>
        public double Index2Radian(int index) {
            double num = (double)(index - this.area_front_) * 2 * 3.14159265358979 / (double)this.area_total_;
            return num;
        }

        /// <summary>
        /// To check connection status.
        /// </summary>
        /// <returns>Return true if connected, return false if no connected. (bool)</returns>
        public bool IsConnected() {
            return this.con_.IsOpen;
        }

        /// <summary>
        /// To convert the data received from Hokuyo from radian to index. 
        /// </summary>
        /// <param name="radian">The radian to convert to index, -2.09 to 2.09. (double)</param>
        /// <returns>The index after convert.(int)</returns>
        public int Radian2Index(double radian) {
            int areaMax_ = (int)(radian * (double)this.area_front_ / 6.28318530717959 + (double)this.area_front_);
            if (areaMax_ < 0) {
                areaMax_ = 0;
            } else if (areaMax_ > this.area_max_) {
                areaMax_ = this.area_max_;
            }
            return areaMax_;
        }

        private bool receivePP() {
            this.sendCommand("PP", 200, false);
            this.con_.ReadTimeout = 10;
            int num = 0;
            while (true) {
                try {
                    string str = this.con_.ReadLine();
                    if (str.Length == 0) {
                        break;
                    } else if (num == 4) {
                        this.area_min_ = this.getSubValue(str);
                    } else if (num == 5) {
                        this.area_max_ = this.getSubValue(str);
                    } else if (num == 1) {
                        this.distance_min_ = this.getSubValue(str);
                    } else if (num == 2) {
                        this.distance_max_ = this.getSubValue(str);
                    } else if (num == 6) {
                        this.area_front_ = this.getSubValue(str);
                    } else if (num == 3) {
                        this.area_total_ = this.getSubValue(str);
                    } else if (num == 7) {
                        this.scan_rpm_ = this.getSubValue(str);
                    }
                } catch (TimeoutException timeoutException) {
                    break;
                }
                num++;
            }
            return false;
        }

        private int sendCommand(string command, int timeout, bool read_lf) {
            int num;
            this.con_.ReadTimeout = timeout;
            this.con_.WriteLine(command);
            try {
                string str = this.con_.ReadLine();
                if (str.CompareTo(command) == 0) {
                    str = this.con_.ReadLine();
                    if (read_lf) {
                        this.con_.ReadLine();
                    }
                    if (str.Length < 3) {
                        return -2;
                    } else {
                        num = Convert.ToInt32(str.Substring(0, 2), 16);
                    }
                } else {
                    num = -1;
                }
            } catch (TimeoutException timeoutException) {
                return -2;
            }
            return num;
        }

        /// <summary>
        /// To send error message when there is no connection.
        /// </summary>
        /// <returns>error message = "no connection".(string)</returns>
        public string what() {
            return this.error_message_;
        }

        private enum Timeout {
            Each = 10,
            First = 200
        }
    }
}