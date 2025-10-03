using System.ComponentModel.DataAnnotations;

namespace libMidi.Messages.enums;

public enum Key
{
    [Display(Name = "変イ短調")]
    A_Flat_Minor,

    [Display(Name = "変ホ短調")]
    E_Flat_Minor,

    [Display(Name = "変ロ短調")]
    B_Flat_Minor,

    [Display(Name = "ヘ短調")]
    F_Minor,

    [Display(Name = "ハ短調")]
    C_Minor,

    [Display(Name = "ト短調")]
    G_Minor,

    [Display(Name = "ニ短調")]
    D_Minor,

    [Display(Name = "イ短調")]
    A_Minor,

    [Display(Name = "ホ短調")]
    E_Minor,

    [Display(Name = "ロ短調")]
    B_Minor,

    [Display(Name = "嬰ヘ短調")]
    F_Sharp_Minor,

    [Display(Name = "嬰ハ短調")]
    C_Sharp_Minor,

    [Display(Name = "嬰ト短調")]
    G_Sharp_Minor,

    [Display(Name = "嬰ニ短調")]
    D_Sharp_Minor,

    [Display(Name = "嬰イ短調")]
    A_Sharp_Minor,

    [Display(Name = "変ハ長調")]
    C_Flat_Major = 0x10,

    [Display(Name = "変ト長調")]
    G_Flat_Major,

    [Display(Name = "変ニ長調")]
    D_Flat_Major,

    [Display(Name = "変イ長調")]
    A_Flat_Major,

    [Display(Name = "変ホ長調")]
    E_Flat_Major,

    [Display(Name = "変ロ長調")]
    B_Flat_Major,

    [Display(Name = "ヘ長調")]
    F_Major,

    [Display(Name = "ハ長調")]
    C_Major,

    [Display(Name = "ト長調")]
    G_Major,

    [Display(Name = "ニ長調")]
    D_Major,

    [Display(Name = "イ長調")]
    A_Major,

    [Display(Name = "ホ長調")]
    E_Major,

    [Display(Name = "ロ長調")]
    B_Major,

    [Display(Name = "嬰ヘ長調")]
    F_Sharp_Major,

    [Display(Name = "嬰ハ長調")]
    C_Sharp_Major
}