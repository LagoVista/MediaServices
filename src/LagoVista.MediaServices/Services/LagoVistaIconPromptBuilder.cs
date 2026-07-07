using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LagoVista.MediaServices.Services
{
    public class LagoVistaIconPromptBuilder : ILagoVistaIconPromptBuilder
    {
        public InvokeResult<string> BuildPrompt(LagoVistaIconGenerationRequest request, LagoVistaIconStyleProfile profile)
        {
            if (request == null)
                return InvokeResult<string>.FromError("LagoVista icon generation request is required.");

            if (profile == null)
                return InvokeResult<string>.FromError("LagoVista icon style profile is required.");

            if (request.SourceEntity == null)
                return InvokeResult<string>.FromError("Source entity is required.");

            if (String.IsNullOrWhiteSpace(request.SourceEntity.DisplayName))
                return InvokeResult<string>.FromError("Source entity display name is required.");

            var meaning = ResolveMeaning(request);
            if (String.IsNullOrWhiteSpace(meaning))
                return InvokeResult<string>.FromError("A meaning, purpose summary, purpose, or description is required to generate an icon prompt.");

            var prompt = new StringBuilder();
            prompt.AppendLine("Create one polished semantic UI icon for a SaaS application.");
            prompt.AppendLine();
            prompt.AppendLine("The icon must look like a reusable product entity icon, not a full illustration.");
            prompt.AppendLine();
            prompt.AppendLine("Style:");
            prompt.AppendLine("- Filled polished flat glyph");
            prompt.AppendLine("- Clean geometric shapes");
            prompt.AppendLine("- Friendly modern SaaS style");
            prompt.AppendLine("- Crisp edges");
            prompt.AppendLine("- Strong silhouette");
            prompt.AppendLine("- Minimal internal detail");
            prompt.AppendLine("- Transparent outer background");
            prompt.AppendLine("- Centered on a square canvas");
            prompt.AppendLine($"- Designed to read clearly at {profile.MinimumSupportedSize}x{profile.MinimumSupportedSize} and above");
            prompt.AppendLine("- Use white interior shapes or negative-space details when helpful for clarity");
            prompt.AppendLine("- The icon should feel like part of a consistent product icon family");
            prompt.AppendLine();
            prompt.AppendLine("Background rules:");
            prompt.AppendLine("- Use a real transparent alpha background.");
            prompt.AppendLine("- The pixels outside the icon must be fully transparent.");
            prompt.AppendLine("- Do not draw, render, simulate, or include a checkerboard transparency grid.");
            prompt.AppendLine("- Do not include any background pattern, tile, grid, paper, canvas, backdrop, scene, square, or border.");
            prompt.AppendLine("- do not incldue text, letters, numbers, logos, watermarks, screenshots, dashboards, UI chrome, photorealism, 3D effects, shadows, glow, blur, background scenes, or checkerboard transparency grids.");

            prompt.AppendLine();
           
            prompt.AppendLine("Color rules:");
            prompt.AppendLine("Use only these exact colors:");
            AppendColorLine(prompt, profile.DominantColor, "primary blue");
            foreach (var accentColor in profile.AccentColors ?? new List<string>())
                AppendColorLine(prompt, accentColor, ResolveAccentName(accentColor));
            AppendColorLine(prompt, "#FFFFFF", "white");
            AppendColorLine(prompt, "#111827", "near black");
            prompt.AppendLine();
            prompt.AppendLine($"Use 2 to {profile.MaxColors} colors maximum.");
            prompt.AppendLine($"Use {profile.DominantColor} as the dominant color unless there is a clear semantic reason not to.");
            prompt.AppendLine("Use one accent color when possible.");
            prompt.AppendLine("Use #FFFFFF for interior contrast, cutout-style details, or simple dividing shapes.");
            prompt.AppendLine("Do not invent new colors.");
            prompt.AppendLine();
            prompt.AppendLine("Subject:");
            prompt.AppendLine($"Entity Type: {SafeValue(request.SourceEntity.Type)}");
            prompt.AppendLine($"Entity Name: {request.SourceEntity.DisplayName.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.DomainKey))
                prompt.AppendLine($"Domain: {request.SourceEntity.DomainKey.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.EntityKey))
                prompt.AppendLine($"Entity Key: {request.SourceEntity.EntityKey.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.ClusterKey))
                prompt.AppendLine($"Cluster: {request.SourceEntity.ClusterKey.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.Description))
                prompt.AppendLine($"Description: {request.SourceEntity.Description.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.UserHelp))
                prompt.AppendLine($"User Help: {request.SourceEntity.UserHelp.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.ShortCode))
                prompt.AppendLine($"Short Code Metadata: {request.SourceEntity.ShortCode.Trim()} (do not render this text in the icon)");

            prompt.AppendLine($"Meaning: {meaning}");

            if (request.SourceInstance != null)
                AppendInstanceSubject(prompt, request);

            AppendOptionalCsv(prompt, "Keywords", request.Keywords);
            AppendOptionalCsv(prompt, "Preferred metaphors", request.SuggestedMetaphors);
            AppendOptionalCsv(prompt, "Avoid metaphors", request.AvoidMetaphors);
            if (!String.IsNullOrWhiteSpace(request.AdditionalGuidance))
            {
                prompt.AppendLine("Additional user guidance:");
                prompt.AppendLine(request.AdditionalGuidance.Trim());
                prompt.AppendLine("Follow this additional guidance only when it does not conflict with the style, color, composition, and exclusion rules above.");
            }

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.AiIconGuidance))
            {
                prompt.AppendLine();
                prompt.AppendLine("Entity-specific icon guidance:");
                prompt.AppendLine(request.SourceEntity.AiIconGuidance.Trim());
                prompt.AppendLine("Use this guidance to choose the semantic metaphor for both the default entity icon and specific instance icons of this entity type.");
            }
            prompt.AppendLine();
            prompt.AppendLine("Composition rules:");
            prompt.AppendLine("- Use one clear semantic metaphor.");
            prompt.AppendLine("- Use one dominant object.");
            prompt.AppendLine("- Keep the icon centered.");
            prompt.AppendLine("- Use generous padding.");
            prompt.AppendLine("- Keep internal details minimal.");
            prompt.AppendLine("- Prefer simple bold shapes over detailed scenes.");
            prompt.AppendLine("- Avoid object piles or stacked compositions unless explicitly requested.");
            prompt.AppendLine();
            prompt.AppendLine("Do not include text, letters, numbers, logos, watermarks, background scenes, shadows under the icon, glow, haze, blur, photorealism, 3D rendering, dashboard elements, or screenshot elements.");

            return InvokeResult<string>.Create(prompt.ToString().Trim());
        }

        private static string AppendInstanceSubject(StringBuilder prompt, LagoVistaIconGenerationRequest request)
        {
            prompt.AppendLine();
            prompt.AppendLine("Specific instance:");
            prompt.AppendLine($"Instance Name: {request.SourceInstance.DisplayName.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceInstance.Description))
                prompt.AppendLine($"Instance Description: {request.SourceInstance.Description.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceInstance.PurposeSummary))
                prompt.AppendLine($"Instance Purpose Summary: {request.SourceInstance.PurposeSummary.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.SourceInstance.Purpose))
                prompt.AppendLine($"Instance Purpose: {request.SourceInstance.Purpose.Trim()}");

            AppendOptionalCsv(prompt, "Instance Keywords", request.SourceInstance.Keywords);

            prompt.AppendLine("Use these instance facts to specialize the icon, but do not violate the entity-specific icon guidance or product icon family style.");

            return prompt.ToString();
        }
        
        
        private static string ResolveMeaning(LagoVistaIconGenerationRequest request)
        {
            if (!String.IsNullOrWhiteSpace(request.Meaning))
                return request.Meaning.Trim();

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.PurposeSummary))
                return request.SourceEntity.PurposeSummary.Trim();

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.Purpose))
                return request.SourceEntity.Purpose.Trim();

            if (!String.IsNullOrWhiteSpace(request.SourceEntity.Description))
                return request.SourceEntity.Description.Trim();

            return null;
        }

        private static void AppendColorLine(StringBuilder prompt, string color, string label)
        {
            if (!String.IsNullOrWhiteSpace(color))
                prompt.AppendLine($"- {color} {label}");
        }

        private static string ResolveAccentName(string color)
        {
            switch (color)
            {
                case "#D48D17":
                    return "amber accent";
                case "#681DD6":
                    return "purple accent";
                case "#1CBA88":
                    return "green accent";
                default:
                    return "accent";
            }
        }

        private static void AppendOptionalCsv(StringBuilder prompt, string label, List<string> values)
        {
            var csv = ToCsv(values);
            if (!String.IsNullOrWhiteSpace(csv))
                prompt.AppendLine($"{label}: {csv}");
        }

        private static string ToCsv(List<string> values)
        {
            if (values == null)
                return null;

            var cleaned = values.Where(value => !String.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();
            return cleaned.Any() ? String.Join(", ", cleaned) : null;
        }

        private static string SafeValue(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
        }
    }
}
