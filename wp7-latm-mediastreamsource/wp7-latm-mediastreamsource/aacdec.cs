using System;

namespace wp7_latm_mediastreamsource
{
    public class aacdec
    {
        const int LOAS_SYNC_WORD = 0x2b7;

        struct LATMContext
        {
            int initialized; //initialized after a valid extradata was seen
            
            //parser data
            int audio_mux_version_A; //latm syntax version
            int frame_length_type; //0/1 variable/fixed frame length
            int frame_length;
        }
    }
}
