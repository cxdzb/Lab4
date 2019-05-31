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

        List<byte> cdata = new List<byte>(),sdata = new List<byte>();   //接受数据的容器和发送数据的容器
        List<byte> temp = new List<byte> { 0, 0, 0 },inte = new List<byte> { 0, 0, 0 }; //温度和光强初始化
        int number = 0,log = 0; //用于计数接收数据的数和log标志

        DispatcherTimer timer;  //时钟
        PointPairList temp_list = new PointPairList(),inte_list = new PointPairList();  //温度和光强的点对列表
        LineItem line1,line2;   //温度和光强曲线
        GraphPane myPane1, myPane2;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();  //设定时钟，1秒调用一次Get_Data
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

            zedGraphControl.GraphPane.Title.Text = "温度";    //绘图控件
            zedGraphControl.GraphPane.XAxis.Title.Text = "时间/s";
            zedGraphControl.GraphPane.YAxis.Title.Text = "";
            zedGraphControl2.GraphPane.Title.Text = "光强";
            zedGraphControl2.GraphPane.XAxis.Title.Text = "时间/s";
            zedGraphControl2.GraphPane.YAxis.Title.Text = "";
        }
        private void ComboBoxPortName_DropDownOpened(object sender, EventArgs e)    //下拉事件
        {
            string[] portnames = SerialPort.GetPortNames(); //获取端口
            ComboBox x = sender as ComboBox;
            x.Items.Clear();
            foreach (string portname in portnames)  //添加端口
                x.Items.Add(portname);
        }

        private void recv_data(object sender,SerialDataReceivedEventArgs e) //接受数据
        {
            cdata.Clear();  //清空接收容器
            if (serial == null) //未选择端口
            {
                return;
            }
            int len = serial.BytesToRead;   //获取接收数据的字节数
            for(int i = 0; i < len; i++)    //依次接收
            {
                int d = serial.ReadByte();
                cdata.Add((byte)d);
            }
            if (cdata.Count == 3)   //3个字节
            {
                string msg = string.Format("Data{0:D}：{1:X2}-{2:X2}-{3:X2}",number,cdata[0],cdata[1],cdata[2]);
                number++;   //计数递增，超过100重置
                if (number == 100)
                {
                    number = 0;
                }
                Dispatcher.BeginInvoke(new Action(delegate {
                    response.Items.Add(msg);    //在listview内显示
                    response.ScrollIntoView(msg);   //进度条跟随新数据滚动
                    try
                    {
                        if (cdata[0] == 0xF9)   //计算学号
                        {
                            int student_number = (int)(cdata[1] | (cdata[2] << 7));
                            Real_Data.Text = "学号："+student_number.ToString();
                        }
                        else if (cdata[0] == 0xFF)   //计算运行时间
                        {
                            int run_time = (int)(cdata[1] | (cdata[2] << 7));
                            Real_Data.Text = "时间：" + run_time.ToString()+"ms";
                        }
                    }
                    catch (Exception){}
                }));

                if (cdata[0] == (byte)0xE0) //0xE0代表温度引脚
                {
                    temp.Clear();
                    temp.AddRange(cdata);   //用于实时显示
                }
                else if (cdata[0] == (byte)0xE1) //0xE0代表光强引脚
                {
                    inte.Clear();
                    inte.AddRange(cdata);
                }
            }
            if (cdata.Count == 2)   //2个字节，类似3个字节
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
        private void send_data(byte cmd, byte first, byte second)   //发送数据
        {
            if (serial != null && serial.IsOpen)    //当串口打开时
            {
                byte[] a = {cmd,first,second };
                serial.Write(a,0,3);    //发送3个字节的命令
            }
        }
        private void send_data(byte cmd, byte data) //发送数据（2）
        {
            if (serial != null && serial.IsOpen)
            {
                byte[] a = { cmd, data };
                serial.Write(a, 0, 2);
            }
        }

        private void Serial_Open(object sender, RoutedEventArgs e)  //打开串口的事件
        {
            if (com.SelectedItem != null)   //如果选择了串口
            {
                if (serial != null) //已经打开过了，则关闭
                {
                    serial.DataReceived -= new SerialDataReceivedEventHandler(recv_data);
                    serial.Close();
                }
                serial = new SerialPort(com.SelectedItem.ToString());   //新建串口

                serial.BaudRate = int.Parse(rate.SelectedItem.ToString());  //设置对应的属性
                serial.Parity = Parity.None;
                serial.StopBits = StopBits.One;
                serial.DataBits = 8;
                serial.Handshake = Handshake.None;
                serial.RtsEnable = false;
                serial.ReceivedBytesThreshold = 1;
                serial.DataReceived += new SerialDataReceivedEventHandler(recv_data);   //接收数据的事件

                serial.Open();

                timer.Start();  //打开的同时启动时钟
            }
            else
            {
                MessageBox.Show("请选择端口！", "警告");
            }
        }
        private void Serial_Close(object sender, RoutedEventArgs e) //关闭串口的事件
        {
            if (serial != null)
            {
                serial.DataReceived -= new SerialDataReceivedEventHandler(recv_data);
                serial.Close();
                timer.Stop();   //关闭的同时暂停时钟
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) //发送消息按钮的事件
        {
            sdata.Clear();  //清空发送数据容器
            string[] message = request.Text.Split(' '); //先分割命令字符串
            if (request.Text.Length == 8 && message.Length == 3&&message[0].Length==2 && message[1].Length == 2 && message[2].Length == 2)  //3个字节
            {
                try
                {
                    for(int i = 0; i < 3; i++)
                    {
                        sdata.Add(Convert.ToByte(message[i],16));   //字符串转16进制数
                    }
                    send_data(sdata[0], sdata[1], sdata[2]);    //发送命令
                }
                catch (Exception)
                {
                    MessageBox.Show("请正确输入需要发送的指令！", "警告");
                }
            }
            else if(request.Text.Length == 5 && message.Length == 2 && message[0].Length == 2 && message[1].Length == 2)  //2个字节
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

        private void Log_Start(object sender, RoutedEventArgs e)    //开始记录日志
        {
            log = 1;    //标志置1
        }
        private void Log_End(object sender, RoutedEventArgs e)  //结束记录日志
        {
            log = 0;    //标志置0

            string fileName = System.IO.Path.GetFullPath(@"..\..\..\" + "log.csv"); //保存日志
            StreamWriter fileWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            fileWriter.Write("时间,温度,光强\r\n");   //开始写入，csv文件以,分割
            for(int i=0;i< temp_list.Count;i++)
            {
                fileWriter.Write("{0},{1},{2}\r\n", i,temp_list[i].Y,inte_list[i].Y);
            }
            fileWriter.Flush();
            fileWriter.Close();

            temp_list.Clear();  //清空两个点对数组
            inte_list.Clear();
        }

        private void Get_Data(object sender, EventArgs e)   //用于实时获取数据
        {
            //发送温度命令
            byte cmd = (byte)0xE0;  //E？ 11 11表示读取对应引脚的AD值
            byte first = (byte)0x11;
            byte second = first;
            send_data(cmd, first, second);
            if (temp[0] != 0 && temp.Count == 3)
                da.Temperature = ((int)(temp[1] | (temp[2] << 7))).ToString();  //转换为10进制数
            //发送光强命令
            cmd = (byte)0xE1;
            send_data(cmd, first, second);
            if (inte[0] != 0 && inte.Count == 3)
            da.Intensity = ((int)(inte[1] | (inte[2] << 7))).ToString();
            //实时显示数据
            if (log == 1)
            {
                temp_list.Add(temp_list.Count, da.Get_Temp());  //每次加入实时的数据
                myPane1 = zedGraphControl.GraphPane;
                line1 = myPane1.AddCurve("",temp_list, System.Drawing.Color.Red, SymbolType.Star);  //添加一条折线

                inte_list.Add(inte_list.Count, da.Get_Inte());
                myPane2 = zedGraphControl2.GraphPane;
                line2 = myPane2.AddCurve("", inte_list, System.Drawing.Color.Blue, SymbolType.Diamond);

                zedGraphControl.AxisChange();   //坐标变换
                zedGraphControl.Refresh();  //界面刷新
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
        }
        private void Change_Value(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            total.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)red.Value, (byte)green.Value, (byte)blue.Value));   //绘制结合的颜色
            //判断是哪个值改变了
            if (255-red.Value != led.red)
            {
                led.red = 255 - red.Value;
                int pwm = (int)led.red;
                send_data(0xD9,(byte)(pwm&0x7F), (byte)((pwm>>7)&0x1));
                return;
            }
            if (255 - green.Value != led.green)
            {
                led.green = 255 - green.Value;
                int pwm = (int)led.green;
                send_data(0xD5, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (255 - yellow.Value != led.yellow)
            {
                led.yellow = 255 - yellow.Value;
                int pwm = (int)led.yellow;
                send_data(0xD3, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (255 - blue.Value != led.blue)
            {
                led.blue = 255 - blue.Value;
                int pwm = (int)led.blue;
                send_data(0xD6, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
            if (255 - white.Value != led.white)
            {
                led.white = 255 - white.Value;
                int pwm = (int)led.white;
                send_data(0xDA, (byte)(pwm & 0x7F), (byte)((pwm >> 7) & 0x1));
                return;
            }
        }
    }
}
