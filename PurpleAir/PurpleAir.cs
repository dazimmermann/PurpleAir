using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurpleAir
{
    class PurpleAir
    {
        //based on JSON2CSharp.com generated class (not QuickType)
        public class Result
        {
            public int ID { get; set; }
            public int? ParentID { get; set; }
            public string Label { get; set; }
            public string DEVICE_LOCATIONTYPE { get; set; }
            public string THINGSPEAK_PRIMARY_ID { get; set; }
            public string THINGSPEAK_PRIMARY_ID_READ_KEY { get; set; }
            public string THINGSPEAK_SECONDARY_ID { get; set; }
            public string THINGSPEAK_SECONDARY_ID_READ_KEY { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string PM2_5Value { get; set; }
            public int LastSeen { get; set; }
            public object State { get; set; }
            public string Type { get; set; }
            public string Hidden { get; set; }
            public object Flag { get; set; }
            public string DEVICE_BRIGHTNESS { get; set; }
            public string DEVICE_HARDWAREDISCOVERED { get; set; }
            public object DEVICE_FIRMWAREVERSION { get; set; }
            public string Version { get; set; }
            public int? LastUpdateCheck { get; set; }
            public string Uptime { get; set; }
            public string RSSI { get; set; }
            public int isOwner { get; set; }
            public object A_H { get; set; }
            public string temp_f { get; set; }
            public string humidity { get; set; }
            public string pressure { get; set; }
            public int AGE { get; set; }
            public string Stats { get; set; }
        }

        public class PAStation  //renamed RootObject to something more appropriate
        {
            public double mapVersion { get; set; } //10-15-2018 - fixed json parse error (map version changed from int to double)
            public int baseVersion { get; set; }
            public string mapVersionString { get; set; }
            public List<Result> results { get; set; }
        }
    }
}
