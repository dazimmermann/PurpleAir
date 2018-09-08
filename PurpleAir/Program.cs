using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace PurpleAir
{
    class Program
    {
        static int Linear(double AQIhigh, double AQIlow, double Conchigh, double Conclow, double Concentration)
        {
            double a = ((Concentration - Conclow) / (Conchigh - Conclow)) * (AQIhigh - AQIlow) + AQIlow;
            return (int)Math.Round(a);
        }

        static int PM25toAQI(float PM25)
        {
            int AQI = 0;
            double c = (Math.Floor(10 * PM25)) / 10;

            switch (c)
            {
                case double n when (n < 12.1):
                    AQI = Linear(50, 0, 12, 0, c);
                    break;
                case double n when (n < 35.5):
                    AQI = Linear(100, 51, 35.4, 12.1, c);
                    break;
                case double n when (n < 55.5):
                    AQI = Linear(150, 101, 55.4, 35.5, c);
                    break;
                case double n when (n < 150.5):
                    AQI = Linear(200, 151, 150.4, 55.5, c);
                    break;
                case double n when (n < 250.5):
                    AQI = Linear(300, 201, 250.4, 150.5, c);
                    break;
                case double n when (n < 350.5):
                    AQI = Linear(400, 301, 350.4, 250.5, c);
                    break;
                case double n when (n < 500.5):
                    AQI = Linear(500, 401, 500.4, 350.5, c);
                    break;
                default:
                    AQI = -1;
                    break;
            }
            return AQI;
        }

        static string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        static void Main(string[] args)
        {
            try
            {
                //load configuration from configuration file (App.Config in dev, PurpleAir.exe.config in production)
                string json = "";
                string api_url = ConfigurationManager.AppSettings["api_url"];
                string aqi_station = ConfigurationManager.AppSettings["aqi_station"];
                string URL = api_url + aqi_station;
                string datapath = ConfigurationManager.AppSettings["datapath"];
                WebRequest r = WebRequest.Create(URL);

                //call web service
                WebResponse resp = r.GetResponse();
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    json = sr.ReadToEnd();
                }

                //deserialize the json returned
                PurpleAir.PAStation pas = new PurpleAir.PAStation();
                pas = JsonConvert.DeserializeObject<PurpleAir.PAStation>(json);

                //pull the PM2.5 value, and calculate AQI based on algorithm used in EPA's AirNow.gov Concentration to AQI javascript
                //https://www.airnow.gov/js/conc-aqi.js (AQIPM25(f), Linear) converted here to C#

                string PM2_5Value = pas.results[0].PM2_5Value;
                int AQI = PM25toAQI(float.Parse(PM2_5Value));

                //get the station name
                string station = pas.results[0].Label;

                //some station names are very long, and end with _<something> or -<something>, especially CARB locations.  Just use the part after the last _ or -
                Regex pattern = new Regex(@"[_-]");
                Match match = pattern.Match(station);
                if (match.Success)
                {
                    station = ReverseString(station);
                    station = ReverseString(station.Substring(0, Math.Max(station.IndexOf("_"), station.IndexOf("-"))));
                }

                //calculate the local time at which the station was last updated
                //LastSeen is # of seconds since 1/1/1970 00:00
                double LastSeen = pas.results[0].LastSeen;
                bool dst = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now); //determine if it's currently Daylight Savings Time or not
                //get the local time offset, then if DST, add an hour
                double tz = double.Parse(ConfigurationManager.AppSettings["timezone"].ToString());
                if (dst) { tz += 1; }
                //finally, convert the LastSeen # of seconds to the date, and correct for local timezone
                DateTime lastseen = DateTime.Parse("1/1/1970").AddDays(LastSeen / (3600 * 24));
                lastseen = lastseen.AddHours(tz);

                //write a current AQI status file
                StreamWriter file = new StreamWriter(datapath + "AQI.txt");
                file.WriteLine("city=" + station);
                file.WriteLine("time=" + lastseen.ToString("yyyy-MM-dd HH:mm:ss"));
                file.WriteLine("timezone=" + tz.ToString());
                file.WriteLine("aqi=" + AQI.ToString());
                file.WriteLine("pm25=" + PM2_5Value.ToString());
                file.WriteLine("temp=" + pas.results[0].temp_f.ToString());
                file.WriteLine("humidity=" + pas.results[0].humidity.ToString());
                file.WriteLine("pressure=" + pas.results[0].pressure.ToString());
                file.WriteLine("latitude=" + pas.results[0].Lat.ToString());
                file.WriteLine("longitude=" + pas.results[0].Lon.ToString());
                file.WriteLine("PurpleAirID=" + pas.results[0].ID.ToString());
                file.Close();
                file.Dispose();

                //write the data into comma-separated format into one log file each month (e.g. AQI-Log-2018-08.txt)
                string logfile = datapath + "AQI-Log-" + lastseen.ToString("yyyy-MM") + ".txt";

                //if the file doesn't exist for the month of the data, create a new one and create the field header
                if (!File.Exists(logfile))
                {
                    StreamWriter file2 = new StreamWriter(datapath + "AQI-History.txt");
                    file2.WriteLine("\"city\",\"time\",\"timezone\",\"aqi\",\"pm25\",\"temp\",\"humidity\",\"pressure\",\"latitude\",\"longitude\",\"PurpleAirID\"");
                    file2.Close();
                    file2.Dispose();
                }

                //at this point, the file already exists, so just append the new data.
                using (StreamWriter history = File.AppendText(logfile))
                {
                    history.WriteLine("\"" + station + "\",\"" + lastseen.ToString("yyyy-MM-dd HH:mm:ss") + "\"," + tz.ToString() + "," + AQI.ToString() + "," + PM2_5Value.ToString() + "," + pas.results[0].temp_f.ToString() + "," + pas.results[0].pressure.ToString() + "," + pas.results[0].Lat.ToString() + "," + pas.results[0].Lon.ToString() + ",\"" + pas.results[0].ID.ToString() + "\"");
                    history.Close();
                    history.Dispose();
                }
            }
            catch (Exception e)
            {
                //if anything goes wrong either with calling the API site or writing files, tell the user what happened, and exit with a non-zero error code
                Console.WriteLine(e.Message.ToString());
                Environment.Exit(-1);
            }
        }
    }
}