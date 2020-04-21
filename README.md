# AWS CodeBuild Test Reporting with .NET Core

At AWS re:Invent 2019, [AWS CodeBuild](https://aws.amazon.com/codebuild/) announced a new test reporting feature which can help make diagnosing test failures in CodeBuild much easier. You can read more about it [here](https://aws.amazon.com/blogs/devops/test-reports-with-aws-codebuild/).

As of the time of writing this post CodeBuild supports JUnit XML or Cucumber JSON formatted files for creating test reports. I wanted to use this feature for .NET and after a little research I was able to quickly add support for .NET tests to my CodeBuild projects. Let's take a look at how I made this work.

## Project Setup

For demonstration purposes I have a test project with the following tests. I admit these tests don't really do anything but they will give me 3 passing tests and one failing test for a malformed Uri exception.

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

To run this project with CodeBuild I first started with a `buildspec.yml` file that built my project and ran my tests.

```yml
version: 0.2

phases:
    install:
        runtime-versions:
            dotnet: 3.1
    build:
        commands:
            - dotnet build -c Release ./CodeBuildDotnetTestReportExample/CodeBuildDotnetTestReportExample.csproj
            - dotnet test -c Release ./CodeBuildDotnetTestReportExample.Tests/CodeBuildDotnetTestReportExample.Tests.csproj
```

The first step to making my test reports was making sure the `dotnet test` command logged the test run. To do that I need to specify the logger format and where to put the logs. I changed the `dotnet test` command shown above to use the `trx` log format and to put the results in the `./testresults` directory.

```yml
            - dotnet test -c Release <project-path> --logger trx --results-directory ./testresults
```

## What to do with a `trx` format file?

As I mentioned earlier, test reporting from CodeBuild currently supports JUnit XML or Cucumber JSON formatted files. So what are we going to do with the `trx` format files that `dotnet test` created? The .NET community has created a .NET Core Global Tool called [trx2junit](https://www.nuget.org/packages/trx2junit/) that can be used to convert `trx` files into JUnit XML format files.

My next step was to modify my `buildspec.yml` file to install the `trx2junit` global tool and then run it on the `trx` files created by `dotnet test`. To do that I first updated the **install** phase of my `buildspec.yml` file to install **trx2junit**.

```yml
    install:
        runtime-versions:
            dotnet: 3.1
        commands:
            - dotnet tool install -g trx2junit
            - dotnet build -c Release ...
```

Next I added a **post_build** phase to convert the `trx` files in the **testresults** directory to JUnit XML files. Depending on the Docker image being used the `~/.dotnet/tools/` directory might not be in the **PATH** environment variable. If it is not then just executing **trx2junit** will fail because the executable can't be found. To ensure **trx2junit** can always be found I executed the tool using the full path relative to the home directory, bypassing the need for `~/.dotnet/tools/` to be in the **PATH** environment variable.

```yml
    post_build:
        commands:
            - ~/.dotnet/tools/trx2junit ./testresults/*
```

After converting the `trx` files I can now integrate .NET's test logging into CodeBuild's test reporting. The last update I needed to make to my `buildspec.yml` was to tell CodeBuild where to find the test logs using the **reports** section. Below is the final `buildspec.yml` I used, and inside the **reports** section you can see `DotnetTestExamples` was the name I chose for my test reports group for this project. You can read more about CodeBuild, and buildspec files, in the [CodeBuild User Guide](https://docs.aws.amazon.com/codebuild/latest/userguide/welcome.html).

```yml
version: 0.2

phases:
    install:
        runtime-versions:
            dotnet: 3.1
        commands:
            - dotnet tool install -g trx2junit
    build:
        commands:
            - dotnet build -c Release ./CodeBuildDotnetTestReportExample/CodeBuildDotnetTestReportExample.csproj
            - dotnet test -c Release ./CodeBuildDotnetTestReportExample.Tests/CodeBuildDotnetTestReportExample.Tests.csproj --logger trx --results-directory ./testresults
    post_build:
        commands:
            - ~/.dotnet/tools/trx2junit ./testresults/*
reports:
    DotnetTestExamples:
        files:
            - '**/*'
        base-directory: './testresults'
```

## Setting up the build project

A CodeBuild project can be configured with a variety of options, for example what source control provider should be used, or whether to run as a stand alone job, or run as part of a pipeline. For this post I'm going to keep it simple and create a standalone CodeBuild project pointing to a GitHub repository. Here are the steps I used to create the CodeBuild project.

* Sign in to the AWS Management Console and navigate to CodeBuild
* Click **Create build project**
* Set a project name
* Select the GitHub repository
* Configure the Environment image
  * Operating System = Amazon Linux 2
  * Runtime = Standard
  * Image = aws/codebuild/amazonlinux2-x86_64-standard:3.0
  * Service Role = New service role
* Click **Create build project** to finish

![alt text](./resources/build-setup.gif "CodeBuild project Setup")

Once the project is created we can start a build which will execute the **buildspec.yml** to build the project and run the tests.

## View test report

CodeBuild will capture the test reports identified in the **reports** section of the **buildspec.yml** file as the builds execute, and we can view the test reports identified in the **report group** I named `DotnetTestExamples` in the CodeBuild console. The example below shows my 4 tests ran, and which one failed. Clicking the failed test will give more details about the failure.

![alt text](./resources/report-overview.png "Test report")

## Conclusion

Adding test reports to your CodeBuild project makes it easier to diagnose CodeBuild jobs and you can see how, with just a few steps, you can add reporting to your .NET builds in [AWS CodeBuild](https://aws.amazon.com/codebuild/).
