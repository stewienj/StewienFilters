using System;
using System.Collections;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;

namespace StewienFilters {
    public class TransparencyByColor : PropertyBasedEffect {

        private Int32 colorToSubtract;

        public enum PropertyNames {
            TransparentColor
        }

        public static string StaticName {
            get {
                return "Transparency By Color Subtraction";
            }
        }

        public static Bitmap StaticIcon {
            get {
                return new Bitmap(typeof(TransparencyByColor), "TransparencyByColorIcon.png");
            }
        }

        public static string StaticSubMenuName {
            get {
                return FilterMenuSettings.TheInstance.TransparencyByColor.ParentMenu;
            }
        }

        public static EffectFlags StaticEffectFlags {
            get {
                return EffectFlags.Configurable;
            }
        }

        public TransparencyByColor()
            : base(StaticName, StaticIcon, StaticSubMenuName, StaticEffectFlags) {
            if (!FilterMenuSettings.TheInstance.TransparencyByColor.IsActive)
                throw new ApplicationException();

        }

        protected override PropertyCollection OnCreatePropertyCollection() {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.TransparentColor, EnvironmentParameters.PrimaryColor.ToColor().ToArgb() & 0xFFFFFF, 0, 0xFFFFFF));
            return new PropertyCollection(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs) {
            this.colorToSubtract = newToken.GetProperty<Int32Property>(PropertyNames.TransparentColor).Value;
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props) {
            ControlInfo configUI = CreateDefaultConfigUI(props);
            configUI.SetPropertyControlValue(PropertyNames.TransparentColor, ControlInfoPropertyNames.DisplayName, "Color To Subtract");
            configUI.SetPropertyControlType(PropertyNames.TransparentColor, PropertyControlType.ColorWheel);
            return configUI;
        }

        protected override void OnRender(Rectangle[] rois, int startIndex, int length) {
            for (int i = startIndex; i < startIndex + length; ++i) {
                Rectangle rect = rois[i];
                RenderRI(DstArgs.Surface, SrcArgs.Surface, rect);
            }
        }

        byte CalcMinimumAlpha(int original, int background) {
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

        byte CalcNewValue(int alpha, int original, int background) {
            if (alpha == 0)
                return (byte)original;
            int newValue = (int)Math.Ceiling((background * alpha + 255.0 * (original - background)) / alpha);
            newValue = Math.Max(0, Math.Min(255, newValue));
            return (byte)newValue;
        }

        void RenderRI(Surface dst, Surface src, Rectangle rect) {
            PdnRegion selectionRegion = EnvironmentParameters.GetSelection(src.Bounds);
            Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();
            ColorBgra background = ColorBgra.FromOpaqueInt32(colorToSubtract);
            for (int y = rect.Top; y < rect.Bottom; ++y) {
                for (int x = rect.Left; x < rect.Right; ++x) {
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
    }
}