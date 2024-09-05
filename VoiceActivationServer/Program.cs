using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;

namespace VoiceActivationServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Configure WebSocket options
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2),  // Send ping frames every 2 minutes
                AllowedOrigins = { "*" } // { "https://localhost:7014" }  // For Dev, its *, for prod to be adjusted to allowed client origins 
            };

            app.UseWebSockets(webSocketOptions); 

            // WebSocket handling middleware
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Handle(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }
            });            
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        static async Task Handle(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            using var audioStream = new MemoryStream();

            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.Count > 0)
                {
                    // Write the received audio to the MemoryStream
                    audioStream.Write(buffer, 0, result.Count);
                }
            } while (!result.CloseStatus.HasValue);

            //await SaveAudioStreamToFile(audioStream, "received_audio.raw");
            //audioStream.Position = 0;


            // Perform keyword recognition using the Speech SDK
            string recognitionResult = await PerformKeywordRecognition(audioStream);
            
            var responseMessage = Encoding.UTF8.GetBytes(recognitionResult);
            await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        // Keyword recognition logic using Speech SDK
        static async Task<string> PerformKeywordRecognition(Stream audioStream)
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "VoiceModels", "hellodoctor.table");

            var keywordModel = KeywordRecognitionModel.FromFile(modelPath);
            using var audioInput = AudioConfig.FromStreamInput(new PullAudioInputStream
                (new BinaryAudioStreamReader(audioStream)));
            using var keywordRecognizer = new KeywordRecognizer(audioInput);

            try
            {
                var result = await keywordRecognizer.RecognizeOnceAsync(keywordModel);
                if (result.Reason == ResultReason.RecognizedKeyword)
                {
                    return $"Keyword recognized: {result.Text}";
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    return $"Recognition canceled: {cancellation.Reason}. Error details: {cancellation.ErrorDetails}";
                }
                else
                {
                    return "Keyword not recognized.";
                }
            }
            catch (Exception ex)
            {
                return $"Error during recognition: {ex.Message}";
            }

        }

        static async Task SaveAudioStreamToFile(Stream audioStream, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {                
                await audioStream.CopyToAsync(fileStream);
            }
        }


        // Stream reader class to provide audio stream input
        public class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _audioStream;

            public BinaryAudioStreamReader(Stream stream)
            {
                _audioStream = stream;
            }

            public override int Read(byte[] dataBuffer, uint size)
            {
                return _audioStream.Read(dataBuffer, 0, (int)size);
            }
        }

    }
}
