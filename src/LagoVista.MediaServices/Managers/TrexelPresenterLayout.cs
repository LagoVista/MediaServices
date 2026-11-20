using LagoVista.MediaServices.Models;
using System;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Consistent default framing for a cropped, transparent Trexel presenter.
    /// PresenterScale is a fraction of the 1920px composition canvas width.
    /// </summary>
    public static class TrexelPresenterLayout
    {
        public const double CompositionCanvasWidth = 1920.0;
        public const double CompositionCanvasHeight = 1080.0;
        public const double PresenterHeightRatio = 0.80;

        public static void Apply(VideoCompositionBlock contentBlock, int sourceWidth, int sourceHeight)
        {
            if (contentBlock == null) throw new ArgumentNullException(nameof(contentBlock));
            if (sourceWidth <= 0) throw new ArgumentOutOfRangeException(nameof(sourceWidth));
            if (sourceHeight <= 0) throw new ArgumentOutOfRangeException(nameof(sourceHeight));

            // Fit the *cropped* video's frame to 80% of the composition height.
            // Width-based scaling is the layout contract shared by the preview
            // and FFmpeg assembly. Cap the width for unusually wide sources.
            var targetHeight = CompositionCanvasHeight * PresenterHeightRatio;
            var widthAtTargetHeight = targetHeight * sourceWidth / sourceHeight;
            var presenterWidth = Math.Min(CompositionCanvasWidth, widthAtTargetHeight);

            contentBlock.PresenterScale = presenterWidth / CompositionCanvasWidth;
            contentBlock.PresenterPositionX = 0.5;
            contentBlock.PresenterPositionY = 1.0;
        }
    }
}
