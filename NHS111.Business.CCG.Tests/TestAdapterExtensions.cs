using NUnit.Framework;

namespace NHS111.Business.CCG.Tests
{
    public static class TestAdapterExtensions
    {
        public static string Expectation(this TestContext.TestAdapter operand)
        {
            return operand.Properties["Description"][0].ToString();
        }
    }
}