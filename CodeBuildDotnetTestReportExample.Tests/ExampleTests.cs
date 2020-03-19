using System;
using Xunit;
using Xunit.Abstractions;

namespace CodeBuildDotnetTestReportExample.Tests
{
    public class ExampleTests
    {
        public ExampleTests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [Fact]
        public void TestSuccess1()
        {
            OutputHelper.WriteLine("TODO Test something");
        }


        [Fact]
        public void TestSuccess2()
        {
            OutputHelper.WriteLine("TODO Test another part of the application");
        }

        [Theory]
        [InlineData("https://www.google.com")]
        [InlineData("fffaaa")]
        public void TestMalformedUri(string uri)
        {
            OutputHelper.WriteLine($"Testing uri {uri}");
            new Uri(uri);
        }
    }
}
