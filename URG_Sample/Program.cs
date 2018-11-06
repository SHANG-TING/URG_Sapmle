using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Timer = System.Timers.Timer;
using System.Linq;
using URG.Library;

namespace URG_Sample {
    class Program {
        /// <summary>
        /// 鼠标控制参数
        /// </summary>
        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVENTF_LEFTUP = 0x4;
        const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        const int MOUSEEVENTF_MIDDLEUP = 0x40;
        const int MOUSEEVENTF_MOVE = 0x1;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        const int MOUSEEVENTF_RIGHTUP = 0x10;

        /// <summary>
        /// 鼠标的位置
        /// </summary>
        public struct PONITAPI {
            public int x, y;
        }

        [DllImport("user32.dll")]
        public static extern int GetCursorPos(ref PONITAPI p);

        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private static readonly int hokuyoComPort = 5;
        private static readonly int hokuyoBaudRate = 115200;

        static readonly int offsetX = 0;
        static readonly int offsetY = 1180;

        static readonly int width = 1535 + offsetX;
        static readonly int height = 760 + offsetY;


        private static Hokuyo hokuyo;

        private static PONITAPI p;

        [STAThread]
        static void Main() {

            using (hokuyo = new Hokuyo(hokuyoComPort, hokuyoBaudRate))
            using (Timer tmr = new Timer { Interval = 100 }) {
                tmr.Elapsed += Tmr_Elapsed;  // 使用事件代替委託
                tmr.Start();          // 開啟定時器
                Console.ReadLine();
                tmr.Stop();          // 停止定時器
                Console.ReadLine();
                tmr.Start();          // 重啟定時器
                Console.ReadLine();
            }

            PONITAPI p = new PONITAPI();
            GetCursorPos(ref p);
            //Console.WriteLine("鼠标现在的位置X:{0}, Y:{1}", p.x, p.y);
            //Console.WriteLine("Sleep 1 sec...");
            //Thread.Sleep(1000);

            //p.x = (new Random()).Next(Screen.PrimaryScreen.Bounds.Width);
            //p.y = (new Random()).Next(Screen.PrimaryScreen.Bounds.Height);
            //Console.WriteLine("把鼠标移动到X:{0}, Y:{1}", p.x, p.y);
            //SetCursorPos(p.x, p.y);
            //GetCursorPos(ref p);
            //Console.WriteLine("鼠标现在的位置X:{0}, Y:{1}", p.x, p.y);
            //Console.WriteLine("Sleep 1 sec...");
            //Thread.Sleep(1000);

            //Console.WriteLine("在X:{0}, Y:{1} 按下鼠标左键", p.x, p.y);
            //mouse_event(MOUSEEVENTF_LEFTDOWN, p.x, p.y, 0, 0);
            //Console.WriteLine("Sleep 1 sec...");
            //Thread.Sleep(1000);

            //Console.WriteLine("在X:{0}, Y:{1} 释放鼠标左键", p.x, p.y);
            //mouse_event(MOUSEEVENTF_LEFTUP, p.x, p.y, 0, 0);
            //Console.WriteLine("程序结束，按任意键退出....");
            //Console.ReadKey();
        }


        [STAThread]
        static void Tmr_Elapsed(object sender, EventArgs e) {

            try {
                var distanceValuesFromHokuyo = hokuyo.GetData();

                int i = 0;
                long x = width + 1;
                long y = height + 1;

                List<long> x_list = new List<long>();
                List<long> y_list = new List<long>();

                var coordinateList = new List<long[]>();

                foreach (int distanceValue in distanceValuesFromHokuyo) {
                    long[] coordinate = hokuyo.GetCoordinate(distanceValuesFromHokuyo, i);
                    long x_temp = coordinate[0];
                    long y_temp = coordinate[1];


                    if (x_temp > offsetX && x_temp <= width && y_temp > offsetY && y_temp <= height) {
                        //if (x > x_temp) x = x_temp;
                        //if (y > y_temp) y = y_temp;

                        coordinate[0] = coordinate[0] > offsetX ? coordinate[0] - offsetX : coordinate[0];
                        coordinate[1] = coordinate[1] > offsetY ? coordinate[1] - offsetY : coordinate[1];
                        coordinateList.Add(coordinate);
                        //x_list.Add(x_temp);
                        //y_list.Add(y_temp);

                        //Console.WriteLine($"x: {x_temp}, y: {y_temp}");
                    }

                    i++;
                }

                //if (x_list.Count > 0) x = Convert.ToInt64(x_list.Min());
                //if (y_list.Count > 0) y = Convert.ToInt64(y_list.Max());

                var data = coordinateList
                    .OrderByDescending(c => c[1])
                    .ThenBy(c => c[0])
                    .FirstOrDefault();

                x = data[0];
                y = 800 - data[1];

                Console.WriteLine($"x: {data[0]}, y: {data[1]}");

                //Console.WriteLine($"x: {x}, y: {y}");

                //x = x > 20 ? x - 20 : 0;
                //y = y > 20 ? y - 20 : 0;

                if (x > 0 && y > 0) {
                    //p.x = Convert.ToInt32(unchecked((int)x) * 1920 / (width - offsetX));
                    //p.y = 1080 - Convert.ToInt32(unchecked((int)y) * 1080 / (height - offsetY));

                    p.x = Convert.ToInt32(unchecked((int)x) * 1535 / (width - offsetX));
                    p.y = Convert.ToInt32(unchecked((int)y));

                    Console.WriteLine($"before x: {p.x}, y: {p.y}");

                    SetCursorPos(p.x, p.y);
                    GetCursorPos(ref p);

                    mouse_event(MOUSEEVENTF_LEFTDOWN, p.x, p.y, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, p.x, p.y, 0, 0);

                    Console.WriteLine($"after x: {p.x}, y: {p.y}");

                }

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
