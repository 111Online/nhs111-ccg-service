using System.Threading.Tasks;

namespace NHS111.DataImport.CCG
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var import = new DataImport();

            await import.PerformImportAsync(args);
        }
    }
}
