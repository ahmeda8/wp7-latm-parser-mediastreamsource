using System;
using System.Collections;

namespace wp7_latm_mediastreamsource
{
    public class RtpParser 
    {
        public struct RTP_HEADER
        {
            public byte version;
            public byte p;
            public byte x;
            public byte cc;
            public byte m;
            public byte pt;
            public UInt16 seq;
            public UInt32 ts;
            public UInt32 ssrc;
            public UInt32[] csrc;
        }

        public static RTP_HEADER GetHeader(byte[] RtpPacket)
        {
            RTP_HEADER header = new RTP_HEADER();
            header.version = (byte)(RtpPacket[0] & BitMasks.VERSION_MASK);
            header.version = (byte)(header.version >> 6);
            header.p = (byte)(RtpPacket[0] & BitMasks.PADDING_MASK);
            header.p = (byte)(header.p >> 5);
            header.x = (byte)(RtpPacket[0] & BitMasks.EXTENSION_MASK);
            header.x = (byte)(header.p >> 4);
            header.cc = (byte)(RtpPacket[0] & BitMasks.CC_MASK);
            header.m = (byte)(RtpPacket[1] & BitMasks.MARKER);
            header.m = (byte)(header.m >> 7);
            header.pt = (byte)(RtpPacket[1] & BitMasks.PT_MASK);
            header.seq = RtpPacket[2];
            header.seq = (UInt16)(header.seq << 8);
            header.seq = (UInt16)(header.seq | RtpPacket[3]);

            int buffer_progress = 4;
            header.ts = RtpPacket[buffer_progress++];
            int next_start = buffer_progress;
            int next_stop = buffer_progress + 3;
            for (int i = next_start; i < next_stop; i++)
            {
                header.ts = header.ts << 8;
                header.ts = header.ts | RtpPacket[i];
                buffer_progress++;
            }

            header.ssrc = RtpPacket[buffer_progress++];
            next_start = buffer_progress;
            next_stop = buffer_progress + 3;
            for (int i = next_start; i < next_stop; i++)
            {
                header.ssrc = header.ssrc << 8;
                header.ssrc = header.ssrc | RtpPacket[i];
                buffer_progress++;
            }

            header.csrc = new UInt32[header.cc];
            for (int i = 0; i < header.cc; i++)
            {
                header.csrc[i] = RtpPacket[buffer_progress++];
                next_start = buffer_progress;
                next_stop = buffer_progress + 3;
                for (int j = next_start; j < next_stop; j++)
                {
                    header.ssrc = header.ssrc << 8;
                    header.ssrc = header.ssrc | RtpPacket[j];
                    buffer_progress++;
                }

            }
               
            return header;
        }
    }
}
