LeanMapper [![Build status](https://ci.appveyor.com/api/projects/status/5xvsx37da4ht6jcu/branch/master?svg=true)](https://ci.appveyor.com/project/eldarerathis/leanmapper/branch/master)
===========

A lightweight, easy to use object mapper for .NET.

### Using LeanMapper

LeanMapper is designed with **simplicity and speed** in mind first and foremost. It aims to eliminate boilerplate code as much as possible.

Using LeanMapper on simple objects does not even require an explicit configuration to be registered beforehand. Simply map your objects and LeanMapper will generate the necessary logic the first time it's called. It can even handle common conversions like enums to ints and objects to strings:

```csharp
enum StatusType
{
    InProgress,
    Complete,
    Cancelled
}

class DomainObject
{
    int Id { get; set; }
    string Name { get; set; }
    DateTime TimeStamp { get; set; }
    int Status { get; set; }
}

class ModelObject
{
    int Id { get; set; }
    string Name { get; set; }
    DateTime TimeStamp { get; set; }
    StatusType Status { get; set; } 
}

// ... Retrieve some ModelObject modelObject ...
DomainObject domain = LeanMapper.Mapper.Map<ModelObject, DomainObject>(modelObject);
```

Custom configurations are fine too, and can be created using in a fluent-style when needed. LeanMapper supports custom mapping logic for properties, ignoring fields, and performing actions after mapping has finished:

```csharp
class DomainObject
{
    Guid Id { get; set; }
    string[] SeparatedValues { get; set; }
}

class ModelObject
{
    string DelimitedValues { get; set; }
}

LeanMapper.Configure<ModelObject, DomainObject>()
    .Ignore(d => d.Id) // Don't map onto the Id property
    .MapProperty(d => d.SeparatedValues, m => m.DelimitedValues.Split(',')) // Custom mapping onto SeparatedValues
    .AfterMapping((d, m) =>
    {
        d.Id = SomeLogic(d.SeparatedValues[0]); // Values set during mapping are available here
    });
```

### Hat Tips

* [Mapster](https://github.com/eswann/Mapster): Provided the basis for a number of unit tests and benchmark tests