using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NFCRing.Service.Common
{
    public class ServiceCommunication
    {
        public static string ReadNetworkMessage(ref TcpClient client)
        {
            if (client == null)
            {
                return "";
            }
            if (!client.Connected)
            {
                return "";
            }
            byte[] buffer = new byte[10000];
            try
            {
                int len = client.GetStream().Read(buffer, 0, buffer.Length);
                byte[] shortBuffer = new byte[len];
                Array.Copy(buffer, shortBuffer, len);
                return Encoding.UTF8.GetString(shortBuffer);
            }
            catch
            {
                return "";
            }
        }
        public static int SendNetworkMessage(ref TcpClient client, string message)
        {
            if (client == null)
            {
                client = new TcpClient();
            }
            try
            {
                if (!client.Connected)
                {
                    client.Connect(NetworkSettings.ServiceHost, NetworkSettings.RegistrationPort);
                }
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                client.GetStream().Write(messageBytes, 0, messageBytes.Length);
                return messageBytes.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
