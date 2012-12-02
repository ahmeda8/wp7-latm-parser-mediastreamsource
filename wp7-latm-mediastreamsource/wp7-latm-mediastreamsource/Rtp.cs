using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

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
        private bool PortDetermined = false;

        private const string PortDetermineServerAddress = "169.254.96.50";
        private const int PortDetermineServerPort = 22222;
        private const int MaxBufferSize = 1024;
        private int CurrentPacketSize;

        private static Rtp instance;

        private enum RtpState
        {
            DeterminePort,
            Stream
        }

        private RtpState CurrentState;

        public static Rtp GetInstance()
        {
            if (instance == null)
                instance = new Rtp();
            return instance;
        }

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
                            PortDetermined = true;
                            break;
                        case RtpState.Stream:
                            if (CurrentPacketSize == 0)
                                CurrentPacketSize = e.BytesTransferred;
                            if(e.BytesTransferred>12) //minimum bytes to accept from rtp packet
                                RtpStream.Write(e.Buffer, 12 , e.BytesTransferred - 12);
                            RtpParser.RTP_HEADER h =  RtpParser.GetHeader(e.Buffer);
                            if (e.BytesTransferred < CurrentPacketSize)
                            {
                                Logging.Log(string.Format("Download Complete {0)",RtpStream.Length));
                            }
                            else
                            {
                                Logging.Log(string.Format("Continue Downloading , stream size={0} , seqno={1}, ssrc={2}, payload type={3},packet_size={4}",RtpStream.Length,h.seq,h.ssrc,h.pt,e.BytesTransferred));
                                Receive();
                            }
                            break;
                    }
                    break;
            }
        }

        public void DeterminePort()
        {
            if (PortDetermined)
                return;
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
            CurrentPacketSize = 0;
            Receive();
        }

        private void Receive()
        {

            try
            {
                RtpEvntArgs.SetBuffer(new byte[MaxBufferSize], 0, MaxBufferSize);
                RtpSocket.ReceiveFromAsync(RtpEvntArgs);
            }
            catch (ObjectDisposedException)
            {
                Logging.Log("Socket Disposed.");
            }
        }

        public void Abort()
        {
            RtpSocket.Close();
            PortDetermined = false;
        }

    }
}
