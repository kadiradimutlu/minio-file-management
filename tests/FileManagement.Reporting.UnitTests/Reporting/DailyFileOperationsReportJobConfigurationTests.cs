using FileManagement.Reporting.Worker.Reporting;
using Hangfire;

namespace FileManagement.Reporting.UnitTests.Reporting;

public sealed class
    DailyFileOperationsReportJobConfigurationTests
{
    [Theory]
    [InlineData(nameof(
        DailyFileOperationsReportJob
            .GeneratePreviousDayAsync))]
    [InlineData(nameof(
        DailyFileOperationsReportJob
            .GenerateAsync))]
    public void JobMethod_HasRetryAndConcurrencyGuards(
        string methodName)
    {
        var method =
            typeof(DailyFileOperationsReportJob)
                .GetMethods()
                .Single(
                    candidate =>
                        candidate.Name ==
                            methodName);

        var retry =
            Assert.Single(
                method.GetCustomAttributes(
                        typeof(
                            AutomaticRetryAttribute),
                        inherit: false)
                    .Cast<
                        AutomaticRetryAttribute>());

        var concurrency =
            Assert.Single(
                method.GetCustomAttributes(
                        typeof(
                            DisableConcurrentExecutionAttribute),
                        inherit: false)
                    .Cast<
                        DisableConcurrentExecutionAttribute>());

        Assert.Equal(3, retry.Attempts);
        Assert.Equal(
            AttemptsExceededAction.Fail,
            retry.OnAttemptsExceeded);
        Assert.Equal(
            [60, 300, 900],
            retry.DelaysInSeconds);
        Assert.Equal(
            600,
            concurrency.TimeoutSec);
    }
}
