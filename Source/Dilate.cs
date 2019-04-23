using System;
using System.Collections;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;

namespace StewienFilters {
    public class Dilate : PropertyBasedEffect {

        private Int32 radius;
        private float[,] dilateWeights;

        public enum PropertyNames {
            Radius
        }

        public static string StaticName {
            get {
                return "Dilate";
            }
        }

        public static Bitmap StaticIcon {
            get {
                return new Bitmap(typeof(Dilate), "DilateIcon.png");
            }
        }

        public static string StaticSubMenuName {
            get {
                return FilterMenuSettings.TheInstance.Dilate.ParentMenu;
            }
        }

        public static EffectFlags StaticEffectFlags {
            get {
                return EffectFlags.Configurable;
            }
        }

        public Dilate()
            : base(StaticName, StaticIcon, StaticSubMenuName, StaticEffectFlags) {
            if (!FilterMenuSettings.TheInstance.Dilate.IsActive)
                throw new ApplicationException();
        }

        protected override PropertyCollection OnCreatePropertyCollection() {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.Radius, 1, 1, 99));
            return new PropertyCollection(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs) {
            this.radius = newToken.GetProperty<Int32Property>(PropertyNames.Radius).Value;

            // Create any intermediate surfaces or working data here
            dilateWeights = (float[,])Array.CreateInstance(typeof(float), new int[] { radius * 2 + 1, radius * 2 + 1 }, new int[] { -radius, -radius });
            for (int dy = -radius; dy <= radius; ++dy) {
                for (int dx = -radius; dx <= radius; ++dx) {
                    // Add some antialiasing to the dilate effect
                    for (float intray = -0.5f; intray <= 0.5; intray += 0.25f) {
                        for (float intrax = -0.5f; intrax <= 0.5; intrax += 0.25f) {
                            float dist = (float)Math.Sqrt((dx+intrax) * (dx+intrax) + (dy+intray) * (dy+intray));
                            float diff = radius - dist;
                            if (dist <= radius) {
                                dilateWeights[dx, dy] += 0.04f;
                            }
                        }
                    }
                }
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props) {
            ControlInfo configUI = CreateDefaultConfigUI(props);
            configUI.SetPropertyControlValue(PropertyNames.Radius, ControlInfoPropertyNames.DisplayName, "Radius");
            configUI.SetPropertyControlType(PropertyNames.Radius, PropertyControlType.Slider);
            return configUI;
        }

        protected override void OnRender(Rectangle[] rois, int startIndex, int length) {
            for (int i = startIndex; i < startIndex + length; ++i) {
                Rectangle rect = rois[i];
                RenderRI(DstArgs.Surface, SrcArgs.Surface, rect);
            }
        }

        // TODO:
        // Have a flag whether dilating darker or lighter
        // Have tapered option, linear or inverse cosine
        // Limit to circular area

        void RenderRI(Surface dst, Surface src, Rectangle rect) {
            PdnRegion selectionRegion = EnvironmentParameters.GetSelection(src.Bounds);
            Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();
            for (int y = rect.Top; y < rect.Bottom; ++y) {
                for (int x = rect.Left; x < rect.Right; ++x) {
                    // RGB Luminance value   =   0.3 R + 0.59 G + 0.11 B
                    float maxLuminance = 0f;
                    ColorBgra maxPoint = new ColorBgra();
                    maxPoint.A = 255;
                    for (int dy = -radius; dy <= radius; ++dy) {
                        for (int dx = -radius; dx <= radius; ++dx) {
                            int xdx = x + dx;
                            int ydy = y + dy;
                            if (xdx >= src.Bounds.Left && xdx < src.Bounds.Right && ydy >= src.Bounds.Top && ydy < src.Bounds.Bottom) {
                                ColorBgra CurrentPixel = src[xdx, ydy];
                                float luminance = (float)(0.3 * CurrentPixel.R + 0.59 * CurrentPixel.G + 0.11 * CurrentPixel.B) * dilateWeights[dx, dy];
                                if (luminance > maxLuminance) {
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
    }
}