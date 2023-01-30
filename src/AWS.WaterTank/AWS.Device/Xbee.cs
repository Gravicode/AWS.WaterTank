using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Device
{
    public class Xbee
    {
        public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
        public class DataReceivedEventArgs : EventArgs
        {
            public string Data;
        }
        public event DataReceivedEventHandler DataReceived;

        bool IsInit = false;

        SerialPort uart;
        public Xbee(string ComPort = "COM7")
        {
            try
            {
                uart = new SerialPort(ComPort, 9600, Parity.None, 8, StopBits.One);
                uart.DataReceived += Uart_DataReceived;

                // if dealing with massive data input, increase the buffer size
                this.uart.ReadBufferSize = 2048;
                uart.Open();

               
                IsInit = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                IsInit = false;
            }




        }

        public bool SendMessage(string Message)
        {
            try
            {
                if (IsInit)
                {
                    uart.WriteLine(Message);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
          
        }

        void Uart_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string dataStr = sp.ReadLine();
            Console.WriteLine("Data Received:");
            Console.Write(dataStr);
            DataReceived?.Invoke(this, new DataReceivedEventArgs() { Data = dataStr });

        }
    }
}
