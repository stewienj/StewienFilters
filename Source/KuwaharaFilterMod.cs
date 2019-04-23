using System;
using System.Collections;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;

namespace StewienFilters {
    public class KuwaharaFilterMod : PropertyBasedEffect {

        private Int32 filterSize;
        private Int32 segmentCount;

        public enum PropertyNames {
            FilterSize,
            SegmentCount
        }

        public static string StaticName {
            get {
                return "Kuwahara Filter Modified";
            }
        }

        public static Bitmap StaticIcon {
            get {
                return new Bitmap(typeof(KuwaharaFilterMod), "KuwaharaFilterModIcon.png");
            }
        }

        public static string StaticSubMenuName {
            get {
                return FilterMenuSettings.TheInstance.KuwaharaFilterMod.ParentMenu;
            }
        }

        public static EffectFlags StaticEffectFlags {
            get {
                return EffectFlags.Configurable;
            }
        }

        public KuwaharaFilterMod()
            : base(StaticName, StaticIcon, StaticSubMenuName, StaticEffectFlags) {
            if (!FilterMenuSettings.TheInstance.KuwaharaFilterMod.IsActive)
                throw new ApplicationException();
        }

        protected override PropertyCollection OnCreatePropertyCollection() {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.FilterSize, 5, 3, 99));
            props.Add(new Int32Property(PropertyNames.SegmentCount, 4, 3, 32));
            return new PropertyCollection(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs) {
            this.filterSize = newToken.GetProperty<Int32Property>(PropertyNames.FilterSize).Value;
            this.segmentCount = newToken.GetProperty<Int32Property>(PropertyNames.SegmentCount).Value;
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props) {
            ControlInfo configUI = CreateDefaultConfigUI(props);
            configUI.SetPropertyControlValue(PropertyNames.FilterSize, ControlInfoPropertyNames.DisplayName, "Filter Size");
            configUI.SetPropertyControlType(PropertyNames.FilterSize, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.SegmentCount, ControlInfoPropertyNames.DisplayName, "Segment Count");
            configUI.SetPropertyControlType(PropertyNames.SegmentCount, PropertyControlType.Slider);
            return configUI;
        }

        protected override void OnRender(Rectangle[] rois, int startIndex, int length) {
            for (int i = startIndex; i < startIndex + length; ++i) {
                Rectangle rect = rois[i];
                RenderRI(DstArgs.Surface, SrcArgs.Surface, rect);
            }
        }

        // Returns the segments that the current pixel belongs to
        int[] SegmentMemberShip(int dx, int dy, int segmentCount, int radius) {
            List<int> retVal = new List<int>();
            float[] angles = new float[9];
            bool withinRadius = false;
            float radius2 = radius * radius;

            int angleIndex = 0;
            for (int dxa = -1; dxa <= 1; ++dxa) {
                for (int dya = -1; dya <= 1; ++dya) {
                    float dxaf = (float)dxa * 0.5f;
                    float dyaf = (float)dya * 0.5f;
                    if (!withinRadius) {
                        float dist = (dy + dyaf) * (dy + dyaf) + (dx + dxaf) * (dx + dxaf);
                        withinRadius = dist < radius2;
                    }

                    angles[angleIndex] = (float)Math.Atan2((float)dy + dyaf, (float)dx + dxaf);
                    if (angles[angleIndex] < 0) {
                        angles[angleIndex] += 2f * (float)Math.PI;
                    }
                    ++angleIndex;
                }
            }
            if (!withinRadius)
                return new int[0];

            // find minimum and maximum angles, if min and max difference >PI then need to treat specially

            float minAngle = angles[0];
            float maxAngle = angles[0];
            for (angleIndex = 0; angleIndex < 9; ++angleIndex) {
                minAngle = Math.Min(minAngle, angles[angleIndex]);
                maxAngle = Math.Max(maxAngle, angles[angleIndex]);
            }

            if ((maxAngle - minAngle) < Math.PI) {
                int minSeg = (int)(minAngle * segmentCount / (2f * Math.PI));
                int maxSeg = (int)(maxAngle * segmentCount / (2f * Math.PI));
                for (int i = minSeg; i <= maxSeg; ++i) {
                    retVal.Add(i);
                }
            } else {
                bool underPiInit = false;
                bool overPiInit = false;
                float minAngleUnderPi = 0;
                float maxAngleUnderPi = 0;
                float minAngleOverPi = 0;
                float maxAngleOverPi = 0;
                for (angleIndex = 0; angleIndex < 9; ++angleIndex) {
                    if (angles[angleIndex] >= Math.PI) {
                        if (overPiInit) {
                            minAngleOverPi = Math.Min(minAngleOverPi, angles[angleIndex]);
                            maxAngleOverPi = Math.Max(maxAngleOverPi, angles[angleIndex]);
                        } else {
                            overPiInit = true;
                            minAngleOverPi = angles[angleIndex];
                            maxAngleOverPi = angles[angleIndex];
                        }
                    } else {
                        if (underPiInit) {
                            minAngleUnderPi = Math.Min(minAngleUnderPi, angles[angleIndex]);
                            maxAngleUnderPi = Math.Max(maxAngleUnderPi, angles[angleIndex]);
                        } else {
                            underPiInit = true;
                            minAngleUnderPi = angles[angleIndex];
                            maxAngleUnderPi = angles[angleIndex];
                        }
                    }
                }
                int minSegUnderPi = (int)(minAngleUnderPi * segmentCount / (2f * Math.PI));
                int maxSegUnderPi = (int)(maxAngleUnderPi * segmentCount / (2f * Math.PI));
                for (int i = minSegUnderPi; i <= maxSegUnderPi; ++i) {
                    retVal.Add(i);
                }
                int minSegOverPi = (int)(minAngleOverPi * segmentCount / (2f * Math.PI));
                int maxSegOverPi = (int)(maxAngleOverPi * segmentCount / (2f * Math.PI));
                for (int i = minSegOverPi; i <= maxSegOverPi; ++i) {
                    retVal.Add(i);
                }
            }
            return retVal.ToArray();
        }

