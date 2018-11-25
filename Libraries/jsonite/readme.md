# jsonite

Jsonite is a lightweight JSON serializer and deserializer for .NET

```C#
var obj = (JsonObject)Json.Deserialize(@"{""name"": ""John"", ""age"": 26}")
```

Jsonite provides the following features:

- The implementation *should be* [ECMA-404](http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf) and [RFC 4627](https://tools.ietf.org/html/rfc4627) compliant. If you find any issues please log an issue!
- Single file serializer/deserializer that can be embedded directly into a project.
- Default implementation serializing/deserializing from/to `JsonObject` / `JsonArray`
- Method `Json.Validate` to validate a json object
- Precise error with line/column when deserializing an invalid json text.
- Very fast and very low GC memory pressure when deserializing/serializing compare to other JSON libraries.
- Simple pluggable API to allow to deserialize/serialize from/to other kinds of .NET objects (through the `JsonReflector` class)
- Default implementation does not use Reflection or Expression to serialize/deserialize to .NET `JsonObject`/`JsonArray`.

Jsonite is easily embeddable for quickly decoding/encoding JSON without relying on an external Json library.

## Usage and Compilation

As this library is intended to be embedded and compiled directly from your project, we don't provide a nuget package.

Instead, you can for example use this repository as a git sub-module of your project and reference directly the file [`Jsonite.cs`](http://github.com/textamina/jsonite/tree/master/src/Textamina.Jsonite/Jsonite.cs)

The code is compatible with `PCL .NET 4.5+`, `CoreCLR`, `CoreRT` and `UWP10`.

## Limitations

Jsonite does not provide a deserializer/serializer from/to an arbitrary object graph. Prefers using a more complete solution like Json.NET.

## License
This software is released under the [BSD-Clause 2 license](http://opensource.org/licenses/BSD-2-Clause). 

## Author

Alexandre Mutel aka [xoofx](http://xoofx.com).
