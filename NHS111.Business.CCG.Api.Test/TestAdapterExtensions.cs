
namespace NHS111.Business.CCG.Api.Test {
    using NUnit.Framework;

    public static class TestAdapterExtensions {
        public static string Expectation(this TestContext.TestAdapter operand) {
            return operand.Properties["Description"][0].ToString();
        }
    }
}