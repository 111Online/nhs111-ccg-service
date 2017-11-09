namespace NHS111.Business.CCG.Services {
    using System.Text.RegularExpressions;

    public static class PostCodeFormatValidator
    {
        public static string PostcodeRegex = @"^([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9]?[A-Za-z]))))[0-9][A-Za-z]{2})$";

        public static bool IsAValidPostcode(string postcode)
        {
            return !string.IsNullOrEmpty(postcode) && Regex.IsMatch(postcode.Replace(" ", string.Empty).ToLower(), PostcodeRegex);
        }
    }
}