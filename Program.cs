// See https://aka.ms/new-console-template for more information
using DataAcquisitionServerAppWithWebPage.Data;
using MQTTnet.Samples.Server;
using System.Xml.Linq;

Console.WriteLine("Hello, World!");

Console.WriteLine();

while (true)
{
    var key = Console.ReadKey(true).Key;

    if (key == ConsoleKey.S)
    {
        //await Server_Simple_Samples.Run_Minimal_Server();
        // 加载XML文件
        XDocument doc = XDocument.Load("devices.xml");

        // 创建设备列表
        List<Device> devices = new List<Device>();

        // 解析XML文件，生成设备对象
        foreach (var deviceElement in doc.Root.Elements("Device"))
        {
            string deviceType = deviceElement.Element("Type")?.Value;
            bool isRegistered = bool.Parse(deviceElement.Element("IsRegistered")?.Value);

            Device device = new Device();
            device.Type = deviceType;
            device.IsRegistered = isRegistered;

            // 根据设备类型设置设备配置
            device.CreateDeviceInstance();
            device.CreatDeviceSettings();

            // 获取设备配置参数的XML元素
            XElement settingsElement = deviceElement.Element("Settings");

            // 设置设备配置参数
            device.Settings.Configure(settingsElement);

            // 添加设备到列表
            devices.Add(device);
        }


       Device device1 = new Device();

        device1.Type = DeviceType.SerialPort.ToString().Trim();
        device1.CreatDeviceSettings();
        device1.CreateDeviceInstance();
        device1.Settings = new SerialPortSettings()
        {
            PortName = "COM2",
            BaudRate = 9600
        };
        device1.IsRegistered = true;
        devices.Add(device1);

        
    }

    else if (key == ConsoleKey.P)
    {
        break;
    }
    else if (key == ConsoleKey.Q)
    {
        break;
    }
}