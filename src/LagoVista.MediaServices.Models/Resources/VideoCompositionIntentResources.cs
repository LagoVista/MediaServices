using System;

namespace LagoVista.MediaServices.Models.Resources
{
    public static class VideoCompositionIntentResources
    {
        public static class Names
        {
            public const string VisualStyle_Unspecified = nameof(VisualStyle_Unspecified);
            public const string VisualStyle_Clean = nameof(VisualStyle_Clean);
            public const string VisualStyle_Premium = nameof(VisualStyle_Premium);
            public const string VisualStyle_Technical = nameof(VisualStyle_Technical);
            public const string VisualStyle_Editorial = nameof(VisualStyle_Editorial);
            public const string VisualStyle_Conversational = nameof(VisualStyle_Conversational);
            public const string VisualStyle_Energetic = nameof(VisualStyle_Energetic);
            public const string VisualStyle_Minimal = nameof(VisualStyle_Minimal);
            public const string VisualStyle_Cinematic = nameof(VisualStyle_Cinematic);

            public const string Pacing_Unspecified = nameof(Pacing_Unspecified);
            public const string Pacing_Slow = nameof(Pacing_Slow);
            public const string Pacing_Moderate = nameof(Pacing_Moderate);
            public const string Pacing_Brisk = nameof(Pacing_Brisk);
            public const string Pacing_Dynamic = nameof(Pacing_Dynamic);

            public const string PresenterEmphasis_Unspecified = nameof(PresenterEmphasis_Unspecified);
            public const string PresenterEmphasis_PresenterLed = nameof(PresenterEmphasis_PresenterLed);
            public const string PresenterEmphasis_Balanced = nameof(PresenterEmphasis_Balanced);
            public const string PresenterEmphasis_VisualLed = nameof(PresenterEmphasis_VisualLed);

            public const string Audience_Unspecified = nameof(Audience_Unspecified);
            public const string Audience_Executive = nameof(Audience_Executive);
            public const string Audience_GeneralBusiness = nameof(Audience_GeneralBusiness);
            public const string Audience_Technical = nameof(Audience_Technical);
            public const string Audience_Instructional = nameof(Audience_Instructional);
            public const string Audience_Marketing = nameof(Audience_Marketing);

            public const string Tone_Unspecified = nameof(Tone_Unspecified);
            public const string Tone_Confident = nameof(Tone_Confident);
            public const string Tone_Warm = nameof(Tone_Warm);
            public const string Tone_Authoritative = nameof(Tone_Authoritative);
            public const string Tone_Conversational = nameof(Tone_Conversational);
            public const string Tone_Urgent = nameof(Tone_Urgent);

            public const string InformationDensity_Unspecified = nameof(InformationDensity_Unspecified);
            public const string InformationDensity_Sparse = nameof(InformationDensity_Sparse);
            public const string InformationDensity_Moderate = nameof(InformationDensity_Moderate);
            public const string InformationDensity_Dense = nameof(InformationDensity_Dense);

            public const string BlockVisualFocus_Unspecified = nameof(BlockVisualFocus_Unspecified);
            public const string BlockVisualFocus_Presenter = nameof(BlockVisualFocus_Presenter);
            public const string BlockVisualFocus_SupportingVisual = nameof(BlockVisualFocus_SupportingVisual);
            public const string BlockVisualFocus_Text = nameof(BlockVisualFocus_Text);
            public const string BlockVisualFocus_Balanced = nameof(BlockVisualFocus_Balanced);

            public const string BlockEmphasis_Unspecified = nameof(BlockEmphasis_Unspecified);
            public const string BlockEmphasis_Supporting = nameof(BlockEmphasis_Supporting);
            public const string BlockEmphasis_Standard = nameof(BlockEmphasis_Standard);
            public const string BlockEmphasis_KeyMessage = nameof(BlockEmphasis_KeyMessage);
        }

        public static string VisualStyle_Unspecified => "Unspecified";
        public static string VisualStyle_Clean => "Clean";
        public static string VisualStyle_Premium => "Premium";
        public static string VisualStyle_Technical => "Technical";
        public static string VisualStyle_Editorial => "Editorial";
        public static string VisualStyle_Conversational => "Conversational";
        public static string VisualStyle_Energetic => "Energetic";
        public static string VisualStyle_Minimal => "Minimal";
        public static string VisualStyle_Cinematic => "Cinematic";

        public static string Pacing_Unspecified => "Unspecified";
        public static string Pacing_Slow => "Slow";
        public static string Pacing_Moderate => "Moderate";
        public static string Pacing_Brisk => "Brisk";
        public static string Pacing_Dynamic => "Dynamic";

        public static string PresenterEmphasis_Unspecified => "Unspecified";
        public static string PresenterEmphasis_PresenterLed => "Presenter Led";
        public static string PresenterEmphasis_Balanced => "Balanced";
        public static string PresenterEmphasis_VisualLed => "Visual Led";

        public static string Audience_Unspecified => "Unspecified";
        public static string Audience_Executive => "Executive";
        public static string Audience_GeneralBusiness => "General Business";
        public static string Audience_Technical => "Technical";
        public static string Audience_Instructional => "Instructional";
        public static string Audience_Marketing => "Marketing";

        public static string Tone_Unspecified => "Unspecified";
        public static string Tone_Confident => "Confident";
        public static string Tone_Warm => "Warm";
        public static string Tone_Authoritative => "Authoritative";
        public static string Tone_Conversational => "Conversational";
        public static string Tone_Urgent => "Urgent";

        public static string InformationDensity_Unspecified => "Unspecified";
        public static string InformationDensity_Sparse => "Sparse";
        public static string InformationDensity_Moderate => "Moderate";
        public static string InformationDensity_Dense => "Dense";

        public static string BlockVisualFocus_Unspecified => "Unspecified";
        public static string BlockVisualFocus_Presenter => "Presenter";
        public static string BlockVisualFocus_SupportingVisual => "Supporting Visual";
        public static string BlockVisualFocus_Text => "Text";
        public static string BlockVisualFocus_Balanced => "Balanced";

        public static string BlockEmphasis_Unspecified => "Unspecified";
        public static string BlockEmphasis_Supporting => "Supporting";
        public static string BlockEmphasis_Standard => "Standard";
        public static string BlockEmphasis_KeyMessage => "Key Message";
    }
}
