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
    }
}
