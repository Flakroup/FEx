using FEx.Extensions.Base.Converters;
using FEx.Extensions.Base.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class ByteToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        var length = System.Convert.ToDouble(value);
        var digits = 3;
        LengthType lType = LengthType.AutoDetect;
        var pS = parameter as string;

        if (pS is not null)
        {
            string[] pA = pS.Split(',');
            digits = System.Convert.ToInt32(pA[0]);

            if (pA.Length > 1)
                lType = (LengthType)Enum.Parse(typeof(LengthType), pA[1]);
        }

        return FileLengthConverter.ConvertFileLengthToString(length, LengthType.Bytes, lType, digits);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}