using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace wp7_latm_mediastreamsource
{
    public class Rtp
    {
        private Socket RtpSocket;
        private SocketAsyncEventArgs RtpEvntArgs;
        public int ServerPort {get;set;}
        public string ServerIP {get;set;}
        public int ClientPort { get; set; }
        public MemoryStream RtpStream;

        private const string PortDetermineServerAddress = "169.254.96.50";
        private const int PortDetermineServerPort = 22222;
        private const int MaxBufferSize = 1024;

        private enum RtpState
        {
            DeterminePort,
            Stream
        }

        private RtpState CurrentState;

        public Rtp()
        {
            RtpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            RtpEvntArgs = new SocketAsyncEventArgs();
            RtpEvntArgs.Completed += RtpEvntArgs_Completed;
        }

        void RtpEvntArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.SendTo:
                    Receive();
                    break;
                case SocketAsyncOperation.ReceiveFrom:
                    switch (CurrentState)
                    {
                        case RtpState.DeterminePort:
                            string message = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                            ClientPort = int.Parse(message);
                            break;
                        case RtpState.Stream:
                            RtpStream.Write(e.Buffer, 11, e.BytesTransferred - 12);
                            //RtpParser.RTP_HEADER h =  RtpParser.GetHeader(e.Buffer);
                            Receive();
                            break;

                    }
                    break;
            }
        }

        public void DeterminePort()
        {
            RtpEvntArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(PortDetermineServerAddress), PortDetermineServerPort);
            var send_buffer = Encoding.UTF8.GetBytes("Connect;LoopBack;");
            RtpEvntArgs.SetBuffer(send_buffer, 0, send_buffer.Length);
            CurrentState = RtpState.DeterminePort;
            RtpSocket.SendToAsync(RtpEvntArgs);
        }

        public void StartStream()
        {
            RtpStream = new MemoryStream();
            RtpEvntArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);
            CurrentState = RtpState.Stream;
            Receive();
        }

        private void Receive()
        {
            RtpEvntArgs.SetBuffer(new byte[MaxBufferSize], 0, MaxBufferSize);
            RtpSocket.ReceiveFromAsync(RtpEvntArgs);
        }

    }
}
