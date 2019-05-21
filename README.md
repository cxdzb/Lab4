# 《C# .NET 综合应用程序开发》实验报告
#### 学院：软件学院  班级：2017级软工1班  学号：3017218062   姓名：刘书裴
#### 日期：  2019  年 5 月 20 日
## 一、功能概述
###### 1、实时接收温度和光强信息。
###### 2、输入命令显示需要的信息。
###### 3、通过滑轮控制对应的二极管亮度。
###### 4、输入学号和时间命令会显示对应的真实信息。
###### 5、点击log按钮进行记录，绘图，结束时保存到csv文件。

## 二、项目特色
###### 1、信息实时获取，实时改变。
###### 2、绘图使用zedgraph，可实现多种功能（放大、缩小、拖动）。
###### 3、使用DispatcherTimer代替多线程，实现了命令的顺序传送。
###### 3、加入try-catch异常语句，防止意外错误的发生。
###### 4、实现了窗口自适应。

## 三、代码总量
无法计算

## 四、工作时间
四周左右

## 五、知识点总结图（Concept MAP）
![](https://github.com/cxdzb/Lab4/blob/master/Images/1.png?raw=true)

## 六、结论
#### 实验过程：

###### 1、根据midi协议编写Auduino程序。
###### 2、实现串口连接/关闭
①　判断是否设置端口
②　设置相应的属性和接收事件
###### 3、实现信息接收/发送
①　接收信息时，先接收信息数量，再依次读取，转换为二进制后存入容器，判断是两个字节或三个字节后格式化显示在界面，特殊数据特殊处理。
②　发送数据时，先判断发送的字节数量，再按顺序依次发送。
③　对于跨线程调用控件，依旧使用Dispatcher.BeginInvoke(new Action(delegate {}));
###### 4、实时接收温度和光强
①　设定时钟，一秒执行一次Get_Data，绑定温度和光强。
②　Get_Data中分别发送读取温度和读取光强的命令，得到返回的数据后转换为真实的数据，然后进行绘图，每次绘图都要更新。
###### 5、改变LED亮度
①　五个灯依次判断，值改变了就发送命令改变亮度。
②　在改变后，储存对应的值用来判断下一次的改变。
③　由于滑块最左边是0，而我的电路中0为最亮，所以用10-value来将滑块与亮度对应。
###### 6、log开始/结束
①　与实时读取同步，也是依靠timer。
②　以log标志来进行开启关闭。
③　关闭时储存数据到csv文件中，随便清空两个点对容器。
#### 实验结果：
###### 1、运行
![](https://github.com/cxdzb/Lab4/blob/master/Images/2.png?raw=true)

###### 2、连接
![](https://github.com/cxdzb/Lab4/blob/master/Images/3.png?raw=true)

###### 3、获取运行时间
![](https://github.com/cxdzb/Lab4/blob/master/Images/4.png?raw=true)

###### 4、获取学号
![](https://github.com/cxdzb/Lab4/blob/master/Images/5.png?raw=true)

###### 5、改变亮度
![](https://github.com/cxdzb/Lab4/blob/master/Images/6.png?raw=true)
![](https://github.com/cxdzb/Lab4/blob/master/Images/7.png?raw=true)
![](https://github.com/cxdzb/Lab4/blob/master/Images/8.png?raw=true)

###### 6、Log开始（中途用手触碰热敏并遮住光敏两次）
![](https://github.com/cxdzb/Lab4/blob/master/Images/9.png?raw=true)