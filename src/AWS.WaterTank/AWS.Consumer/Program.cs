﻿using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AWS.Consumer;
class Program
{
    private static ManualResetEvent manualResetEvent;

    static void Main(string[] args)
    {
        var iotEndPoint = "a2teks7xu15e4c-ats.iot.ap-southeast-1.amazonaws.com";
        var iotPort = 8883;

        Console.WriteLine("AWS IoT dotnetcore message consumer starting..");
        var rootCert = Path.Join(AppContext.BaseDirectory, "AmazonRootCA1.crt");
        var clientcert = Path.Join(AppContext.BaseDirectory, "certificate.cert.pfx");
        var privatecert = Path.Join(AppContext.BaseDirectory, "water-monitor-gateway.private.key");
        var caCertSource = File.ReadAllBytes(rootCert);//UTF8Encoding.UTF8.GetBytes("Need AWS root CA certificate");
        var clientCertSource = File.ReadAllBytes(clientcert); //UTF8Encoding.UTF8.GetBytes("Need your AWS client CA certificate");
        var privateKeyData = File.ReadAllBytes(privatecert);//UTF8Encoding.UTF8.GetBytes("Need your AWS private key");
        X509Certificate CaCert = new X509Certificate(caCertSource);
        X509Certificate ClientCert = new X509Certificate2(clientCertSource, "123qweasd");

        var client = new MqttClient(iotEndPoint, iotPort, true, CaCert, ClientCert, MqttSslProtocols.TLSv1_2);


        client.MqttMsgSubscribed += IotClient_MqttMsgSubscribed;
        client.MqttMsgPublishReceived += IotClient_MqttMsgPublishReceived;

        string clientId = Guid.NewGuid().ToString();
        client.Connect(clientId);
        Console.WriteLine($"Connected to AWS IoT with client ID: {clientId}");

        string topic = "Hello/World";
        Console.WriteLine($"Subscribing to topic: {topic}");
        client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

        // Keep the main thread alive for the event receivers to get invoked
        KeepConsoleAppRunning(() =>
        {
            client.Disconnect();
            Console.WriteLine("Disconnecting client..");
        });
    }

    private static void IotClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        Console.WriteLine("Message received: " + Encoding.UTF8.GetString(e.Message));
    }

    private static void IotClient_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
    {
        Console.WriteLine($"Successfully subscribed to the AWS IoT topic.");
    }

    private static void KeepConsoleAppRunning(Action onShutdown)
    {
        manualResetEvent = new ManualResetEvent(false);
        Console.WriteLine("Press CTRL + C or CTRL + Break to exit...");

        Console.CancelKeyPress += (sender, e) =>
        {
            onShutdown();
            e.Cancel = true;
            manualResetEvent.Set();
        };

        manualResetEvent.WaitOne();
    }
}