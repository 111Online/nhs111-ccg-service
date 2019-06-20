using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NUnit.Framework;

namespace NHS111.Business.CCG.Functional.Tests.Tools
{
    public class SchemaValidation
    {
        public enum ResponseSchemaType
        {
            Get,
            GetDetails
        }
        public static void AssertValidResponseSchema(string result, ResponseSchemaType schemaType)
        {
            switch (schemaType)
            {
                case ResponseSchemaType.Get:
                    AssertValidGetResponseSchema(result);
                    break;
                case ResponseSchemaType.GetDetails:
                    AssertValidGetDetailsResponseSchema(result);
                    break;
                default:
                    throw new InvalidEnumArgumentException(string.Format("{0}{1}{2}", "ResponseSchemaType of ", schemaType.ToString(), "is unsupported"));
            }
        }

        private static void AssertValidGetResponseSchema(string result)
        {
            Assert.IsTrue(result.Contains("\"postcode"));
            Assert.IsTrue(result.Contains("\"ccg"));
            Assert.IsTrue(result.Contains("\"app"));
            Assert.IsTrue(result.Contains("\"dosSearchDistance"));
        }

        private static void AssertValidGetDetailsResponseSchema(string result)
        {
            Assert.IsTrue(result.Contains("\"stpName"));
            Assert.IsTrue(result.Contains("\"referralServiceIdWhitelist"));
            Assert.IsTrue(result.Contains("\"pharmacyReferralServiceIdWhitelist"));
            Assert.IsTrue(result.Contains("\"postcode"));
            Assert.IsTrue(result.Contains("\"ccg"));
            Assert.IsTrue(result.Contains("\"app"));
            Assert.IsTrue(result.Contains("\"dosSearchDistance"));
        }
    }
}
