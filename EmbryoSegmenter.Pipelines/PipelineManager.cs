using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Pipelines
{
    public static class PipelineManager
    {
        public static Pipeline InitializePipeline(PipelineType typeOfPipeline)
        {
            Pipeline return_pipeline = null;
            switch (typeOfPipeline)
            {
                case PipelineType.TEST:

                    TestPipeline test = new TestPipeline();
                    return_pipeline = test;
                    break;

                case PipelineType.EMPTY:

                    EmptyPipeline empty = new EmptyPipeline();
                    return_pipeline = empty;
                    break;
            }
            return return_pipeline;
        }
        /*
        public bool LoadPipelineParameters()
        {
            try
            {
                iocards = new List<SettingIOCard>();
                string fileName = Settings.m_Directory + "\\SettingsIOCard.xml";

                //Check if file exits - if not create a "blank" IOCard settings file
                if (!System.IO.File.Exists(fileName))
                {
                    m_Setting.defaulSettingIOCard();
                    iocards.Add(m_Setting);
                    return true;
                }

                //Read IOCard settings in XML document
                XmlTextReader XReader = new XmlTextReader(fileName);
                XmlDocument XDoc = new XmlDocument();
                XDoc.Load(XReader);
                XmlNodeList XSettingsList = XDoc.GetElementsByTagName("SettingsIOCard");
                XmlNode XSettings = XSettingsList[0];
                XmlNodeList XSettingList = XSettings.SelectNodes("SettingIOCard" + (short)m_SettingIOCardID);
                XmlNode XSetting = XSettingList[0];


                //Load PHI default settings
                if (XSetting.Attributes.GetNamedItem("ioCardIndex") != null)
                {
                    int.TryParse(XSetting.Attributes.GetNamedItem("ioCardIndex").Value, out m_Setting.ioCardIndex);
                }

                if (XSetting.Attributes.GetNamedItem("laserShutterDelay") != null)
                {
                    int.TryParse(XSetting.Attributes.GetNamedItem("laserShutterDelay").Value, out m_Setting.laserShutterDelay);
                }

                if (XSetting.Attributes.GetNamedItem("laserShutterPulseLength") != null)
                {
                    int.TryParse(XSetting.Attributes.GetNamedItem("laserShutterPulseLength").Value, out m_Setting.laserShutterPulseLength);
                }

                iocards.Add(m_Setting);
                XReader.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SavePipelineParameters()
        {
            try
            {
                string fileName = Settings.m_Directory + "\\SettingsIOCard.xml";
                XmlDocument doc = new XmlDocument();

                //Check if Settings file exists and if not create one with choosen settings
                if (!System.IO.File.Exists(fileName))
                {
                    //Start writer
                    XmlTextWriter textWriter = new XmlTextWriter(fileName, null);
                    textWriter.Formatting = Formatting.Indented;
                    //Start writing document
                    textWriter.WriteStartDocument();
                    textWriter.WriteStartElement("SettingsIOCard");

                    //Write settings
                    textWriter.WriteStartElement("SettingIOCard");
                    textWriter.WriteAttributeString("ioCardIndex", m_Setting.ioCardIndex.ToString());
                    textWriter.WriteAttributeString("laserShutterDelay", m_Setting.laserShutterDelay.ToString());
                    textWriter.WriteAttributeString("laserShutterPulseLength", m_Setting.laserShutterPulseLength.ToString());
                    textWriter.WriteEndElement();

                    // Ends the document.
                    textWriter.WriteEndElement();
                    textWriter.WriteEndDocument();

                    // close writer
                    textWriter.Close();
                    return true;
                }
                //If file exists, insert new settings for duplicate object
                else
                {

                    doc.Load(fileName);
                    XmlNode root = doc.DocumentElement;
                    XmlNode node = root.SelectSingleNode("SettingLaser" + (short)m_SettingIOCardID);
                    XmlElement elem = doc.CreateElement("SettingLaser" + (short)m_SettingIOCardID);

                    //Check if a present objects settings already exits and if so remove it and insert objects new settings
                    if (node != null)
                    {
                        root.RemoveChild(node);
                    }
                    elem.SetAttribute("ioCardIndex", m_Setting.ioCardIndex.ToString());
                    elem.SetAttribute("laserShutterDelay", m_Setting.laserShutterDelay.ToString());
                    elem.SetAttribute("laserShutterPulseLength", m_Setting.laserShutterPulseLength.ToString());

                    root.InsertAfter(elem, root.FirstChild);
                    doc.Save(fileName);

                    return true;
                }

            }
            catch
            {
                return false;
            }
        }
       */
    }
    public enum PipelineType
    {
        GRADIENT_MAGNITUDE = 1,
        TEST = 0,
        EMPTY = 2
    }
}
