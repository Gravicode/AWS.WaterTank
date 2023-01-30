using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AWS.Device;
class Program
{
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
        message = "Test message";
        while (true)
        {
            iotClient.Publish(topic, Encoding.UTF8.GetBytes($"{message} {i}"));
            Console.WriteLine($"Published: {message} {i}");
            i++;
            Thread.Sleep(5000);
        }

    }
}

