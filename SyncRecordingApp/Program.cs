/// <summary>
/// Sync Recording App
/// This is an application to syncronize recording commands between Optitrack Motive and Rokoko Studio
/// github repository - https://github.com/Neill3d/SyncRecordingApp
/// Developed by Sergei <Neill3d> Solokhin 2022
/// </summary>
/// 

using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

// NOTE: about Optitrack Motive XML broadcasting
// When triggering via XML messages, the Remote Trigger setting under Advanced Network Settings must be set to true.

// NOTE: about Rokoko Studio
// Command API have to be enabled and a key value have to be 1234

namespace SyncRecordingApp
{
    
    [Serializable]
    public class ResponseMessage
    {
        public string description;
        public string response_code;
        public long startTime;
    }

    internal class Program
    {
        public const string VERSION_LEGACY = "v1";
        public const string VERSION_BETA = "v2";
        public const int COMMAND_API_DEFAULT_PORT = 14053;
        public const string API_KEY = "1234";

        public const string HEADER_INFO = @"Commands: 
        x to start recording
        z to stop recording 
        c to calibrate 
        r to toggle receive xml commands
        s to toggle send xml commands
        v to toggle verbose
        q to exit";

        public static int commandAPIPort = COMMAND_API_DEFAULT_PORT;
        public static string commandAPIKey = API_KEY;

        // This is the port Motive is sending/listening commands
        public const int PORT_COMMAND_XML2 = 1510;
        public const int PORT_COMMAND_XML = 1512;

        public static int portCommandXML = PORT_COMMAND_XML;
        private static bool sendXMLCommands = true;
        private static bool receiveXMLCommands = true;
        private static bool verbose = true;

        private const string MOTIVE_CAPTURE_START_COMMAND = "CaptureStart";
        private const string MOTIVE_CAPTURE_STOP_COMMAND = "CaptureStop";

        private static float frameRate = 30.0f;

        private static bool isRunning = true;
        private static int processID = 0;

        private static string lastRecordingName = "";

        static void Main(string[] args)
        {
            IniParser settings = new IniParser("settings.ini");
            
            if (settings.isEmpty)
            {
                settings.AddSetting("XML Commands", "Do Send", true);
                settings.AddSetting("XML Commands", "Do Receive", true);
                settings.AddSetting("XML Commands", "Port", PORT_COMMAND_XML);

                settings.AddSetting("Command API", "Port", COMMAND_API_DEFAULT_PORT);
                settings.AddSetting("Command API", "Key", API_KEY);
                settings.SaveSettings();
            }

            sendXMLCommands = settings.GetSetting("XML Commands", "Do Send", true);
            receiveXMLCommands = settings.GetSetting("XML Commands", "Do Receive", true);
            portCommandXML = settings.GetSetting("XML Commands", "Port", PORT_COMMAND_XML);

            commandAPIPort = settings.GetSetting("Command API", "Port", COMMAND_API_DEFAULT_PORT);
            commandAPIKey = settings.GetSetting("Command API", "Key", API_KEY);

            processID = Process.GetCurrentProcess().Id;
            Console.WriteLine($"Current Process ID {processID}");

            Console.WriteLine(HEADER_INFO);

            // http client to communicate with Rokoko Studio

            HttpClient httpClient = CreateClient(commandAPIPort, "127.0.0.1"); // NOTE: localhost is slow due to attemp to connect to IPv6 localhost first

            // start UDP server to communicate with Optitrack Motive

            UdpClient udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, portCommandXML));
            
            if (receiveXMLCommands)
            {
                StartUdpClientListening(httpClient, udpClient);
            }
            
