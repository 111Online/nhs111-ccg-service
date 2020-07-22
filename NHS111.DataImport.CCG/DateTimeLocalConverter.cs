namespace NHS111.DataImport.CCG
{
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using System;

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