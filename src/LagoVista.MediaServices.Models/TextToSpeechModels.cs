// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9ee3a93c1ba9ffc8e17d6f0c93df05118123c020531a3d1eb1397c1e1333fa3f
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class GoogleTextToSpeech
    {
        [JsonProperty("input")]
        public GoogleTextToSpeechInput Input { get; set; } = new GoogleTextToSpeechInput();

        [JsonProperty("voice")]
        public GoogleTextToSpeechVoice Voice { get; set; } = new GoogleTextToSpeechVoice();

        [JsonProperty("audioConfig")]
        public GoogleTextToSpeechAudioConfig AudioConfig { get; set; } = new GoogleTextToSpeechAudioConfig();
    }

    public class GoogleTextToSpeechInput
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("ssml")]
        public string SSML { get; set; }
    }

    public class GoogleTextToSpeechVoice
    {
        [JsonProperty("languageCode")]
        public string LanguageCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ssmlGender")]
        public string SsmlGender { get; set; }
    }

    public class GoogleTextToSpeechAudioConfig
    {
        [JsonProperty("audioEncoding")]
        public string AudioEncoding { get; set; } = "MP3";
    }

    public class GoogleTextSpeechResponse
    {
        [JsonProperty("audioContent")]
        public string B64AudioContent { get; set; }
    }

    public class TextToSpeechRequest
    {
        public string Name { get; set; }

        public EntityHeader Library { get; set; }
        public string Key { get; set; }
        public string Text { get; set; }
        public string Ssml { get; set; }
        public string Language { get; set; }
        public string Gender { get; set; }
        public string Voice { get; set; }
    }

    public class GoogleTextToSpeechVoicesResponse
    {
        [JsonProperty("voices")]
        public List<GoogleTextToSpeechVoiceResponse> Voices { get; set; }
    }

    public class GoogleTextToSpeechVoiceResponse
    {
        [JsonProperty("languageCodes")]
        public List<string> LanguageCodes { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ssmlGender")]
        public string SSMLGender { get; set; }
    }

}
