using LagoVista.Core.Interfaces;
using LagoVista.Core.Validation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
   public interface IMediaServicesRepo
    {

        Task<InvokeResult> AddMediaAsync(byte[] data, string org, string fileName, string contentType);
        Task<InvokeResult<byte[]>> GetMediaAsync(string id, string org);
    }

    public interface IMediaServicesConnectionSettings
    {
        IConnectionSettings DeviceTypeResourceMediaConnection { get; }
    }
}
