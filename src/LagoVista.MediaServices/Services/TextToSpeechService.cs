// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: caf31c4d44722dadd72b58d87a51122483ab9fad4f933ef43937905b4245f958
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var response = await client.PostAsJsonAsync(url, googleRequest);
            if (!response.IsSuccessStatusCode)
                return InvokeResult<byte[]>.FromError($"Could not request audio file: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsAsync<GoogleTextSpeechResponse>();

            var buffer = System.Convert.FromBase64String(responseContent.B64AudioContent);
            return InvokeResult<byte[]>.Create(buffer);
        }

        public async Task<InvokeResult<List<EntityHeader>>> GetVoicesForLanguageAsync(string languageCode)
        {
            var url = "https://texttospeech.googleapis.com/v1/voices";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Goog-Api-Key", _settings.GoogleTextToSpeechAPIKey);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return InvokeResult<List<EntityHeader>>.FromError($"Could not get voices: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsAsync<GoogleTextToSpeechVoicesResponse>();
            var voices = responseContent.Voices.Where(vc => vc.LanguageCodes.Contains(languageCode)).Select(vc => EntityHeader.Create(vc.Name, vc.SSMLGender, vc.Name)).ToList();
            return InvokeResult<List<EntityHeader>>.Create(voices);
        }
    }
}
