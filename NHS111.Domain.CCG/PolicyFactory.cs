using Polly;
using Polly.Retry;
using System;

namespace NHS111.Domain.CCG
{
    public static class PolicyFactory
    {
        public static AsyncRetryPolicy IfException(int retryWaitMilliseconds = 200) {
            return Policy
              .Handle<Exception>()
              .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * retryWaitMilliseconds));
        }
    }
}
