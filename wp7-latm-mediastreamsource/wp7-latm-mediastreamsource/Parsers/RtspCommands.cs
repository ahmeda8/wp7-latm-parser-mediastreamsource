using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wp7_latm_mediastreamsource
{
    class RtspCommands
    {
        private const string RtspVersion = "RTSP/1.0";
        private string SourceUrl;
        private int Cseq;

        public string ContentBase { get; set; } //from describe frame
        public string Stream { get; set; } //from describe frame
        public string Range { get; set; } //from describe fram
        public string Session { get; set; } //from setup frame
        public string SSRC { get; set; } //from setup frame
        public int ServerPortRtsp { get; set; } //from setup frame
        public int ServerPortRtcp { get; set; } //from setup frame
        public int RtpSeq { get; set; } //from play frame
        public int RtpTime { get; set; } //from play frame


        public RtspCommands(string Url)
        {
            SourceUrl = Url;
            Cseq = 0;
        }

        public string Describe()
        {
            string command = "DESCRIBE " + SourceUrl +" "+ RtspVersion +Environment.NewLine
                           + "CSeq: " + Cseq++ + Environment.NewLine;
            return command;
        }
                
        public string Setup(int ClientPort)
        {

            string command = "SETUP " + ContentBase + Stream +" "+RtspVersion + Environment.NewLine 
                            +"Transport: RTP/AVP/UDP;unicast;client_port=" + ClientPort + "-" + (ClientPort + 1) + ";mode=play" + Environment.NewLine 
                            +"CSeq: " + Cseq++ + Environment.NewLine;
            return command;
        }

        public string Play()
        {

            string command = "PLAY " + ContentBase + Stream +" "+ RtspVersion + Environment.NewLine 
                            +"Range:" + Range + Environment.NewLine
                            +"Session:" + Session + Environment.NewLine
                            +"CSeq: " + Cseq++ + Environment.NewLine;
            return command;
        }

        public string Teardown()
        {
            string command = "TEARDOWN " + SourceUrl +" "+ RtspVersion + Environment.NewLine
                            +"Session:" + Session + Environment.NewLine
                            +"CSeq: " + Cseq++ + Environment.NewLine;
            return command;
        }
    }
}
