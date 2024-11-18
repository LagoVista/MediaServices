using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Services
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly IMediaServicesConnectionSettings _settings;

        public TextToSpeechService(IMediaServicesConnectionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (String.IsNullOrEmpty(settings.GoogleTextToSpeechAPIKey))
                throw new ArgumentNullException("Missing setting GoogleApiKeys__TextToSpeech");
        }

        public async Task<InvokeResult<byte[]>> GenerateAudio(TextToSpeechRequest request)
        {
            

            var url = "https://texttospeech.googleapis.com/v1/text:synthesize";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Goog-Api-Key", _settings.GoogleTextToSpeechAPIKey);
            client.DefaultRequestHeaders.Add("ContentType", "application/json; charset=utf-8");

            var googleRequest = new GoogleTextToSpeech();
            googleRequest.Input.Text = request.Text;
            googleRequest.Input.SSML = request.Ssml;
            googleRequest.Voice.SsmlGender = request.Gender;
            googleRequest.Voice.LanguageCode = request.Language;
            googleRequest.Voice.Name = request.Voice;


            Console.WriteLine(JsonConvert.SerializeObject(googleRequest));

            var response = await client.PostAsJsonAsync(url, googleRequest);
            if (!response.IsSuccessStatusCode)
                return InvokeResult<byte[]>.FromError($"Could not request audio file: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsAsync<Response>();

            var buffer = System.Convert.FromBase64String(responseContent.B64AudioContent);
            return InvokeResult<byte[]>.Create(buffer);
        }

    }
}
