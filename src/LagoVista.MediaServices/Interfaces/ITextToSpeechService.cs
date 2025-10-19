// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 2292e150da94a20cfb930e6c5b4c50111f05c9d63d831ff59e0367934a87e459
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ITextToSpeechService
    {
        Task<InvokeResult<byte[]>> GenerateAudio(TextToSpeechRequest request);

        Task<InvokeResult<List<EntityHeader>>> GetVoicesForLanguageAsync(string languageCode);
    }
}
