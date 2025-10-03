using System.Xml.Serialization;

namespace libMidi.SMF;

[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot("Music.PitchNameList", Namespace = "", IsNullable = false)]
public partial class DevicePitchList
{
    [XmlElement("Music.PitchName")]
    public List<DevicePitch> Items { get; set; } = new();

    [XmlAttribute("title")]
    public string Title { get; set; } = string.Empty;
}
