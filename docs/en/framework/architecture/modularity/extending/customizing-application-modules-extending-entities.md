# Customizing the Application Modules: Extending Entities

In some cases, you may want to add some additional properties (and database fields) for an entity defined in a depended module. This section will cover some different approaches to make this possible.

## Extra Properties

[Extra properties](../../domain-driven-design/entities.md) is a way of storing some additional data on an entity without changing it. The entity should implement the `IHasExtraProperties` interface to allow it. All the aggregate root entities defined in the pre-built modules implement the `IHasExtraProperties` interface, so you can store extra properties on these objects.

Example:

````csharp
//SET AN EXTRA PROPERTY
var user = await _identityUserRepository.GetAsync(userId);
user.SetProperty("Title", "My custom title value!");
await _identityUserRepository.UpdateAsync(user);

//GET AN EXTRA PROPERTY
var user = await _identityUserRepository.GetAsync(userId);
return user.GetProperty<string>("Title");
````

This approach is very easy to use and available out of the box. No extra code needed. You can store more than one property at the same time by using different property names (like `Title` here).

Extra properties are stored as a single `JSON` formatted string value in the database for the EF Core. For MongoDB, they are stored as separate fields of the document.

See the [entities document](../../domain-driven-design/entities.md) for more about the extra properties system.

> It is possible to perform a **business logic** based on the value of an extra property. You can [override a service method](./customizing-application-modules-overriding-services.md), then get or set the value as shown above.

## Entity Extensions (EF Core)

As mentioned above, all extra properties of an entity are stored as a single JSON object in the database table. This is not so natural especially when you want to;

* Create **indexes** and **foreign keys** for an extra property.
* Write **SQL** or **LINQ** using the extra property (search table by the property value, for example).
* Creating your **own entity** maps to the same table, but defines an extra property as a **regular property** in the entity (see the [EF Core migration document](../../../data/entity-framework-core/migrations.md) for more).

To overcome the difficulties described above, ABP entity extension system for the Entity Framework Core that allows you to use the same extra properties API defined above, but store a desired property as a separate field in the database table.

Assume that you want to add a `SocialSecurityNumber` to the `IdentityUser` entity of the [Identity Module](../../../../modules/identity.md). You can use the `ObjectExtensionManager`:

````csharp
ObjectExtensionManager.Instance
    .MapEfCoreProperty<IdentityUser, string>(
        "SocialSecurityNumber",
        (entityBuilder, propertyBuilder) =>
        {
            propertyBuilder.HasMaxLength(32);
        }
    );
````

* You provide the `IdentityUser` as the entity name, `string` as the type of the new property, `SocialSecurityNumber` as the property name (also, the field name in the database table).
* You also need to provide an action that defines the database mapping properties using the [EF Core Fluent API](https://docs.microsoft.com/en-us/ef/core/modeling/entity-properties).

> This code part must be executed before the related `DbContext` used. The [application startup template](../../../../solution-templates/layered-web-application) defines a static class named `YourProjectNameEfCoreEntityExtensionMappings`. You can define your extensions in this class to ensure that it is executed in the proper time. Otherwise, you should handle it yourself.

Once you define an entity extension, you then need to use the standard [Add-Migration](https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/powershell#add-migration) and [Update-Database](https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/powershell#update-database) commands of the EF Core to create a code first migration class and update your database.

You can then use the same extra properties system defined in the previous section to manipulate the property over the entity.

## Creating a New Entity Maps to the Same Database Table/Collection

Another approach can be **creating your own entity** mapped to **the same database table** (or collection for a MongoDB database).

## Creating a New Entity with Its Own Database Table/Collection

Mapping your entity to an **existing table** of a depended module has a few disadvantages;

* You deal with the **database migration structure** for EF Core. While it is possible, you should extra care about the migration code especially when you want to add **relations** between entities.
* Your application database and the module database will be the **same physical database**. Normally, a module database can be separated if needed, but using the same table restricts it.

If you want to **loose couple** your entity with the entity defined by the module, you can create your own database table/collection and map your entity to your own table in your own database.

In this case, you need to deal with the **synchronization problems**, especially if you want to **duplicate** some properties/fields of the related entity. There are a few solutions;

* If you are building a **monolithic** application (or managing your entity and the related module entity within the same process), you can use the [local event bus](../../../infrastructure/event-bus/local) to listen changes.
* If you are building a **distributed** system where the module entity is managed (created/updated/deleted) on a different process/service than your entity is managed, then you can subscribe to the [distributed event bus](../../../infrastructure/event-bus/distributed) for change events.

Once you handle the event, you can update your own entity in your own database.

### Subscribing to Local Events

[Local Event Bus](../../../infrastructure/event-bus/local) system is a way to publish and subscribe to events occurring in the same application.

Assume that you want to get informed when a `IdentityUser` entity changes (created, updated or deleted). You can create a class that implements the `ILocalEventHandler<EntityChangedEventData<IdentityUser>>` interface.

````csharp
public class MyLocalIdentityUserChangeEventHandler :
    ILocalEventHandler<EntityChangedEventData<IdentityUser>>,
    ITransientDependency
{
    public async Task HandleEventAsync(EntityChangedEventData<IdentityUser> eventData)
    {
        var userId = eventData.Entity.Id;
        var userName = eventData.Entity.UserName; 
        
        //...
    }
}
````

* `EntityChangedEventData<T>` covers create, update and delete events for the given entity. If you need, you can subscribe to create, update and delete events individually (in the same class or different classes).
* This code will be executed in the **current unit of work**, the whole process becomes transactional.

> Reminder: This approach needs to change the `IdentityUser`  entity in the same process contains the handler class. It perfectly works even for a clustered environment (when multiple instances of the same application are running on multiple servers).

### Subscribing to Distributed Events

[Distributed Event Bus](../../../infrastructure/event-bus/distributed) system is a way to publish an event in one application and receive the event in the same or different application running on the same or different server.

Assume that you want to get informed when `Tenant` entity (of the [Tenant Management](../../../../modules/tenant-management.md) module) has created. In this case, you can subscribe to the `EntityCreatedEto<TenantEto>` event as shown in the following example:

````csharp
public class MyDistributedEventHandler :
    IDistributedEventHandler<EntityCreatedEto<TenantEto>>,
    ITransientDependency
{
    public async Task HandleEventAsync(EntityCreatedEto<TenantEto> eventData)
    {
        var tenantId = eventData.Entity.Id;
        var tenantName = eventData.Entity.Name;
        //...your custom logic
    }

    //...
}
````

This handler is executed only when a new tenant has been created. All the pre-built ABP [application modules](../../../../modules) define corresponding `ETO` types for their entities. So, you can easily get informed when they changes.

> Notice that ABP doesn't publish distributed events for an entity by default. Because it has a cost and should be enabled by intention. See the [distributed event bus document](../../../infrastructure/event-bus/distributed) to learn more.

## See Also

* [Migration System for the EF Core](../../../data/entity-framework-core/migrations.md)
* [Customizing the Existing Modules](./customizing-application-modules-guide.md)
