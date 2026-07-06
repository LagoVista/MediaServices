using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class LagoVistaIconGenerationManager : ILagoVistaIconGenerationManager
    {
        private readonly ILagoVistaIconStyleProfileProvider _styleProfileProvider;
        private readonly ILagoVistaIconPromptBuilder _promptBuilder;
        private readonly ILagoVistaIconAssetPublisher _assetPublisher;
        private readonly List<ILagoVistaIconImageGenerator> _imageGenerators;

        public LagoVistaIconGenerationManager(ILagoVistaIconStyleProfileProvider styleProfileProvider, ILagoVistaIconPromptBuilder promptBuilder, ILagoVistaIconAssetPublisher assetPublisher, IEnumerable<ILagoVistaIconImageGenerator> imageGenerators)
        {
            _styleProfileProvider = styleProfileProvider ?? throw new ArgumentNullException(nameof(styleProfileProvider));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _assetPublisher = assetPublisher ?? throw new ArgumentNullException(nameof(assetPublisher));
            _imageGenerators = imageGenerators?.ToList() ?? new List<ILagoVistaIconImageGenerator>();
        }

        public InvokeResult<string> BuildPrompt(LagoVistaIconGenerationRequest request)
        {
            if (request == null)
                return InvokeResult<string>.FromError("LagoVista icon generation request is required.");

            var profileResult = _styleProfileProvider.GetProfile(request.StyleProfileKey);
            if (!profileResult.Successful)
                return profileResult.ToInvokeResult<string>();

            return _promptBuilder.BuildPrompt(request, profileResult.Result);
        }

        public async Task<InvokeResult<LagoVistaIconPublishResult>> GenerateAsync(LagoVistaIconGenerationRequest request, EntityHeader org, EntityHeader user)
        {
            if (request == null)
                return InvokeResult<LagoVistaIconPublishResult>.FromError("LagoVista icon generation request is required.");

            var profileResult = _styleProfileProvider.GetProfile(request.StyleProfileKey);
            if (!profileResult.Successful)
                return profileResult.ToInvokeResult<LagoVistaIconPublishResult>();

            var promptResult = _promptBuilder.BuildPrompt(request, profileResult.Result);
            if (!promptResult.Successful)
                return promptResult.ToInvokeResult<LagoVistaIconPublishResult>();

            var generator = _imageGenerators.FirstOrDefault();
            if (generator == null)
                return InvokeResult<LagoVistaIconPublishResult>.FromError("No LagoVista icon image generator has been registered.");

            var generatedImageResult = await generator.GenerateAsync(request, profileResult.Result, promptResult.Result, org, user);
            if (!generatedImageResult.Successful)
                return generatedImageResult.ToInvokeResult<LagoVistaIconPublishResult>();

            var generatedImage = generatedImageResult.Result;
            if (generatedImage == null || generatedImage.ImageData == null || generatedImage.ImageData.Length == 0)
                return InvokeResult<LagoVistaIconPublishResult>.FromError("The LagoVista icon image generator did not return image data.");

            var publishRequest = new LagoVistaIconAssetPublishRequest
            {
                GenerationRequest = request,
                StyleProfile = profileResult.Result,
                SourceImageData = generatedImage.ImageData,
                SourceContentType = generatedImage.ContentType,
                SourceOutputFormat = generatedImage.OutputFormat,
                Prompt = promptResult.Result,
                RevisedPrompt = generatedImage.RevisedPrompt,
                Provider = generatedImage.Provider,
                ProviderResponseId = generatedImage.ProviderResponseId,
                Model = generatedImage.Model,
                GeneratedUtc = generatedImage.GeneratedUtc,
                CdnBaseUrl = request.CdnBaseUrl,
                Usage = generatedImage.Usage
            };

            return await _assetPublisher.PublishAsync(publishRequest);
        }

        public async Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync(LagoVistaIconAssetPublishRequest publishRequest, EntityHeader org, EntityHeader user)
        {
            if (publishRequest == null)
                return InvokeResult<LagoVistaIconPublishResult>.FromError("LagoVista icon asset publish request is required.");

            if (publishRequest.GenerationRequest == null)
                return InvokeResult<LagoVistaIconPublishResult>.FromError("LagoVista icon generation request is required.");

            if (publishRequest.StyleProfile == null)
            {
                var profileResult = _styleProfileProvider.GetProfile(publishRequest.GenerationRequest.StyleProfileKey);
                if (!profileResult.Successful)
                    return profileResult.ToInvokeResult<LagoVistaIconPublishResult>();

                publishRequest.StyleProfile = profileResult.Result;
            }

            if (String.IsNullOrWhiteSpace(publishRequest.Prompt))
            {
                var promptResult = _promptBuilder.BuildPrompt(publishRequest.GenerationRequest, publishRequest.StyleProfile);
                if (!promptResult.Successful)
                    return promptResult.ToInvokeResult<LagoVistaIconPublishResult>();

                publishRequest.Prompt = promptResult.Result;
            }

            return await _assetPublisher.PublishAsync(publishRequest);
        }
    }
}
