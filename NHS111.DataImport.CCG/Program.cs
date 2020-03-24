namespace NHS111.DataImport.CCG
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var import = new DataImport();

            import.PerformImport(args);
        }
    }
}
