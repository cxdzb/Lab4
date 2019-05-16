using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZedGraph;
using System.Drawing;
using System.IO;

namespace Lab4
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Data da = new Data();
        LED led = new LED ();
        SerialPort serial = null;

        List<byte> cdata = new List<byte>(),sdata = new List<byte>();
        List<byte> temp = new List<byte> { 0, 0, 0 },inte = new List<byte> { 0, 0, 0 };
        int number = 0,log = 0;

        DispatcherTimer timer;
        PointPairList temp_list = new PointPairList(),inte_list = new PointPairList();
        LineItem line1,line2;
        GraphPane myPane1, myPane2;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();  //设定时钟
            timer.Tick += new EventHandler(Get_Data);
            timer.Interval = TimeSpan.FromSeconds(1);

            Binding binding_temp = new Binding();   //数据绑定，绑定温度和光强
            Binding binding_inte = new Binding();
            binding_temp.Source = da;
            binding_inte.Source = da;
            binding_temp.Path =new PropertyPath("Temperature");
            binding_inte.Path = new PropertyPath("Intensity");
            temperature.SetBinding(TextBox.TextProperty, binding_temp);
            light_intensity.SetBinding(TextBox.TextProperty, binding_inte);

            rate.Items.Clear();  //清空rate，并将波特率填入
            rate.Items.Add("9600");
            rate.Items.Add("19200");
            rate.Items.Add("38400");
            rate.Items.Add("57600");
            rate.Items.Add("115200");
            rate.Items.Add("921600");
            rate.SelectedItem = rate.Items[0];

            zedGraphControl.GraphPane.Title.Text = "温度";
            zedGraphControl.GraphPane.XAxis.Title.Text = "时间/s";
            zedGraphControl.GraphPane.YAxis.Title.Text = "";
            zedGraphControl2.GraphPane.Title.Text = "光强";
            zedGraphControl2.GraphPane.XAxis.Title.Text = "时间/s";
            zedGraphControl2.GraphPane.YAxis.Title.Text = "";
        }
        private void ComboBoxPortName_DropDownOpened(object sender, EventArgs e)
        {
            string[] portnames = SerialPort.GetPortNames();
            ComboBox x = sender as ComboBox;
            x.Items.Clear();
            foreach (string portname in portnames)
                x.Items.Add(portname);
        }
        private void recv_data(object sender,SerialDataReceivedEventArgs e)
        {
            cdata.Clear();
            if (serial == null)
            {
                return;
            }
            int len = serial.BytesToRead;
            for(int i = 0; i < len; i++)
            {
                int d = serial.ReadByte();
                cdata.Add((byte)d);
            }
            if (cdata.Count == 3)
            {
                string msg = string.Format("Data{0:D}：{1:X2}-{2:X2}-{3:X2}",number,cdata[0],cdata[1],cdata[2]);
                number++;
                if (number == 100)
                {
                    number = 0;
                }
                Dispatcher.BeginInvoke(new Action(delegate {
                    response.Items.Add(msg);
                    response.ScrollIntoView(msg);
                }));

                if (cdata[0] == (byte)0xE0)
                {
                    temp.Clear();
                    temp.AddRange(cdata);
                }
                else if (cdata[0] == (byte)0xE1)
                {
                    inte.Clear();
                    inte.AddRange(cdata);
                }
            }
            if (cdata.Count == 2)
            {
                string msg = string.Format("Data{0:D}：{1:X2}-{2:X2}", number, cdata[0], cdata[1]);
                number++;
                if (number == 100)
                {
                    number = 0;
                }
                Dispatcher.BeginInvoke(new Action(delegate {
                    response.Items.Add(msg);
                    response.ScrollIntoView(msg);
                }));
            }
        }
        private void send_data(byte cmd, byte first, byte second)
        {
            if (serial != null && serial.IsOpen)
            {
                byte[] a = {cmd,first,second };
                serial.Write(a,0,3);
            }
        }
        private void send_data(byte cmd, byte data)
        {
            if (serial != null && serial.IsOpen)
            {
                byte[] a = { cmd, data };
                serial.Write(a, 0, 2);
            }
        }

        private void Serial_Open(object sender, RoutedEventArgs e)
        {
            if (com.SelectedItem != null)
            {
                if (serial != null)
                {
                    serial.DataReceived -= new SerialDataReceivedEventHandler(recv_data);
                    serial.Close();
                }
                serial = new SerialPort(com.SelectedItem.ToString());

                serial.BaudRate = int.Parse(rate.SelectedItem.ToString());
                serial.Parity = Parity.None;
                serial.StopBits = StopBits.One;
                serial.DataBits = 8;
                serial.Handshake = Handshake.None;
                serial.RtsEnable = false;
                serial.ReceivedBytesThreshold = 1;
                serial.DataReceived += new SerialDataReceivedEventHandler(recv_data);

                serial.Open();

                timer.Start();
            }
            else
            {
                MessageBox.Show("请选择端口！", "警告");
            }
        }
        private void Serial_Close(object sender, RoutedEventArgs e)
        {
            if (serial != null)
            {
                serial.DataReceived -= new SerialDataReceivedEventHandler(recv_data);
                serial.Close();
                timer.Stop();
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sdata.Clear();
            string[] message = request.Text.Split(' ');
            if (request.Text.Length == 8 && message.Length == 3&&message[0].Length==2 && message[1].Length == 2 && message[2].Length == 2)
            {
                try
                {
                    for(int i = 0; i < 3; i++)
                    {
                        sdata.Add(Convert.ToByte(message[i],16));
                    }
                    send_data(sdata[0], sdata[1], sdata[2]);
                }
                catch (Exception)
                {
                    MessageBox.Show("请正确输入需要发送的指令！", "警告");
                }
            }
            else if(request.Text.Length == 5 && message.Length == 2 && message[0].Length == 2 && message[1].Length == 2)
            {
                try
                {
                    for (int i = 0; i < 2; i++)
                    {
                        sdata.Add(Convert.ToByte(message[i], 16));
                    }
                    send_data(sdata[0], sdata[1]);
                }
                catch (Exception)
                {
                    MessageBox.Show("请正确输入需要发送的指令！", "警告");
                }
            }
            else
            {
                MessageBox.Show("请正确输入需要发送的指令！", "警告");
            }
            
        }

        private void Log_Start(object sender, RoutedEventArgs e)
        {
            log = 1;
        }
        private void Log_End(object sender, RoutedEventArgs e)
        {
            log = 0;
            string fileName = System.IO.Path.GetFullPath(@"..\..\..\" + "log.csv");
            StreamWriter fileWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            fileWriter.Write("时间,温度,光强\r\n");
            for(int i=0;i< temp_list.Count;i++)
            {
                fileWriter.Write("{0},{1},{2}\r\n", i,temp_list[i].Y,inte_list[i].Y);
            }
            fileWriter.Flush();
            fileWriter.Close();
            temp_list.Clear();
            inte_list.Clear();
        }

        private void Get_Data(object sender, EventArgs e)
        {
            byte cmd = (byte)0xE0;
            byte first = (byte)0x11;
            byte second = first;
            send_data(cmd, first, second);
            if (temp[0] != 0 && temp.Count == 3)
                da.Temperature = ((int)(temp[1] | (temp[2] << 7))).ToString();

            cmd = (byte)0xE1;
            send_data(cmd, first, second);
            if (inte[0] != 0 && inte.Count == 3)
            da.Intensity = ((int)(inte[1] | (inte[2] << 7))).ToString();

            if (log == 1)
            {
                temp_list.Add(temp_list.Count, da.Get_Temp());
                myPane1 = zedGraphControl.GraphPane;
                line1 = myPane1.AddCurve("",temp_list, System.Drawing.Color.Red, SymbolType.Star);

                inte_list.Add(inte_list.Count, da.Get_Inte());
                myPane2 = zedGraphControl2.GraphPane;
                line2 = myPane2.AddCurve("", inte_list, System.Drawing.Color.Blue, SymbolType.Diamond);

                zedGraphControl.AxisChange();
                zedGraphControl.Refresh();
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
        }
        private void Change_Value(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            total.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)((10-red.Value)*25.5), (byte)((10 - green.Value) * 25.5), (byte)((10 - blue.Value) * 25.5)));
            if (10-red.Value != led.red)
            {
                led.red = 10-red.Value;
                int pwm = (int)(led.red * 255 / 10);
                send_data(0xD9,(byte)(pwm&0x7F), (byte)((pwm>>7)&0x1));
                return;
            }
            if (10 - green.Value != led.green)
            {
                led.green = 10 - green.Value;
                int pwm = (int)(led.green * 255 / 10);
                send_data(0xD5, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (10 - yellow.Value != led.yellow)
            {
                led.yellow = 10 - yellow.Value;
                int pwm = (int)(led.yellow * 255 / 10);
                send_data(0xD3, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (10 - blue.Value != led.blue)
            {
                led.blue = 10 - blue.Value;
                int pwm = (int)(led.blue * 255 / 10);
                send_data(0xD6, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (10 - white.Value != led.white)
            {
                led.white = 10 - white.Value;
                int pwm = (int)(led.white * 255 / 10);
                send_data(0xDA, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
        }
    }
}
