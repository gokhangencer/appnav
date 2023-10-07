using System;
using System.Drawing;
using System.IO;
using System.Windows;

namespace appnav.Utils;

public static class TrayIconHelper
{
    public static Icon GetIcon(string path)
    {
        Stream iconStream = Application.GetResourceStream(new Uri(path)).Stream;
        return new Icon(iconStream);
    }
}
