using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace wp7_latm_mediastreamsource
{
    public class Rtsp : IDisposable
    {
        private Socket RtspSocket;
        private SocketAsyncEventArgs RtspEvntArgs;
        private string RtspUrl;
        private Uri RtspUri;
        private RtspCommands RtspMessages;
        private Rtp RtpStream;

        private const int RtspPort = 554;
        private const int MaxBufferSize = 4096;

        private enum State
        {
            Connect,
            Setup,
            Describe,
            Play,
            Pause,
            Teardown
        }

        private State CurrentState;
        
        public Rtsp(string url)
        {
            RtspUrl = url;
            RtspUri = new Uri(RtspUrl, UriKind.Absolute);
            RtspMessages = new RtspCommands(RtspUrl);
            RtspSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RtspEvntArgs = new SocketAsyncEventArgs();
            RtpStream = Rtp.GetInstance();
            RtspEvntArgs.RemoteEndPoint = new DnsEndPoint(RtspUri.Host, RtspPort);
            RtspEvntArgs.Completed += RtspEvntArgs_Completed;
            RtspEvntArgs.SetBuffer(0, MaxBufferSize);
            //RtpStream.DeterminePort(new AsyncCallback(CB));
        }

        private void PlayAsyncCallback(IAsyncResult ar)
        {
            Logging.Log((ar.AsyncState).ToString());
            CurrentState = State.Connect;
            RtspSocket.ConnectAsync(RtspEvntArgs);
        }

        void RtspEvntArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    SendMessage(RtspMessages.Describe());
                    CurrentState = State.Describe;
                    break;
                case SocketAsyncOperation.Send:
                    ReceiveMessage();
                    break;
                case SocketAsyncOperation.Receive:
                    string message = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                    Logging.Log(CurrentState.ToString());
                    Logging.Log(message);
                    Match m;
                    switch (CurrentState)
                    {
                        case State.Describe:
                            string ContentBasePattern = "Content-Base:(.*)"+Regex.Escape(Environment.NewLine);
                            string RangePattern = "a=range:(.*)" + Regex.Escape(Environment.NewLine);
                            m = Regex.Match(message,ContentBasePattern);
                            RtspMessages.ContentBase = m.Value.Substring(13, m.Length-15);
                            m = Regex.Match(message, RangePattern);
                            RtspMessages.Range = m.Value.Substring(8, m.Length-11);
                            RtspMessages.Stream = "trackID=1";
                            SendMessage(RtspMessages.Setup(RtpStream.ClientPort));
                            CurrentState = State.Setup;
                            break;
                        case State.Setup:
                            string SessionPattern = "Session:[^;]+;";
                            string SsrcPattern = "ssrc=(.*)" + Regex.Escape(Environment.NewLine);
                            string ServerPortPattern = "server_port=[^;]+;";
                            string ServerIpPattern = "source=[^;]+;";
                            m = Regex.Match(message, SessionPattern);
                            RtspMessages.Session = m.Value.Substring(9,8);
                            m = Regex.Match(message, SsrcPattern);
                            RtspMessages.SSRC = m.Value.Substring(5, 8);
                            m = Regex.Match(message, ServerPortPattern);
                            RtspMessages.ServerPortRtsp = int.Parse(m.Value.Substring(12,5));
                            RtspMessages.ServerPortRtcp = int.Parse(m.Value.Substring(18,5));
                            RtpStream.ServerPort = RtspMessages.ServerPortRtsp;
                            m = Regex.Match(message, ServerIpPattern);
                            RtpStream.ServerIP = m.Value.Substring(7, m.Length - 8);
                            SendMessage(RtspMessages.Play());
                            CurrentState = State.Play;
                            break;
                        case State.Play:
                            RtpStream.StartStream();
                            string RtpSeqPattern = "seq=[^;]+;";
                            string RtpTimePattern = "rtptime=(.*)"+Regex.Escape(Environment.NewLine);
                            m = Regex.Match(message, RtpSeqPattern);
                            RtspMessages.RtpSeq =int.Parse(m.Value.Substring(4, m.Length - 5));
                            m = Regex.Match(message, RtpTimePattern);
                            RtspMessages.RtpTime = int.Parse(m.Value.Substring(8,m.Length-10));
                            
                            break;
                        case State.Teardown:
                            break;
                    }
                    break;
            }
        }

        private void SendMessage(string msg)
        {
            var buffer = Encoding.UTF8.GetBytes(msg + Environment.NewLine);
            RtspEvntArgs.SetBuffer(buffer, 0, buffer.Length);
            RtspSocket.SendAsync(RtspEvntArgs);
        }

        private void ReceiveMessage()
        {
            RtspEvntArgs.SetBuffer(new byte[MaxBufferSize], 0, MaxBufferSize);
            RtspSocket.ReceiveAsync(RtspEvntArgs);
        }

        public void Play()
        {
            CurrentState = State.Connect;
            //RtspSocket.ConnectAsync(RtspEvntArgs);
            RtpStream.DeterminePort(new AsyncCallback(PlayAsyncCallback));
        }

        public void Teardown()
        {
            SendMessage(RtspMessages.Teardown());
            CurrentState = State.Teardown;
            RtpStream.Abort();
        }

        public void Dispose()
        {
            Teardown();
        }
    }
}
