using System.Xml.Serialization;

namespace libMidi.SMF;

[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class DevicePitch
{
    [XmlAttribute("pitch")]
    public byte Pitch { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("scorePitch")]
    public string ScorePitch { get; set; } = string.Empty;

    [XmlAttribute("notehead")]
    public string Notehead { get; set; } = string.Empty;

    [XmlAttribute("technique")]
    public string Technique { get; set; } = string.Empty;

    [XmlAttribute("flags")]
    public string Flags { get; set; } = string.Empty;
}