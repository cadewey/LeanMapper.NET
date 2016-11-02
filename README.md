LeanMapper.NET [![Build status](https://ci.appveyor.com/api/projects/status/5xvsx37da4ht6jcu/branch/master?svg=true)](https://ci.appveyor.com/project/eldarerathis/leanmapper/branch/master)
===========

A lightweight, easy to use object mapper for .NET.

### Using LeanMapper.NET

LeanMapper.NET is designed with **simplicity and speed** in mind first and foremost. It aims to eliminate boilerplate code as much as possible.

Using LeanMapper.NET on simple objects does not even require an explicit configuration to be registered beforehand. Simply map your objects and LeanMapper will generate the necessary logic the first time it's called. It can even handle common conversions like enums to ints and objects to strings:

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

### Customizing mapping configurations

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

LeanMapper.Mapper.Configure<ModelObject, DomainObject>()
    .Ignore(d => d.Id) // Don't map onto the Id property
    .MapProperty(d => d.SeparatedValues, m => m.DelimitedValues.Split(',')) // Custom mapping onto SeparatedValues
    .AfterMapping((d, m) =>
    {
        d.Id = SomeLogic(d.SeparatedValues[0]); // Values set during mapping are available here
    });
```

### Inheritence and mapping

Another place where LeanMapper.NET tries to reduce boilerplate code is in how it handles mapping of derived types. If you need to apply a custom mapping to a field on a base type, you can simply configure the mapping
once and LeanMapper.NET will attempt to "flatten" the hierarchy during mapping; that means you don't need to add custom configurations for each of your derived types individually!

```csharp
interface IDto
{
    DateTime Timestamp { get; set; }
}

class SimplePocoBase
{
    public string ValueString { get; set; }
    public string Timestamp { get; set; }
}

class SimpleDtoBase
{
    public int Value { get; set; }
}

class SimplePoco : SimplePocoBase
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

class SimpleDto : SimpleDtoBase, IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
}

// Configure the mappings between our base types
LeanMapper.Mapper.Config<SimplePocoBase, SimpleDtoBase>()
    .MapProperty(d => d.Value, p => Int32.Parse(p.ValueString));

// You can also map an interface if you need to
LeanMapper.Mapper.Config<SimplePocoBase, IDto>()
    .MapProperty(d => d.Timestamp, p => DateTime.Parse(p.Timestamp));

// Now the mapper knows everything it needs about the properties on the derived types
var dto = LeanMapper.Mapper.Map<SimplePoco, SimpleDto>(poco);
```

### Hat Tips

* [Mapster](https://github.com/eswann/Mapster): Provided the basis for a number of unit tests and benchmark tests