            while (isRunning)
            {
                var keyInfo = Console.ReadKey();
                Console.Write("\b ");
                switch(keyInfo.KeyChar)
                {
                    case 'x':
                        Console.Write("Enter a clip name: ");
                        lastRecordingName = Console.ReadLine();
                        if (lastRecordingName != "")
                        {
                            SendRecordingCommand(true, lastRecordingName, "0:0:0:0", frameRate, "", httpClient, sendXMLCommands ? udpClient : null);
                        }
                        break;
                    case 'z':
                        SendRecordingCommand(false, lastRecordingName, "0:0:0:0", frameRate, "", httpClient, sendXMLCommands ? udpClient : null);
                        break;
                    case 'c':
                        SendCalibrateCommand(httpClient);
                        break;
                    case 's':
                        sendXMLCommands = !sendXMLCommands;
                        Console.WriteLine($"Send XML Commands Mode: {sendXMLCommands}");
                        break;
                    case 'r':
                        receiveXMLCommands = !receiveXMLCommands;
                        Console.WriteLine($"Receive XML Commands Mode: {receiveXMLCommands}");
                        break;
                    case 'v':
                        verbose = !verbose;
                        Console.WriteLine($"Verbose Mode: {verbose}");
                        break;
                    case 'q':
                        isRunning = false;
                        break;
                }
            }

            httpClient.Dispose();

