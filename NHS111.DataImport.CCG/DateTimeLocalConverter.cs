namespace NHS111.DataImport.CCG
{
    using System;

    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    public class DateTimeLocalConverter : DateTimeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return DateTime.ParseExact(text, "dd/MM/yyyy", null);
        }
    }
}