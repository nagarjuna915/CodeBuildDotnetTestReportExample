# AWS CodeBuild Test Reporting with .NET Core

As part of AWS re:Invent 2019 the CodeBuild team announced a new test reporting feature which can help make diagnosing test failures in CodeBuild so much easier. You can read more about [here](https://aws.amazon.com/blogs/devops/test-reports-with-aws-codebuild/).

As of the time of writing this CodeBuild supports JUnit XML and Cucumber JSON formatted files for creating test reports. I wanted to use this feature for .NET and after a little research was quickly able to make view my test reports for my .NET Core project in the CodeBuild console. Let's take a look at how I made this work.

## Project Setup

For demonstration purpose I have a test which these tests. I admit in these tests don't really do anything but they will give me 3 passing tests and one failing test for a malformed uri exception.

```csharp
using System;
using System.Net;
using Xunit;

namespace CodeBuildDotnetTestReportExample.Tests
{
    public class ExampleTests
    {
        [Fact]
        public void TestSuccess1()
        {
            
        }
        
        
        [Fact]
        public void TestSuccess2()
        {
            
        }

        [Theory]
        [InlineData("https://www.google.com")]
        [InlineData("fffaaa")]
        public void TestMalformedUri(string uri)
        {
            new Uri(uri);
        }
    }
}
```

Now to run this project CodeBuild I first started with a `buildspec.yml` file that built my project and ran my tests.

```yml
version: 0.2

phases:
    install:
        runtime-versions:
            dotnet: 2.2
    build:
        commands:
            - dotnet build -c Release ./CodeBuildDotnetTestReportExample/CodeBuildDotnetTestReportExample.csproj
            - dotnet test -c Release ./CodeBuildDotnetTestReportExample.Tests/CodeBuildDotnetTestReportExample.Tests.csproj
```

The first step to making this work was making sure the `dotnet test` command logged the test run. To do that I need to specify the logger format and where to put the logs. I changed the `dotnet test` command to look like this to use the `trx` log format and put the results in the `../testresults` directory.

```yml
            - dotnet test -c Release ./CodeBuildDotnetTestReportExample.Tests/CodeBuildDotnetTestReportExample.Tests.csproj --logger trx --results-directory ../testresults

```

## What do I do with a trx file?

As I mentioned before CodeBuild's test reporting supports JUnit XML and Cucumber JSON formatted files. So what are we going to do with trx files which `dotnet test` created.? The .NET community has created a .NET Core Global Tool called [trx2junit](https://www.nuget.org/packages/trx2junit/) to convert trx files into JUnit xml files.

What we need to do now in our `buildspec.yml` file is install trx2junit and run it on the trx files created by dotnet test. To do that I updated the **install** phase of my `buildspec.yml` file to install **trx2junit**.

```yml
    install:
        runtime-versions:
            dotnet: 2.2
        commands:
            - dotnet tool install -g trx2junit
```

With **trx2unit** installed I added a **post_build** phase to convert the trx files in the **testresults** directory to JUnit xml files. Depending on your Docker image used the `~/.dotnet/tools/` directory might not be in the **PATH** environment variable. If its not then just executing **trx2junit** will fail because the executable can't be found. To ensure trx2junit can always be found I executed the tool using full path relative to the home directory and ignore the need for `~/.dotnet/tools/` being in the **PATH** environment variable.

```yml
    post_build:
        commands:
            - ~/.dotnet/tools/trx2junit ./testresults/*
```

After doing these changes to convert the trx files from `dotnet test` into JUnit XML files we can integrate .NET's test logging in CodeBuild's test reporting. The last update I needed to make to my `buildspec.yml` was to tell CodeBuild where to find the test logs using the **reports** section. Below is the full `buildspec.yml` file including the **report** section.

```yml
version: 0.2

phases:
    install:
        runtime-versions:
            dotnet: 2.2
        commands:
            - dotnet tool install -g trx2junit
    build:
        commands:
            - dotnet build -c Release ./CodeBuildDotnetTestReportExample/CodeBuildDotnetTestReportExample.csproj
            - dotnet test -c Release ./CodeBuildDotnetTestReportExample.Tests/CodeBuildDotnetTestReportExample.Tests.csproj --logger trx --results-directory ../testresults
    post_build:
        commands:
            - ~/.dotnet/tools/trx2junit ./testresults/*
reports:
    DotnetTestExamples:
        files:
            - '**/*'
        base-directory: './testresults'          
```

## Examining the test report

