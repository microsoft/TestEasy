using System;
using System.Net;
using System.Net.Sockets;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstraction to deal with Environment
    /// </summary>
    public class EnvironmentSystem : IEnvironmentSystem
    {
        public string ExpandEnvironmentVariables(string environmentVariable)
        {
            return Environment.ExpandEnvironmentVariables(environmentVariable);
        }

        public int GetNextAvailablePort(int usedPort = 0)
        {
            const int lowerAddressRangeBoundary = 2048;
            const int higherAddressRangeBoundary = 3072;
            var r = new Random(unchecked((int)DateTime.Now.Ticks));
            int port;

            do
            {
                port = r.Next(lowerAddressRangeBoundary, higherAddressRangeBoundary);

            } while (IsLocalTcpPortInUse(port)  || port == usedPort);

            return port;
        }

        public Version OSVersion 
        {
            get
            {
                return Environment.OSVersion.Version;
            }
        }

        public string MachineName
        {
            get { return Environment.MachineName; }
        }

        public bool Is64BitOperatingSystem
        {
            get { return Environment.Is64BitOperatingSystem;  }
        }

        private bool IsLocalTcpPortInUse(int port)
        {
            bool result;

            var ipAddress = Dns.GetHostEntry("127.0.0.1").AddressList[0];
            var ipe = new IPEndPoint(ipAddress, port);
            var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(ipe);
                result = socket.Connected;
            }
            catch (SocketException)
            {
                result = false;
            }
            finally
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }

            return result;
        }
    }
}
