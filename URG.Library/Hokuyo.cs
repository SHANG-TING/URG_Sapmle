using System;
using System.Collections.Generic;
using System.Text;

namespace URG.Library {
    /// <summary>
    /// Supports the Hokuyo sensor.
    /// </summary>
    internal class Hokuyo: IDisposable {
        private bool _disposed;
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing) {
            if (_disposed) return;
            if (disposing) {
                Disconnect();
            }
            _disposed = true;
        }

        private UrgCtrl hokuyo;
        private readonly int baudRate;
        private readonly int comPort;
        private const int maxBufferSize = 682;

        /// <summary>
        /// Constructs a Hokuyo instance.
        /// </summary>
        /// <param name="comPort">Com port number for the Hokuyo</param>
        /// <param name="baudRate">BaudRate for the com port</param>
        public Hokuyo(int comPort, int baudRate) {
            this.baudRate = baudRate;
            this.comPort = comPort;
            Connect();
        }

        /// <summary>
        /// Connects to the Hokuyo sensor.
        /// </summary>
        public void Connect() {
            if (hokuyo != null) {
                Disconnect();
            }
            hokuyo = new UrgCtrl();

            try {
                hokuyo.Connect(comPort, baudRate);
            } catch (Exception e) {
                //Logger.Log(e);
            }
        }

        /// <summary>
        /// Disconnects from the Hokuyo sensor.
        /// </summary>
        public void Disconnect() {
            if (hokuyo.IsConnected()) {
                hokuyo.Disconnect();
            }
        }

        /// <summary>
        /// Gets distance data from the Hokuyo sensor.
        /// </summary>
        /// <returns>Distance in 682 points in mm from the Hokuyo sensor.</returns>
        public int[] GetData() {
            int[] distanceValuesFromHokuyo = new int[maxBufferSize];
            return Capture(distanceValuesFromHokuyo);
        }

        private int[] Capture(int[] distanceValuesFromHokuyo) {
            if (!hokuyo.IsConnected()) {
                Connect();
            }
            bool validData = false;
            while (!validData) {
                hokuyo.Capture(distanceValuesFromHokuyo);
                validData = ValidateData(distanceValuesFromHokuyo);
            }
            return distanceValuesFromHokuyo;
        }

        private bool ValidateData(int[] distanceValuesFromHokuyo) {
            int zeroNumberInData = 0;
            foreach (int value in distanceValuesFromHokuyo) {
                if (value == 0) {
                    zeroNumberInData++;
                }
            }
            if (zeroNumberInData == distanceValuesFromHokuyo.Length) {
                return false;
            } else {
                return true;
            }
        }

        public long[] GetCoordinate(int[] data, int index) {
            var l = data[index];
            var radian = hokuyo.Index2Radian(index);
            
            var x = (long)(l * -Math.Sin(radian));
            var y = (long)(l * Math.Cos(radian));

            return new long[] { x, y };
        }
        
        
    }
}
