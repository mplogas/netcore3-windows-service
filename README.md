# Building a Windows Service with netcore 3.0.0 preview

## Intro
A netcore app can be hosted on Windows as a Windows Service without using IIS. When being hosted as Windows Service, the app automatically starts after reboot. Steve Gordon [posted](https://www.stevejgordon.co.uk/running-net-core-generic-host-applications-as-a-windows-service) a nice way for netcore 2.2, but with netcore 3 things got a little easier.

Source code can be found on [GitHub](https://github.com/mplogas/netcore3-windows-service)

## Preparation
1. Get netcore 3.0.0 preview (obviously) and install it
2. make sure you are using the installed version by issueing 
    ```
    λ dotnet --version
    ```
    a. if you're still seeing something like ```2.2.x``` or ```2.1.x``` add a ```global.json``` to your current folder
    b. insert the following JSON and make sure the version declared in the JSON matches your installed version
    ```json
    {
      "sdk": {
        "version": "3.0.100-preview3-010431"
      }
    }
    ```
3. create a new dotnet project using the **Worker Service** template: 
    ```sh
    λ dotnet new worker
    ```
4. Open the ```*.csproj``` file and modify the ```<PropertyGroup>``` by adding the following lines to it. It sets the **runtime identifier (RID)** to a specific target platform (*goodbye x-plat netcore o7*) and disables the creation of a ```web.config``` file.
    ```xml
    <PropertyGroup>
        ...
        <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
        <IsTransformWebConfigDisabled>true</IsTransformWebConfigDisabled>
    </PropertyGroup>
    ```
5. Add the following package references. Both ```Microsoft.Extensions.Hosting.*``` references are mandatory, the rest is optional but recommended. (Who doesn't like some good logging and DI?)
    ```
    Microsoft.Extensions.Hosting
    Microsoft.Extensions.Hosting.WindowsServices
    Microsoft.Extensions.Logging
    Microsoft.Extensions.Logging.Console
    Microsoft.Extensions.Configuration
    Microsoft.Extensions.Configuration.FileExtensions
    Microsoft.Extensions.Configuration.Json
    Microsoft.Extensions.DependencyInjection
    ```
    
## Configuring the Service

6. Open ```Program.cs``` in an editor of your choice.
7. Behold the wonders of C# 7.1! Have an *async* Main method! 
    ```
    public static async Task Main(string[] args)
    ```
8. Clean the Main method from any calls
9. Optional: when the program needs to keep track of the starting option, this is the place.
    ```
    var isService = !(System.Diagnostics.Debugger.IsAttached || args.Contains("--console"));
    ```
10. Create a ```HostBuilder``` instance and set up the host configuration. It's required to set the basepath of the host, because, by default, a Windows service returns the ```C:\WINDOWS\system32``` folder when GetCurrentDirectory is called
    ```cs
    var builder = new HostBuilder()
        .ConfigureHostConfiguration(configHost =>
        {
            configHost.SetBasePath(Directory.GetCurrentDirectory());
        })
    ```
11. Optional: when the program is configured using the ```Microsoft.Extensions.Configuration```package, call ```ConfigureAppConfiguration```
    ```cs
    .ConfigureAppConfiguration((hostContext, configApp) =>
        {
            configApp.SetBasePath(Directory.GetCurrentDirectory());
            configApp.AddJsonFile(pathToAppSettings, true);
        })
    ```
12. Optional: Who doesn't like logging? Let's configure that as well.
    ```cs
    .ConfigureLogging((hostContext, configLogging) =>
        {
            configLogging.AddConsole();
        });
    ```
13. And finally, the important bit that makes the app run as a service. Register the worker that implements the service methods. For that, the class has to extend the ```BackgroundService``` base class or implement both interfaces ```IHostedService``` and ```IDisposable```. Activate the ```ServiceBaseLifeTime``` to register the ``ÌHostLifetime``` implementation provided by the nuget package. 
    ```cs
    .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddHostedService<Worker>();
        })
    .UseServiceBaseLifetime()
    ```
14. You don't need to differentiate between console mode and service mode anymore, as the ```ServiceBaseLifetimeHostBuilderExtensions``` takes care of that now ([Github](https://github.com/aspnet/Extensions/blob/master/src/Hosting/WindowsServices/src/WindowsServiceLifetimeHostBuilderExtensions.cs)).

## Install the service

15. Build the service in Release configuration
    ```sh
    λ dotnet build Sample.csp --configuration Release
    ```
16. Register the service on elevated commandline. Don't forget to have a space between the ```binPath=``` and the path!
    ```sh
    λ sc create TestWorker binPath=  "C:\tmp\worker\bin\Release\netcoreapp3.0\win7-x64\Sample.exe"
    ```
17. Start the service.
    ```sh
    λ sc start TestWorker
    
    SERVICE_NAME: TestWorker
            TYPE               : 10  WIN32_OWN_PROCESS
            STATE              : 2  START_PENDING
                                    (NOT_STOPPABLE, NOT_PAUSABLE, IGNORES_SHUTDOWN)
            WIN32_EXIT_CODE    : 0  (0x0)
            SERVICE_EXIT_CODE  : 0  (0x0)
            CHECKPOINT         : 0x0
            WAIT_HINT          : 0x7d0
            PID                : 25164
            FLAGS              :
    ```
