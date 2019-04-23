using System;
using System.Collections;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;

namespace StewienFilters {
    public class ToAngle : PropertyBasedEffect {

        private Int32 filterSize;

        public enum PropertyNames {
            FilterSize
        }

        public static string StaticName {
            get {
                return "To Angle";
            }
        }

        public static Bitmap StaticIcon {
            get {
                return new Bitmap(typeof(ToAngle), "ToAngleIcon.png");
            }
        }

        public static string StaticSubMenuName {
            get {
                return FilterMenuSettings.TheInstance.ToAngle.ParentMenu;
            }
        }

        public static EffectFlags StaticEffectFlags {
            get {
                return EffectFlags.Configurable;
            }
        }

        public ToAngle()
            : base(StaticName, StaticIcon, StaticSubMenuName, StaticEffectFlags) {
            if (!FilterMenuSettings.TheInstance.ToAngle.IsActive)
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
            int[] xdx = new int[2];
            int[] ydy = new int[2];

            for (int y = rect.Top; y < rect.Bottom; ++y) {
                for (int x = rect.Left; x < rect.Right; ++x) {

                    float xdiffR = 0f;
                    float xdiffG = 0f;
                    float xdiffB = 0f;
                    float ydiffR = 0f;
                    float ydiffG = 0f;
                    float ydiffB = 0f;

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

                            xdiffR += src[xdx[0], ydy[0]].R;
                            xdiffR -= src[xdx[1], ydy[0]].R;
                            xdiffG += src[xdx[0], ydy[0]].G;
                            xdiffG -= src[xdx[1], ydy[0]].G;
                            xdiffB += src[xdx[0], ydy[0]].B;
                            xdiffB -= src[xdx[1], ydy[0]].B;
                            if (dy != 0) {
                                xdiffR += src[xdx[0], ydy[1]].R;
                                xdiffR -= src[xdx[1], ydy[1]].R;
                                xdiffG += src[xdx[0], ydy[1]].G;
                                xdiffG -= src[xdx[1], ydy[1]].G;
                                xdiffB += src[xdx[0], ydy[1]].B;
                                xdiffB -= src[xdx[1], ydy[1]].B;
                            }

                            ydiffR += src[xdx[0], ydy[0]].R;
                            ydiffR -= src[xdx[0], ydy[1]].R;
                            ydiffG += src[xdx[0], ydy[0]].G;
                            ydiffG -= src[xdx[0], ydy[1]].G;
                            ydiffB += src[xdx[0], ydy[0]].B;
                            ydiffB -= src[xdx[0], ydy[1]].B;
                            if (dx != 0) {
                                ydiffR += src[xdx[1], ydy[0]].R;
                                ydiffR -= src[xdx[1], ydy[1]].R;
                                ydiffG += src[xdx[1], ydy[0]].G;
                                ydiffG -= src[xdx[1], ydy[1]].G;
                                ydiffB += src[xdx[1], ydy[0]].B;
                                ydiffB -= src[xdx[1], ydy[1]].B;
                            }
                        }
                    }

                    ColorBgra CurrentPixel = src[x, y];
                    CurrentPixel.R = (byte)(255 * Math.Abs(Math.Atan2(ydiffR, xdiffR) / Math.PI));
                    CurrentPixel.G = (byte)(255 * Math.Abs(Math.Atan2(ydiffG, xdiffG) / Math.PI));
                    CurrentPixel.B = (byte)(255 * Math.Abs(Math.Atan2(ydiffB, xdiffB) / Math.PI));
                    dst[x, y] = CurrentPixel;
                }
            }
        }
    }
}