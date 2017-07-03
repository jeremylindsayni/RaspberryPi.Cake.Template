#addin "Cake.Putty"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var framework = Argument("framework", "netcoreapp2.0");

///////////////////////////////////////////////////////////////////////
// PARAMETERS FOR WINDOWS
///////////////////////////////////////////////////////////////////////
//var runtime = Argument("runtime", "win10-arm");
//var os = Argument("os", "windows");
//var destinationIp = Argument("destinationPi", "192.168.1.125");
//var destinationDirectory = Argument("destinationDirectory", @"c$\ConsoleApps\Test");

///////////////////////////////////////////////////////////////////////
// PARAMETERS FOR LINUX
///////////////////////////////////////////////////////////////////////
var runtime = Argument("runtime", "ubuntu.16.04-arm");
var os = Argument("os", "ubuntu");
var destinationIp = Argument("destinationPi", "192.168.1.110");
var destinationDirectory = Argument("destinationDirectory", @"/home/ubuntu/ConsoleApps/Test");
var username = Argument("username", "ubuntu");
var executableName = Argument("executableName", "SamplePi");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var binaryDir = Directory("./bin");
var objectDir = Directory("./obj");
var publishDir = Directory("./publish");
var projectFile = "./" + executableName + ".csproj";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(binaryDir);
        CleanDirectory(objectDir);
        CleanDirectory(publishDir);
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreRestore(projectFile);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var settings = new DotNetCoreBuildSettings
        {
            Framework = framework,
            Configuration = configuration,
            OutputDirectory = "./bin/"
        };

        DotNetCoreBuild(projectFile, settings);
    });

Task("Publish")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var settings = new DotNetCorePublishSettings
        {
            Framework = framework,
            Configuration = configuration,
            OutputDirectory = "./publish/",
            Runtime = runtime
        };
                    
        DotNetCorePublish(projectFile, settings);
    });

Task("Deploy")
    .IsDependentOn("Publish")
    .Does(() =>
    {
        var files = GetFiles("./publish/*");
        
        if(runtime.StartsWith("win")) 
        {
            var destination = @"\\" + destinationIp + @"\" + destinationDirectory;
            CopyFiles(files, destination, true);
        }
        else
        {
            var destination = destinationIp + ":" + destinationDirectory;
            var fileArray = files.Select(m => m.ToString()).ToArray();
            Pscp(fileArray, destination, new PscpSettings
                                                { 
                                                    SshVersion = SshVersion.V2, 
                                                    User = username 
                                                }
            );

            var plinkCommand = "chmod u+x,o+x " + destinationDirectory + "/" + executableName;
            Plink(username + "@" + destination, plinkCommand);
        }
    });

Task("Default")
    .IsDependentOn("Deploy");

RunTarget(target);