        // The Kuwahara filter works as follows
        // For each pixel, divide up the region around it into
        // 4 overlapping blocks where each block has the centre
        // pixel as a corner pixel.
        // For each of the blocks calculate the mean and the variance.
        // Set the middle pixel equal to the mean of the block with the
        // smallest variance.
        // This modified version allows for more than 4 segments, and
        // limits the distance of the filter to the actual radius
        // rather than using a square block.

        void RenderRI(Surface dst, Surface src, Rectangle rect) {
            PdnRegion selectionRegion = EnvironmentParameters.GetSelection(src.Bounds);
            Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();
            int radius = this.filterSize / 2;

            // indicies are for each of the 4 quadrants

            float[] meansR = new float[segmentCount];
            float[] meansG = new float[segmentCount];
            float[] meansB = new float[segmentCount];
            float[] stdDevsR = new float[segmentCount];
            float[] stdDevsG = new float[segmentCount];
            float[] stdDevsB = new float[segmentCount];
            float[] pixelCount = new float[segmentCount];

            // Pre calculate the segment membership table

            int[,][] segmentsByDxDy = new int[radius * 2 + 1, radius * 2 + 1][];
            for (int dx = -radius; dx <= radius; ++dx) {
                for (int dy = -radius; dy <= radius; ++dy) {
                    segmentsByDxDy[dx + radius, dy + radius] = SegmentMemberShip(dx, dy, segmentCount, radius);
                }
            }

            for (int y = rect.Top; y < rect.Bottom; ++y) {
                for (int x = rect.Left; x < rect.Right; ++x) {

                    // clear the means and standard deviations

                    for (int i = 0; i < segmentCount; ++i) {
                        meansR[i] = 0f;
                        meansG[i] = 0f;
                        meansB[i] = 0f;
                        stdDevsR[i] = 0f;
                        stdDevsG[i] = 0f;
                        stdDevsB[i] = 0f;
                        pixelCount[i] = 0f;
                    }

                    // Calculate means

                    for (int dx = -radius; dx <= radius; ++dx) {
                        int xdx = x + dx;
                        if (xdx < src.Bounds.Left)
                            xdx = src.Bounds.Left + src.Bounds.Left - xdx;
                        if (xdx >= src.Bounds.Right)
                            xdx = src.Bounds.Right - 2 - xdx + src.Bounds.Right;

                        for (int dy = -radius; dy <= radius; ++dy) {
                            int ydy = y + dy;
                            if (ydy < src.Bounds.Top)
                                ydy = src.Bounds.Top + src.Bounds.Top - ydy;
                            if (ydy >= src.Bounds.Bottom)
                                ydy = src.Bounds.Bottom - 2 - ydy + src.Bounds.Bottom;


                            // int segmentNo

                            ColorBgra pixels = src[xdx, ydy];

                            int[] segments = segmentsByDxDy[dx + radius, dy + radius];
                            for (int i = 0; i < segments.Length; ++i) {
                                meansR[segments[i]] += pixels.R;
                                meansG[segments[i]] += pixels.G;
                                meansB[segments[i]] += pixels.B;
                                pixelCount[segments[i]] += 1f;
                            }
                        }
                    }

                    for (int i = 0; i < segmentCount; ++i) {
                        if (pixelCount[i] > 0f) {
                            meansR[i] /= pixelCount[i];
                            meansG[i] /= pixelCount[i];
                            meansB[i] /= pixelCount[i];
                        }
                    }

                    // Calculate standard deviations

                    for (int dx = -radius; dx <= radius; ++dx) {
                        int xdx = x + dx;
                        if (xdx < src.Bounds.Left)
                            xdx = src.Bounds.Left + src.Bounds.Left - xdx;
                        if (xdx >= src.Bounds.Right)
                            xdx = src.Bounds.Right - 2 - xdx + src.Bounds.Right;

                        for (int dy = -radius; dy <= radius; ++dy) {
                            int ydy = y + dy;
                            if (ydy < src.Bounds.Top)
                                ydy = src.Bounds.Top + src.Bounds.Top - ydy;
                            if (ydy >= src.Bounds.Bottom)
                                ydy = src.Bounds.Bottom - 2 - ydy + src.Bounds.Bottom;

                            ColorBgra pixels = src[xdx, ydy];

                            int[] segments = segmentsByDxDy[dx + radius, dy + radius];
                            for (int i = 0; i < segments.Length; ++i) {
                                stdDevsR[segments[i]] += (meansR[segments[i]] - pixels.R) * (meansR[segments[i]] - pixels.R);
                                stdDevsG[segments[i]] += (meansG[segments[i]] - pixels.G) * (meansG[segments[i]] - pixels.G);
                                stdDevsB[segments[i]] += (meansB[segments[i]] - pixels.B) * (meansB[segments[i]] - pixels.B);
                            }
                        }
                    }

                    float lowest = float.MaxValue;
                    int lowestIndex = 0;

                    // work out the lowest standard deviation
                    for (int i = 0; i < segmentCount; ++i) {
                        if (pixelCount[i] > 0f) {
                            float rgbSum = (stdDevsR[i] + stdDevsG[i] + stdDevsB[i]) / pixelCount[i];
                            if (rgbSum < lowest) {
                                lowest = rgbSum;
                                lowestIndex = i;
                            }
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