# ABP Platform 5.0 Beta 1 Has Been Released

Today, we are excited to release the [ABP Framework](https://abp.io/) and the [ABP Commercial](https://commercial.abp.io/) version **5.0 Beta 1**. This blog post introduces the new features and important changes in this new version.

> **The planned release date for the [5.0.0 Release Candidate](https://github.com/abpframework/abp/milestone/51) version is November, 2021**.

## Get Started with the 5.0 Beta 1

If you want to try the version 5.0.0 today, follow the steps below;

1) **Upgrade** the ABP CLI to the version `5.0.0-beta.1` using a command line terminal:

````bash
dotnet tool update Volo.Abp.Cli -g --version 5.0.0-beta.1
````

**or install** if you haven't installed before:

````bash
dotnet tool install Volo.Abp.Cli -g --version 5.0.0-beta.1
````

2) Create a **new application** with the `--preview` option:

````bash
abp new BookStore --preview
````

See the [ABP CLI documentation](https://docs.abp.io/en/abp/latest/CLI) for all the available options.

> You can also use the *Direct Download* tab on the [Get Started](https://abp.io/get-started) page by selecting the **Preview checkbox**.

### Migration Notes & Breaking Changes

This is a major version and there are some breaking changes and upgrade steps. Please see the [migration document](https://docs.abp.io/en/abp/5.0/Migration-Guides/Abp-5_0) for all the details.

Here, a list of important breaking changes in this version:

* Upgraded to .NET 6.0-rc.1, so you need to move your solution to .NET 6.0 if you want to use the ABP 5.0.
* `IRepository` doesn't inherit from `IQueryable` anymore. It was already made obsolete in 4.2.
* Removed NGXS and states from the Angular UI.
* Removed gulp dependency from the MVC / Razor Pages UI in favor of `abp install-libs` command of ABP CLI.

You can see all [the closed issues and pull request](https://github.com/abpframework/abp/releases/tag/5.0.0-beta.1) on GitHub.

## What's new with Beta 1?

In this section, I will introduce some major features released with beta 1.

### Static (Generated) Client Proxies for C# and JavaScript

Dynamic C# ([see](https://docs.abp.io/en/abp/latest/API/Dynamic-CSharp-API-Clients)) and JavaScript ([see](https://docs.abp.io/en/abp/latest/UI/AspNetCore/Dynamic-JavaScript-Proxies)) client proxy system is one of the most loved features of the ABP Framework. It generates the proxy code on runtime and makes client-to-server calls a breeze. With ABP 5.0, we are providing an alternative approach: You can generate the client proxy code on development-time.

Development-time client proxy generation has a performance advantage since it doesn't need to obtain the HTTP API definition on runtime. It also makes it easier to consume a (micro)service behind an API Gateway. With dynamic proxies, we should return a combined HTTP API definition from the API Gateway and need to add HTTP API layers of the microservices to the gateway. Static proxies removes this requirement. One disadvantage is that you should re-generate the client proxy code whenever you change your API endpoint definition. Yes, software development always has tradeoffs :)

Working with static client proxy generation is simple with the ABP CLI. You first need to run the server application, open a command-line terminal, locate to the root folder of your client application, then run the `generate-proxy` command of the ABP CLI.

#### Creating C# client proxies

C# client proxies are useful to consume APIs from Blazor WebAssembly applications. It is also useful for microservice to microservice HTTP API calls. Notice that the client application need to have a reference to the application service contracts (typically, the `.Application.Contracts` project in your solution) in order to consume the services.

**Example usage:**

````bash
abp generate-proxy -t csharp -u https://localhost:44305
````

`-t` indicates the client type, C# here. `-u` is the URL of the server application. It creates the proxies for the application (the app module) by default. You can specify the module name as `-m <module-name>` if you are building a modular system. The following figure shows the generated files:

![csharp-proxies](csharp-proxies.png)

Once the proxies are generated, you can inject and use the application service interfaces of these proxies, like `IProductAppService` in this example. Usage is same of the [dynamic C# client proxies](https://docs.abp.io/en/abp/latest/API/Dynamic-CSharp-API-Clients).

#### Creating JavaScript client proxies

JavaScript proxies are useful to consume APIs from MVC / Razor Pages UI. It works on JQuery AJAX API, just like the [dynamic JavaScript proxies](https://docs.abp.io/en/abp/latest/UI/AspNetCore/Dynamic-JavaScript-Proxies).

**Example usage:**

````bash
abp generate-proxy -t js -u https://localhost:44305
````

The following figure shows the generated file:

![js-proxies](js-proxies.png)

Then you can consume the server-side APIs from your JavaScript code just like the [dynamic JavaScript proxies](https://docs.abp.io/en/abp/latest/UI/AspNetCore/Dynamic-JavaScript-Proxies).

#### Creating Angular client proxies

Angular developers already knows that the generate-proxy command was already available in ABP's previous versions to create client-side proxy services in the Angular UI. The same functionality continues to be available and already [documented here](https://docs.abp.io/en/abp/latest/UI/Angular/Service-Proxies).

Example usage:

````bash
abp generate-proxy -t ng
````

URL is not required here. It gets the URL from the application's configuration by default. See [the documentation](https://docs.abp.io/en/abp/latest/UI/Angular/Service-Proxies) for more information.

### Transactional Outbox & Inbox for the Distributed Event Bus

This was one of the most awaited features by distributed software developers.

The [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) is used to publishing distributed events within the same transaction that manipulates the application database. Distributed events are saved into the database inside the same transaction with your data changes, then sent to the message broker (like RabbitMQ or Kafka) by a separate background worker with a re-try system. In this way, it ensures the consistency between your database state and the published events.

The transactional inbox pattern, on the other hand, saves incoming events into database first, then executes the event handler in a transactional manner and removes the event from the inbox queue in the same transaction. It also ensures that the event is only executed one time by keeping the processed messages for a while and discarding the duplicate events received from the message broker.

Enabling the inbox and outbox patterns requires a few manual steps for your application. We've created a simple [console application example](https://github.com/abpframework/abp/tree/dev/test/DistEvents), but I will also explain all the steps here.

#### Pre-requirements

First of all, you need to have EF Core or MongoDB installed into your solution. It should be already installed if you'd created a solution from the ABP startup template.

#### Install the package

Install the new [Volo.Abp.EventBus.Boxes](https://www.nuget.org/packages/Volo.Abp.EventBus.Boxes) NuGet package to your database layer (to `EntityFrameworkCore` or `MongoDB` project) or to the host application. Open a command-line terminal at the root directory of your database (or host) project and execute the following command:

````csharp
abp add-package Volo.Abp.EventBus.Boxes
````

This will install the package and setup the ABP module dependency.

#### Configure the DbContext

Open your `DbContext` class, implement the `IHasEventInbox` and the `IHasEventOutbox` interfaces. You should end up by adding two `DbSet` properties into your `DbContext` class:

````csharp
public DbSet<IncomingEventRecord> IncomingEvents { get; set; }
public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; }
````

Add the following lines inside the `OnModelCreating` method of your `DbContext` class:

````csharp
builder.ConfigureEventInbox();
builder.ConfigureEventOutbox();
````

It is similar for MongoDB; implement the `IHasEventInbox` and the `IHasEventOutbox` interfaces. There is no `Configure...` method for MongoDB.

Now, we've added inbox/outbox related tables to our database schema. Now, for EF Core, use the standard `Add-Migration` and `Update-Database` commands to apply changes into your database (you can skip this step for MongoDB). If you want to use the command-line terminal, run the following commands in the root directory of the database integration project:

````bash
dotnet ef migrations add "Added_Event_Boxes"
dotnet ef database update
````

#### Configure the distributed event bus options

As the last step, write the following configuration code inside the `ConfigureServices` method of your module class:

````csharp
Configure<AbpDistributedEventBusOptions>(options =>
{
    options.Outboxes.Configure(config =>
    {
        config.UseDbContext<YourDbContext>();
    });
    
    options.Inboxes.Configure(config =>
    {
        config.UseDbContext<YourDbContext>();
    });
});
````

Replace `YourDbContext` with your `DbContext` class.

That's all. You can continue to publishing and consuming events just as before. See the [distributed event bus documentation](https://docs.abp.io/en/abp/latest/Distributed-Event-Bus) if you don't know how to use it.

### Publishing events in transaction

TODO

