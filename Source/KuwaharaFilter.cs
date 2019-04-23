using System;
using System.Collections;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;

namespace StewienFilters {
    public class KuwaharaFilter : PropertyBasedEffect {

        private Int32 filterSize;

        public enum PropertyNames {
            FilterSize
        }

        public static string StaticName {
            get {
                return "Kuwahara Filter";
            }
        }

        public static Bitmap StaticIcon {
            get {
                return new Bitmap(typeof(KuwaharaFilter), "KuwaharaFilterIcon.png");
            }
        }

        public static string StaticSubMenuName {
            get {
                return FilterMenuSettings.TheInstance.KuwaharaFilter.ParentMenu;
            }
        }

        public static EffectFlags StaticEffectFlags {
            get {
                return EffectFlags.Configurable;
            }
        }

        public KuwaharaFilter()
            : base(StaticName, StaticIcon, StaticSubMenuName, StaticEffectFlags) {
                    if (!FilterMenuSettings.TheInstance.KuwaharaFilter.IsActive)
                        throw new ApplicationException();
                }

        protected override PropertyCollection OnCreatePropertyCollection() {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.FilterSize, 5, 3, 99));
            return new PropertyCollection(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs) {
            this.filterSize = newToken.GetProperty<Int32Property>(PropertyNames.FilterSize).Value;
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props) {
            ControlInfo configUI = CreateDefaultConfigUI(props);
            configUI.SetPropertyControlValue(PropertyNames.FilterSize, ControlInfoPropertyNames.DisplayName, "Filter Size");
            configUI.SetPropertyControlType(PropertyNames.FilterSize, PropertyControlType.Slider);
            return configUI;
        }

        protected override void OnRender(Rectangle[] rois, int startIndex, int length) {
            for (int i = startIndex; i < startIndex + length; ++i) {
                Rectangle rect = rois[i];
                RenderRI(DstArgs.Surface, SrcArgs.Surface, rect);
            }
        }

        // The Kuwahara filter works as follows
        // For each pixel, divide up the region around it into
        // 4 overlapping blocks where each block has the centre
        // pixel as a corner pixel.
        // For each of the blocks calculate the mean and the variance.
        // Set the middle pixel equal to the mean of the block with the
        // smallest variance.
        void RenderRI(Surface dst, Surface src, Rectangle rect) {
            PdnRegion selectionRegion = EnvironmentParameters.GetSelection(src.Bounds);
            Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();
            int radius = this.filterSize / 2;
            float scaler = 1f / ((radius + 1) * (radius + 1));

            // indicies are for each of the 4 quadrants

            float[] meansR = new float[4];
            float[] meansG = new float[4];
            float[] meansB = new float[4];
            float[] stdDevsR = new float[4];
            float[] stdDevsG = new float[4];
            float[] stdDevsB = new float[4];
            ColorBgra[] pixels = new ColorBgra[4];

            int[] xdx = new int[2];
            int[] ydy = new int[2];

            for (int y = rect.Top; y < rect.Bottom; ++y) {
                for (int x = rect.Left; x < rect.Right; ++x) {

                    // clear the means and standard deviations

                    for (int i = 0; i < 4; ++i) {
                        meansR[i] = 0f;
                        meansG[i] = 0f;
                        meansB[i] = 0f;
                        stdDevsR[i] = 0f;
                        stdDevsG[i] = 0f;
                        stdDevsB[i] = 0f;
                    }

                    // Calculate means

                    for (int dx = 0; dx <= radius; ++dx) {
                        xdx[0] = x - dx;
                        if (xdx[0] < src.Bounds.Left)
                            xdx[0] = src.Bounds.Left + src.Bounds.Left - xdx[0];
                        xdx[1] = x + dx;
                        if (xdx[1] >= src.Bounds.Right)
                            xdx[1] = src.Bounds.Right - 2 - xdx[1] + src.Bounds.Right;

                        for (int dy = 0; dy <= radius; ++dy) {
                            ydy[0] = y - dy;
                            if (ydy[0] < src.Bounds.Top)
                                ydy[0] = src.Bounds.Top + src.Bounds.Top - ydy[0];
                            ydy[1] = y + dy;
                            if (ydy[1] >= src.Bounds.Bottom)
                                ydy[1] = src.Bounds.Bottom - 2 - ydy[1] + src.Bounds.Bottom;

                            pixels[0] = src[xdx[0], ydy[0]];
                            pixels[1] = src[xdx[1], ydy[0]];
                            pixels[2] = src[xdx[0], ydy[1]];
                            pixels[3] = src[xdx[1], ydy[1]];

                            for (int i = 0; i < 4; ++i) {
                                meansR[i] += (float)pixels[i].R;
                                meansG[i] += (float)pixels[i].G;
                                meansB[i] += (float)pixels[i].B;
                            }
                        }
                    }

                    for (int i = 0; i < 4; ++i) {
                        meansR[i] *= scaler;
                        meansG[i] *= scaler;
                        meansB[i] *= scaler;
                    }

                    // Calculate standard deviations

                    for (int dx = 0; dx <= radius; ++dx) {
                        xdx[0] = x - dx;
                        if (xdx[0] < src.Bounds.Left)
                            xdx[0] = src.Bounds.Left + src.Bounds.Left - xdx[0];
                        xdx[1] = x + dx;
                        if (xdx[1] >= src.Bounds.Right)
                            xdx[1] = src.Bounds.Right - 2 - xdx[1] + src.Bounds.Right;

                        for (int dy = 0; dy <= radius; ++dy) {
                            ydy[0] = y - dy;
                            if (ydy[0] < src.Bounds.Top)
                                ydy[0] = src.Bounds.Top + src.Bounds.Top - ydy[0];
                            ydy[1] = y + dy;
                            if (ydy[1] >= src.Bounds.Bottom)
                                ydy[1] = src.Bounds.Bottom - 2 - ydy[1] + src.Bounds.Bottom;

                            pixels[0] = src[xdx[0], ydy[0]];
                            pixels[1] = src[xdx[1], ydy[0]];
                            pixels[2] = src[xdx[0], ydy[1]];
                            pixels[3] = src[xdx[1], ydy[1]];

                            for (int i = 0; i < 4; ++i) {
                                stdDevsR[i] += (meansR[i] - (float)pixels[i].R) * (meansR[i] - (float)pixels[i].R);
                                stdDevsG[i] += (meansG[i] - (float)pixels[i].G) * (meansG[i] - (float)pixels[i].G);
                                stdDevsB[i] += (meansB[i] - (float)pixels[i].B) * (meansB[i] - (float)pixels[i].B);
                            }
                        }
                    }

                    float lowest = float.MaxValue;
                    int lowestIndex = 0;

                    // work out the lowest standard deviation
                    for (int i = 0; i < 4; ++i) {
                        float rgbSum = stdDevsR[i] + stdDevsG[i] + stdDevsB[i];
                        if (rgbSum < lowest) {
                            lowest = rgbSum;
                            lowestIndex = i;
                        }
                    }

                    // Assign the destination pixel value

                    ColorBgra CurrentPixel = src[x, y];
                    CurrentPixel.R = (Byte)meansR[lowestIndex];
                    CurrentPixel.G = (Byte)meansG[lowestIndex];
                    CurrentPixel.B = (Byte)meansB[lowestIndex];
                    dst[x, y] = CurrentPixel;
                }
            }
        }
    }
}