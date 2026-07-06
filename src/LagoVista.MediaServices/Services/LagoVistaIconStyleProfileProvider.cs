using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Services
{
    public class LagoVistaIconStyleProfileProvider : ILagoVistaIconStyleProfileProvider
    {
        public InvokeResult<LagoVistaIconStyleProfile> GetProfile(string styleProfileKey)
        {
            if (!String.IsNullOrWhiteSpace(styleProfileKey) && !String.Equals(styleProfileKey, LagoVistaIconStyleProfile.NuvOsSemanticIconKey, StringComparison.OrdinalIgnoreCase))
                return InvokeResult<LagoVistaIconStyleProfile>.FromError($"Unknown LagoVista icon style profile '{styleProfileKey}'.");

            return InvokeResult<LagoVistaIconStyleProfile>.Create(new LagoVistaIconStyleProfile
            {
                Key = LagoVistaIconStyleProfile.NuvOsSemanticIconKey,
                Name = "NuvOS Semantic Icon",
                Version = "1.0",
                MinimumSupportedSize = 32,
                PreferredDisplaySizes = new List<int> { 32, 40, 48, 64 },
                PublishedSizes = new List<int> { 32, 40, 48, 64, 128, 256 },
                GeneratedMasterSize = 1024,
                Background = "transparent",
                RenderStyle = "filled-polished-flat",
                MaxColors = 3,
                DominantColor = "#1976D2",
                AllowedColors = new List<string> { "#1976D2", "#D48D17", "#681DD6", "#1CBA88", "#FFFFFF", "#111827" },
                AccentColors = new List<string> { "#D48D17", "#681DD6", "#1CBA88" },
                AllowGradients = false,
                AllowGlow = false,
                AllowShadow = false,
                AllowScene = false,
                AllowText = false,
                AllowLogos = false,
                AllowPhotorealism = false,
                Allow3D = false
            });
        }
    }
}
