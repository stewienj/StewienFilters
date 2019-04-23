using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

namespace StewienFilters {
    public class FilterMenuSettings {
        public class Setting {
            [XmlAttribute]
            public bool IsActive = true;
            [XmlAttribute]
            public string ParentMenu = "StewienFilters";
        }
        private static FilterMenuSettings theInstance = null;
        private Setting dilate;
        private Setting kuwaharaFilter;
        private Setting kuwaharaFilterMod;
        private Setting toAngle;
        private Setting transparencyByColor;

        public Setting Dilate {
            get {
                if (dilate == null)
                    dilate = new Setting();
                return dilate;
            }
            set {
                dilate = value;
            }
        }

        public Setting KuwaharaFilter {
            get {
                if (kuwaharaFilter == null)
                    kuwaharaFilter = new Setting();
                return kuwaharaFilter;
            }
            set {
                kuwaharaFilter = value;
            }
        }

        public Setting KuwaharaFilterMod {
            get {
                if (kuwaharaFilterMod == null)
                    kuwaharaFilterMod = new Setting();
                return kuwaharaFilterMod;
            }
            set {
                kuwaharaFilterMod = value;
            }
        }

        public Setting ToAngle {
            get {
                if (toAngle == null)
                    toAngle = new Setting();
                return toAngle;
            }
            set {
                toAngle = value;
            }
        }

        public Setting TransparencyByColor {
            get {
                if (transparencyByColor == null)
                    transparencyByColor = new Setting();
                return transparencyByColor;
            }
            set {
                transparencyByColor = value;
            }
        }

        public static FilterMenuSettings TheInstance {
            get {
                if (theInstance == null) {
                    try {
                        string location = Assembly.GetExecutingAssembly().Location;
                        FileInfo xmlInfo = new FileInfo(location.Substring(0, location.Length - 3) + "xml");
                        XmlSerializer serializer = new XmlSerializer(typeof(FilterMenuSettings));
                        if (xmlInfo.Exists) {
                            using (FileStream stream = new FileStream(xmlInfo.FullName, FileMode.Open)) {
                                theInstance = (FilterMenuSettings)serializer.Deserialize(stream);
                            }
                        }
                        if (theInstance == null)
                            theInstance = new FilterMenuSettings();
                        using (FileStream stream = new FileStream(xmlInfo.FullName, FileMode.Create)) {
                            serializer.Serialize(stream, theInstance);
                        }
                    } catch (Exception) {
                    }
                }
                if (theInstance == null)
                    theInstance = new FilterMenuSettings();
                return theInstance;
            }
        }
    }
}
