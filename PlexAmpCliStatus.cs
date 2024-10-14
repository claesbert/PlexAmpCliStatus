using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Linq;
using System.Threading;

class PlexStatusChecker
{
    // Edit these constants
    private const string PLEXIP = "PLEXIPHERE";
    private const string PLEXTOKEN = "PLEXTOKENHERE";

    // Don't touch anything after here
    private static string URL = $"http://{PLEXIP}:32400/status/sessions?X-Plex-Token={PLEXTOKEN}";

    // Function to fetch and parse XML data
    static async Task<string> FetchDataAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                Console.WriteLine($"Fetching data from: {url}");
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error fetching data: {e.Message}");
                Console.ResetColor();
                return null;
            }
        }
    }

    // Function to parse XML data and extract device and track information
    static Dictionary<string, DeviceInfo> ParseXml(string xmlData)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlData);
        XmlNodeList trackNodes = doc.SelectNodes("//Track");

        var devices = new Dictionary<string, DeviceInfo>();

        foreach (XmlNode track in trackNodes)
        {
            XmlNode deviceInfo = track.SelectSingleNode(".//Player");
            if (deviceInfo != null)
            {
                string deviceName = deviceInfo.Attributes["title"].Value;
                string status = deviceInfo.Attributes["state"].Value;

                if (!devices.ContainsKey(deviceName))
                {
                    devices[deviceName] = new DeviceInfo
                    {
                        Status = status,
                        Tracks = new List<TrackInfo>()
                    };
                }

                // Extract track info
                XmlNode media = track.SelectSingleNode(".//Media");
                if (media != null)
                {
                    string album = track.Attributes["parentTitle"]?.Value ?? "Unknown Album";
                    string artist = track.Attributes["grandparentTitle"]?.Value ?? "Unknown Artist";
                    string thumbnailUrl = media.SelectSingleNode(".//thumb")?.InnerText ?? "";

                    // If artist is "Various Artists", use originalTitle instead
                    if (artist == "Various Artists")
                    {
                        artist = track.Attributes["originalTitle"]?.Value ?? "Unknown Artist";
                    }

                    // Compute progress
                    int viewOffset = int.Parse(track.Attributes["viewOffset"]?.Value ?? "0");
                    int duration = int.Parse(media.Attributes["duration"]?.Value ?? "0");
                    double progressPercentage = (duration > 0) ? (viewOffset / (double)duration) * 100 : 0;

                    var trackInfo = new TrackInfo
                    {
                        Album = album,
                        Track = track.Attributes["title"]?.Value ?? "Unknown Track",
                        Artist = artist,
                        Duration = duration,
                        Progress = progressPercentage,
                        Thumbnail = thumbnailUrl
                    };
                    devices[deviceName].Tracks.Add(trackInfo);
                }
            }
        }

        return devices;
    }

    // Function to display device and track information
    static void DisplayInfo(Dictionary<string, DeviceInfo> devices)
    {
        Console.Clear();

        foreach (var device in devices)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Device: {device.Key} (Status: {device.Value.Status.ToUpper()})");
            Console.ResetColor();

            foreach (var track in device.Value.Tracks)
            {
                int minutes = track.Duration / 60000;
                int seconds = (track.Duration % 60000) / 1000;
                string durationStr = $"{minutes}:{seconds:D2}";

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Track: {track.Track}");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"  Artist: {track.Artist}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Album: {track.Album}");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  Duration: {durationStr}");

                if (!string.IsNullOrEmpty(track.Thumbnail))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  Thumbnail: {track.Thumbnail}");
                }

                // Display progress bar
                int progressBarLength = 40;
                int progress = (int)(track.Progress / 100 * progressBarLength);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  Progress: [{new string('#', progress)}{new string('.', progressBarLength - progress)}] {track.Progress:F0}%");
                Console.ResetColor();
            }

            Console.WriteLine();  // Empty line between devices
        }
    }

    // Main loop
    public static async Task Main(string[] args)
    {
        while (true)
        {
            string xmlData = await FetchDataAsync(URL);
            if (!string.IsNullOrEmpty(xmlData))
            {
                var devices = ParseXml(xmlData);
                if (devices.Count > 0)
                {
                    DisplayInfo(devices);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No device info to display.");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to retrieve data.");
                Console.ResetColor();
            }

            await Task.Delay(10000);  // Refresh every 10 seconds
        }
    }
}

// Helper classes for device and track info
public class DeviceInfo
{
    public string Status { get; set; }
    public List<TrackInfo> Tracks { get; set; }
}

public class TrackInfo
{
    public string Album { get; set; }
    public string Track { get; set; }
    public string Artist { get; set; }
    public int Duration { get; set; }
    public double Progress { get; set; }
    public string Thumbnail { get; set; }
}