            Console.WriteLine("Finish");
        }

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static async void StartUdpClientListening(HttpClient httpClient, UdpClient udpClient)
        {
            var from = new IPEndPoint(IPAddress.Any, portCommandXML - 2);

            await Task.Run(() =>
            {
                while (isRunning)
                {
                    var recvBuffer = udpClient.Receive(ref from);
                    DateTime dateTime = DateTime.Now;
                    string xmlData = Encoding.UTF8.GetString(recvBuffer);
                    
                    if (verbose)
                        Console.WriteLine($"[{dateTime}] UDP Received: {xmlData}");
                    
                    try
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(xmlData);

                        bool isStartRecording = true;
                        XmlNode node = xml.SelectSingleNode($"{MOTIVE_CAPTURE_START_COMMAND}/ProcessID");
                        
                        if (node == null)
                        {
                            isStartRecording = false;
                            node = xml.SelectSingleNode($"{MOTIVE_CAPTURE_STOP_COMMAND}/ProcessID");
                        }
                        
                        // don't evaluate self broadcated packets
                        if (node != null 
                            && int.TryParse(node.Attributes["VALUE"].Value, out int pid)
                            && pid != processID)
                        {
                            string captureCommand = isStartRecording ? MOTIVE_CAPTURE_START_COMMAND : MOTIVE_CAPTURE_STOP_COMMAND;
                            node = xml.SelectSingleNode($"{captureCommand}/Name");
                            string recordingName = (node != null) ? node.Attributes["VALUE"].Value : "Clip001";

                            node = xml.SelectSingleNode($"{captureCommand}/TimeCode");
                            string timeCode = (node != null) ? node.Attributes["VALUE"].Value : "0:0:0:0";

                            SendRecordingCommand(isStart: isStartRecording, recordingName, timeCode, frameRate, "", httpClient, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{dateTime}] unpack xml responce - {ex.Message}");
                    }
                }
            });
        }

        private static string GenerateXMLCommand(string commandName, string recordingName, string timecode, string notes, string description, string databasePath, int packetID, int processID)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"<{commandName}>")
                .Append($"<TimeCode VALUE=\"{timecode}\"/>")
                .Append($"<Name VALUE=\"{recordingName}\"/>")
                .Append($"<Notes VALUE=\"{notes}\"/>")
                .Append($"<Description VALUE=\"{description}\"/>")
                .Append($"<DatabasePath VALUE=\"{databasePath}\"/>")
                .Append($"<PacketID VALUE=\"{packetID}\"/>")
                .Append($"<ProcessID VALUE=\"{processID}\"/>")
                .Append($"</{commandName}>");

            return sb.ToString();
        }

        private static async Task SendCalibrateCommand(HttpClient httpClient)
        {
            DateTime dateTime = DateTime.Now;
            Console.WriteLine($"[{dateTime}] calibration request - Send A Command to Studio");

            string requestUri = $"{GetApiBaseRoute()}/calibrate";
            HttpResponseMessage responseMessage = null;
            CommandAPICalibrationInput input = new CommandAPICalibrationInput()
            {
                deviceID = "",
                countdownDelay = 1
            };

            string jsonCommandInput = JsonConvert.SerializeObject(input);
            byte[] inputData = Encoding.ASCII.GetBytes(jsonCommandInput);
            HttpContent httpContent = new ByteArrayContent(inputData);

            try
            {
                // send to Rokoko Studio
                if (httpClient != null)
                {
                    responseMessage = await httpClient.PostAsync(requestUri, httpContent);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{dateTime}] calibration responce - {e.Message}");
            }

            UnpackHttpResponce(responseMessage);
        }

        private static async Task SendRecordingCommand(bool isStart, string recordingName, string timeCode, float frameRate, string actorName, HttpClient httpClient, UdpClient udpClient)
        {
            DateTime dateTime = DateTime.Now;
            Console.WriteLine($"[{dateTime}] request - Send A Command to Studio");

            Stopwatch stopwatch = Stopwatch.StartNew();

            HttpResponseMessage responseMessage = null;
            CommandAPIRecordingInput input = new CommandAPIRecordingInput()
            {
                filename = recordingName,
                time = timeCode,
                frameRate = frameRate
            };
            
            string jsonCommandInput =  JsonConvert.SerializeObject(input);
            byte[] inputData = Encoding.ASCII.GetBytes(jsonCommandInput);
            HttpContent httpContent = new ByteArrayContent(inputData);

            string commandName = isStart ? "start" : "stop";
            string requestUri = $"{GetApiBaseRoute()}/recording/{commandName}";
            var xmlData = Encoding.UTF8.GetBytes(GenerateXMLCommand(isStart ? MOTIVE_CAPTURE_START_COMMAND : MOTIVE_CAPTURE_STOP_COMMAND, 
                recordingName, timeCode, "", "", "", 0, processID));
            int bytesSend = 0;

            try
            {
                // send to Optitrack Motive
                if (udpClient != null)
                {
                    bytesSend = udpClient.Send(xmlData, xmlData.Length, "255.255.255.255", portCommandXML);
                    bytesSend = udpClient.Send(xmlData, xmlData.Length, "255.255.255.255", portCommandXML - 2);
                    if (verbose)
                        Console.WriteLine($"[{dateTime}] udp bytes send {bytesSend}");
                }
                // send to Rokoko Studio
                if (httpClient != null)
                {
                    responseMessage = await httpClient.PostAsync(requestUri, httpContent);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{dateTime}] responce - {e.Message}");
            }

            UnpackHttpResponce(responseMessage);

            stopwatch.Stop();
            if (verbose)
            {
                Console.WriteLine($"Elapsed ms - {stopwatch.ElapsedMilliseconds}");
            }
        }

        private static void UnpackHttpResponce(HttpResponseMessage responseMessage)
        {
            if (responseMessage == null)
                return;

            byte[] data = responseMessage.Content.ReadAsByteArrayAsync().Result;
            string json = Encoding.ASCII.GetString(data);
            ResponseMessage message = JsonConvert.DeserializeObject<ResponseMessage>(json);

            if (message != null)
            {
                Console.WriteLine("Response code - " + message.response_code);
                Console.WriteLine("Description - " + message.description);
                Console.WriteLine($"Responce Start time - {message.startTime}");
            }
            else
            {
                Console.WriteLine("No Responce message deserialized");
            }
        }

        private static string GetApiBaseRoute()
        {
            return $"{VERSION_LEGACY}/{commandAPIKey}";
        }

        private static HttpClient CreateClient(int port, string url)
        {
            HttpClient client = new HttpClient(new HttpClientHandler
            {
                UseProxy = false
            });

            client.BaseAddress = new Uri($"http://{url}:{port}/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
