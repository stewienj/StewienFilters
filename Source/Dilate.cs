// Name: Dilate
// Submenu: Photo
// Author: John Stewien
// Title: Dilate
// Version:
// Desc:
// Keywords:
// URL:
// Help:
#region UICode
IntSliderControl Amount1 = 5; // [0,100] Filter Radius
#endregion

void Render(Surface dst, Surface src, Rectangle rect)
{
    int radius = Amount1;
    float[,] dilateWeights;
    
    dilateWeights = (float[,])Array.CreateInstance(typeof(float), new int[] { radius * 2 + 1, radius * 2 + 1 }, new int[] { -radius, -radius });
    for (int dy = -radius; dy <= radius; ++dy) 
    {
        for (int dx = -radius; dx <= radius; ++dx) 
        {
            // Add some antialiasing to the dilate effect
            for (float intray = -0.5f; intray <= 0.5; intray += 0.25f) 
            {
                for (float intrax = -0.5f; intrax <= 0.5; intrax += 0.25f) 
                {
                    float dist = (float)Math.Sqrt((dx+intrax) * (dx+intrax) + (dy+intray) * (dy+intray));
                    float diff = radius - dist;
                    if (dist <= radius) 
                    {
                        dilateWeights[dx, dy] += 0.04f;
                    }
                }
            }
        }
    }
    
    // Delete any of these lines you don't need
    Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();
    ColorBgra CurrentPixel;
     for (int y = rect.Top; y < rect.Bottom; ++y) 
     {
        for (int x = rect.Left; x < rect.Right; ++x) 
        {
            // RGB Luminance value   =   0.3 R + 0.59 G + 0.11 B
            float maxLuminance = 0f;
            ColorBgra maxPoint = new ColorBgra();
            maxPoint.A = 255;
            for (int dy = -radius; dy <= radius; ++dy) 
            {
                for (int dx = -radius; dx <= radius; ++dx) 
                {
                    int xdx = x + dx;
                    int ydy = y + dy;
                    if (xdx >= src.Bounds.Left && xdx < src.Bounds.Right && ydy >= src.Bounds.Top && ydy < src.Bounds.Bottom)
                    {
                        CurrentPixel = src[xdx, ydy];
                        float luminance = (float)(0.3 * CurrentPixel.R + 0.59 * CurrentPixel.G + 0.11 * CurrentPixel.B) * dilateWeights[dx, dy];
                        if (luminance > maxLuminance)
                        {
                            maxPoint.R = (Byte)(CurrentPixel.R * dilateWeights[dx, dy]);
                            maxPoint.G = (Byte)(CurrentPixel.G * dilateWeights[dx, dy]);
                            maxPoint.B = (Byte)(CurrentPixel.B * dilateWeights[dx, dy]);
                            maxLuminance = luminance;
                        }
                    }
                }
            }
            dst[x, y] = maxPoint;
        }
    }
 }
