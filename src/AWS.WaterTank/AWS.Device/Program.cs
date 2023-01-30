﻿using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WaterTank.Models;

namespace AWS.Device;
class Program
{
    static Xbee xbee;
    static void Main(string[] args)
    {
       
        var iotEndPoint = "a2teks7xu15e4c-ats.iot.ap-southeast-1.amazonaws.com";
        var iotPort = 8883;
        var deviceId = "water-monitor-gateway";

        var topicShadowUpdate = string.Format("$aws/things/{0}/shadow/update", deviceId);
        var topicShadowGet = string.Format("$aws/things/{0}/shadow/get", deviceId);

        var message = "{\"state\":{\"desired\":{\"My message\":\"Hello World\"}}}";
        var rootCert = Path.Join(AppContext.BaseDirectory, "AmazonRootCA1.crt");
        var clientcert = Path.Join(AppContext.BaseDirectory, "certificate.cert.pfx");
        var privatecert = Path.Join(AppContext.BaseDirectory, "water-monitor-gateway.private.key");
        var caCertSource = File.ReadAllBytes(rootCert);//UTF8Encoding.UTF8.GetBytes("Need AWS root CA certificate");
        var clientCertSource = File.ReadAllBytes(clientcert); //UTF8Encoding.UTF8.GetBytes("Need your AWS client CA certificate");
        var privateKeyData = File.ReadAllBytes(privatecert);//UTF8Encoding.UTF8.GetBytes("Need your AWS private key");
        X509Certificate CaCert = new X509Certificate(caCertSource);
        X509Certificate ClientCert = new X509Certificate2(clientCertSource,"123qweasd");

        var iotClient = new MqttClient(iotEndPoint, iotPort, true, CaCert, ClientCert, MqttSslProtocols.TLSv1_2);
        //string clientId = Guid.NewGuid().ToString();
        var connectCode = iotClient.Connect(deviceId);
        Console.WriteLine($"Connected to AWS IoT with client id: {deviceId}.");

        /*
        var clientSetting = new MqttClientSetting
        {
            BrokerName = iotEndPoint,
            BrokerPort = iotPort,
            CaCertificate = CaCert,
            ClientCertificate = ClientCert,
            SslProtocol = System.Security.Authentication.SslProtocols.Tls12
        };

        var iotClient = new Mqtt(clientSetting);
        */
        iotClient.MqttMsgPublishReceived += (p1,p2) =>
        {
            Debug.WriteLine("Received message: " + Encoding.UTF8.GetString(p2.Message));
        };

        iotClient.MqttMsgSubscribed += (a, b) => { Debug.WriteLine("Subscribed"); };

        Debug.WriteLine("Connecting....");



        //var connectCode = iotClient.Connect(connectSetting);
        string topic = "Hello/World";
        ushort packetId = 1;

        iotClient.Subscribe(new string[] { topicShadowGet, topic }, new byte[]
            { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

        iotClient.Publish(topicShadowUpdate, Encoding.UTF8.GetBytes(message),
             MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE , false);
        int i = 0;
        var random = new Random(Environment.TickCount);
        xbee = new Xbee(AppConstants.COM_PORT);
        xbee.DataReceived += (object sender, Xbee.DataReceivedEventArgs e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            try
            {
                Console.WriteLine(e.Data);
                var filtered = e.Data.Replace("data:", string.Empty);
                if(double.TryParse(filtered,out var nilai))
                {
                    var newItem = new SensorData() { Tanggal = DateTime.Now, WaterDistance = nilai*1000, Humidity = random.Next(10, 100), Temperature = random.Next(28, 38), FlowIn = random.Next(0, 100), FlowOut = random.Next(0, 100) };
                    message = JsonSerializer.Serialize(newItem);
                    iotClient.Publish(topic, Encoding.UTF8.GetBytes($"{message}"));
                    Console.WriteLine($"Published: {message}");
                    i++;
                }
               
                /*
                var obj = JsonSerializer.Deserialize<SensorData>(e.Data);
                if (obj != null)
                {

                }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                //throw;
            }



        };
        Console.ReadLine();
        /*
        while (true)
        {
            var newItem = new SensorData() { Tanggal = DateTime.Now, WaterDistance = random.Next(10, 300), Humidity = random.Next(10, 100), Temperature = random.Next(28, 38), FlowIn = random.Next(0, 100), FlowOut = random.Next(0, 100) };
            message = JsonSerializer.Serialize(newItem);
            iotClient.Publish(topic, Encoding.UTF8.GetBytes($"{message}"));
            Console.WriteLine($"Published: {message}");
            i++;
            Thread.Sleep(5000);
        }*/
    }
  
}

