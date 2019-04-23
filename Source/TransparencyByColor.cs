// Name: TransparencyByColor
// Submenu: Photo
// Author: John Stewien
// Title: Transparency By Secondary Color
// Version:
// Desc:
// Keywords:
// URL:
// Help:
#region UICode
#endregion

byte CalcMinimumAlpha(int original, int background) 
{
    int newValue;

    if (background == original) {
        return 0;
    } else if (background < original) {
        newValue = 255;
    } else {
        newValue = 0;
    }

    // Take the ceiling as if alpha is too low then the new value will be outside the range 0 to 255

    int alpha = (int)Math.Ceiling((255.0 * (original - background) / (newValue - background)));
    alpha = Math.Max(0, Math.Min(255, alpha));
    return (byte)alpha;
}

byte CalcNewValue(int alpha, int original, int background) 
{
    if (alpha == 0)
        return (byte)original;
    int newValue = (int)Math.Ceiling((background * alpha + 255.0 * (original - background)) / alpha);
    newValue = Math.Max(0, Math.Min(255, newValue));
    return (byte)newValue;
}

// Here is the main render loop function
void Render(Surface dst, Surface src, Rectangle rect)
{
    // Delete these lines if you don't need the primary or secondary color
    ColorBgra PrimaryColor = (ColorBgra)EnvironmentParameters.PrimaryColor;
    ColorBgra SecondaryColor = (ColorBgra)EnvironmentParameters.SecondaryColor;
    ColorBgra background = SecondaryColor;

    for (int y = rect.Top; y < rect.Bottom; ++y) 
    {
        if (IsCancelRequested)
            return;
        for (int x = rect.Left; x < rect.Right; ++x) 
        {
            ColorBgra CurrentPixel = src[x, y];

            // Calculate the minimum alpha for each channel, the 
            // overall minimum is the maxium of all those

            byte alpha = Math.Max(
                CalcMinimumAlpha(CurrentPixel.R, background.R), Math.Max(
                CalcMinimumAlpha(CurrentPixel.G, background.G),
                CalcMinimumAlpha(CurrentPixel.B, background.B)));

            CurrentPixel.R = CalcNewValue(alpha, CurrentPixel.R, background.R);
            CurrentPixel.G = CalcNewValue(alpha, CurrentPixel.G, background.G);
            CurrentPixel.B = CalcNewValue(alpha, CurrentPixel.B, background.B);
            CurrentPixel.A = alpha;

            dst[x, y] = CurrentPixel;
        }
    }
}

