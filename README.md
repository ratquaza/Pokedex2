# Pokedex2
Lightweight open source C# Library Pokedex that loads Pokemon data through PokeAPI.

Currently, the Pokedex only holds minimal data on each Pokemon. 

## Basic Usage
```csharp
Pokemon rayquaza = Pokedex.ByName("Rayquaza"); // If this is the first request for Rayquaza, Pokedex2 will make a GET request and process the data.
Pokemon rayquazaOther = Pokedex.ByName("Rayquaza"); // Rayquaza's data exists in the registry by now, so no GET request will be made
```

## Dependencies
[Newtonsoft.Json v.13.0.1 or later](https://www.newtonsoft.com/json)

## Building
Install with NuGet OR build with Visual Studio and modify to any extent.
