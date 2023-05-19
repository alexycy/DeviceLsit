using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.IO.Ports;
using System.Net.Sockets;

namespace DataAcquisitionServerAppWithWebPage.Data
{

    public class Device
    {
        public string Type { get; set; }
        public DeviceSettingsBase Settings { get; set; }
        public bool IsRegistered { get; set; }
        public object Instance { get; set; } // 新增对象用于存储设备实例

        public DeviceType GetDeviceType()
        {
            return Enum.Parse<DeviceType>(Type);
        }

        public void CreatDeviceSettings()
        {
            switch (GetDeviceType())
            {
                case DeviceType.SerialPort:
                    Settings = new SerialPortSettings();
                    break;
                case DeviceType.TcpListener:
                    Settings = new TcpListenerSettings();
                    break;
                // 添加其他设备类型的配置参数类
                default:
                  break;
            }
        }

        public void CreateDeviceInstance()
        {
            switch (GetDeviceType())
            {
                case DeviceType.SerialPort:
                    Instance = new SerialPort();
                    break;
                case DeviceType.TcpListener:
                    Instance = new TcpListener(IPAddress.Any, 0);
                    break;
                // 添加其他设备类型的实例化
                default:
                    throw new NotSupportedException($"Device type '{Type}' is not supported.");
            }
        }


        public virtual void Start()
        {
            // 默认的设备启动逻辑
            Console.WriteLine($"Starting {Settings.DeviceName} ...");
        }

        public virtual void Stop()
        {
            // 默认的设备停止逻辑
            Console.WriteLine($"Stopping {Settings.DeviceName} ...");
        }

        /// <summary>
        /// 动态修改参数后  调用该函数对当前设备的对象实例生效
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public void ApplyDeviceSettings()
        {
            switch (Instance)
            {
                case SerialPort serialPort:
                    if (Settings is SerialPortSettings serialPortSettings)
                    {
                        serialPort.PortName = serialPortSettings.PortName;
                        serialPort.BaudRate = serialPortSettings.BaudRate;
                        // 添加其他串口特定的配置参数
                    }
                    break;
                case TcpListener tcpListener:
                    if (Settings is TcpListenerSettings tcpListenerSettings)
                    {
                        tcpListener.Server.Bind(new IPEndPoint(IPAddress.Parse(tcpListenerSettings.Address), tcpListenerSettings.Port));
                        // 添加其他TCP监听特定的配置参数
                    }
                    break;
                // 添加其他设备类型的配置参数
                default:
                    throw new NotSupportedException($"Device type '{Instance.GetType().Name}' is not supported.");
            }
        }
    }


    public abstract class DeviceSettingsBase
    {
        // 添加通用的设备配置参数
        public string DeviceName { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }

        public abstract void Configure(XElement settingsElement);
    }

    public class SerialPortSettings : DeviceSettingsBase
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        // 添加串口设备特定的配置参数

        public override void Configure(XElement settingsElement)
        {
            // 解析配置参数并设置属性值
            PortName = settingsElement?.Element("PortName")?.Value;
            BaudRate = int.Parse(settingsElement?.Element("BaudRate")?.Value);
        }
    }

    public class TcpListenerSettings : DeviceSettingsBase
    {

        // 添加TCP监听设备特定的配置参数

        public override void Configure(XElement settingsElement)
        {
            // 解析配置参数并设置属性值
            Address = settingsElement?.Element("TcpAddress")?.Value;
            Port = int.Parse(settingsElement?.Element("TcpPort")?.Value);
        }
    }

    public enum DeviceType
    {
        SerialPort,
        TcpListener,
        // 添加其他设备类型...
    }

    public class DeviceManager
    {
        private List<Device> _devicesList;

        public DeviceManager()
        {
            _devicesList = new List<Device>();
        }

        public void LoadDevicesFromXml(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"XML file '{filename}' not found.");
            }

            XDocument doc = XDocument.Load(filename);
            _devicesList = doc.Root.Elements("Device")
                .Select(e => new Device
                {
                    Type = e.Element("Type")?.Value,
                    IsRegistered = bool.Parse(e.Element("IsRegistered")?.Value),
                    Settings = CreateDeviceSettings(e.Element("Type")?.Value, e.Element("Settings"))
                })
                .ToList();
        }

        public void SaveDevicesToXml(string filename)
        {
            XDocument doc = new XDocument(
                new XElement("Devices",
                    _devicesList.Select(d =>
                        new XElement("Device",
                            new XElement("Type", d.Type),
                            new XElement("IsRegistered", d.IsRegistered),
                            SerializeDeviceSettings(d.Settings)
                        )
                    )
                )
            );

            doc.Save(filename);
        }


        private DeviceSettingsBase CreateDeviceSettings(string deviceType, XElement settingsElement)
        {
            DeviceType type = Enum.Parse<DeviceType>(deviceType);

            DeviceSettingsBase settings = null;
            switch (type)
            {
                case DeviceType.SerialPort:
                    settings = new SerialPortSettings();
                    break;
                case DeviceType.TcpListener:
                    settings = new TcpListenerSettings();
                    break;
                // 添加其他设备类型的配置参数类
                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceType), deviceType, "Invalid device type.");
            }

            if (settingsElement != null)
            {
                // 解析配置参数
                // 根据具体的配置参数类进行设置
                switch (settings)
                {
                    case SerialPortSettings serialPortSettings:
                        serialPortSettings.PortName = settingsElement.Element("PortName")?.Value;
                        serialPortSettings.BaudRate = int.Parse(settingsElement.Element("BaudRate")?.Value);
                        break;
                    case TcpListenerSettings tcpListenerSettings:
                        tcpListenerSettings.Address = settingsElement.Element("TcpAddress")?.Value;
                        tcpListenerSettings.Port = int.Parse(settingsElement.Element("TcpPort")?.Value);
                        break;
                        // 添加其他设备类型的配置参数解析
                }
            }

            return settings;
        }

        private XElement SerializeDeviceSettings(DeviceSettingsBase settings)
        {
            if (settings == null)
            {
                return null;
            }

            XElement settingsElement = new XElement("Settings");

            // 根据具体的配置参数类进行序列化
            switch (settings)
            {
                case SerialPortSettings serialPortSettings:
                    settingsElement.Add(
                        new XElement("PortName", serialPortSettings.PortName),
                        new XElement("BaudRate", serialPortSettings.BaudRate)
                    );
                    break;
                case TcpListenerSettings tcpListenerSettings:
                    settingsElement.Add(
                        new XElement("TcpAddress", tcpListenerSettings.Address),
                        new XElement("TcpPort", tcpListenerSettings.Port)
                    );
                    break;
                    // 添加其他设备类型的配置参数序列化
            }

            return settingsElement;
        }


    }
}

