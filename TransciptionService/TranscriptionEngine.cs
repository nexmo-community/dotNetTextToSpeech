using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TransciptionService
{
    public class TranscriptionEngine
    {
        const int SAMPLES_PER_SECOND = 8000;
        const int BITS_PER_SAMPLE = 16;
        const int NUMBER_OF_CHANNELS = 1;

        static SpeechConfig _config = SpeechConfig.FromSubscription("SUBSCRIPTION_KEY", "REGION");
        static PushAudioInputStream _inputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(SAMPLES_PER_SECOND, BITS_PER_SAMPLE, NUMBER_OF_CHANNELS));
        static AudioConfig _audioInput = AudioConfig.FromStreamInput(_inputStream);
        static SpeechRecognizer _recognizer;
        static bool _started = false;

        public static async Task StartSpeechTranscriptionEngine(string language)
        {
            if (!_started)
            {                
                _config.SpeechRecognitionLanguage = language;
                 _recognizer = new SpeechRecognizer(_config, _audioInput);
                _recognizer.Recognized += _recognizer_Recognized;
                await _recognizer.StartContinuousRecognitionAsync();
            }
            _started = true;
        }

        public static async void StopTranscriptionEngine()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            if (_started)
            {
                _recognizer.Recognized -= _recognizer_Recognized;
                await _recognizer.StopContinuousRecognitionAsync();
                _recognizer.Dispose();
            }
            _started = false;
            sw.Stop();

            Debug.WriteLine("Elapsed={0}", sw.Elapsed);
        }

        private static void _recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            Debug.WriteLine("Recognized: " + e.Result.Text);
        }

        public static async Task WriteToTranscriber(HttpContext context, WebSocket webSocket)
        {
            const int BUFFER_SIZE = 160 * 2;
            var buffer = new byte[BUFFER_SIZE];            
            
            try
            {
                var init_buffer = new byte[500];
                var initial_response = await webSocket.ReceiveAsync(new ArraySegment<byte>(init_buffer), CancellationToken.None);
                var converted = Encoding.UTF8.GetString(init_buffer);                
                var initial_object = JObject.Parse(converted);
                string language;
                if (initial_object.ContainsKey("language"))
                {
                    language = (string)initial_object["language"];
                }
                else
                {
                    language = "en-US";
                }

                await StartSpeechTranscriptionEngine(language);
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    _inputStream.Write(buffer);

                    Console.WriteLine(result.Count);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                StopTranscriptionEngine();
            }
        }
    }
}
