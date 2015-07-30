# LimeBean

[![Build status](https://ci.appveyor.com/api/projects/status/o3lauspwhfk898o7?svg=true)](https://ci.appveyor.com/project/AlekseyMartynov/limebean)
[![Build Status](https://travis-ci.org/AlekseyMartynov/LimeBean.svg)](https://travis-ci.org/AlekseyMartynov/LimeBean)

[RedBeanPHP](http://redbeanphp.com/)-inspired data access layer for .NET, .NET Core (DNX/DNXCore), Mono and Xamarin.

## Available on NuGet Gallery

[![NuGet](https://img.shields.io/nuget/v/LimeBean.svg)](https://www.nuget.org/packages/LimeBean)

    PM> Install-Package LimeBean

## Supported Frameworks and Databases

              | .NET | [.NET Core](http://dotnet.github.io/core/) | Mono | Xamarin 
--------------|------|--------------------------------------------|------|---------
SQLite *      | +    | +                                          | +    | +
MySQL/MariaDB | +    | future                                     | +    | -
SQL Server    | +    | +                                          | +    | -
\* including System.Data.SQLite, Mono.Data.Sqlite and Microsoft.Data.Sqlite

## Synopsis

LimeBean treats your data entities as `IDictionary<string, IConvertible>` and maintains the database schema on the fly (when in fluid mode).

LimeBean does not use any Reflection, IL emitting, `dynamic`, etc. Instead it relies on strings, dictionaries and fragments of plain SQL.  

## Code Samples

* [Untyped CRUD](https://github.com/AlekseyMartynov/LimeBean/blob/master/LimeBean.Tests/Examples/Crud.cs)
* [Strongly-typed models with inter-bean links and lifecycle hooks](https://github.com/AlekseyMartynov/LimeBean/blob/master/LimeBean.Tests/Examples/Northwind.cs)
* [Usage in ASP.NET](https://github.com/AlekseyMartynov/LimeBean/blob/master/LimeBean.Tests/Examples/AspNet.cs)
* [GUID primary keys](https://github.com/AlekseyMartynov/LimeBean/blob/master/LimeBean.Tests/Examples/AutoGuidKeys.cs)

## API

All available properties and methods are exposed via the [BeanApi facade class](https://github.com/AlekseyMartynov/LimeBean/blob/master/LimeBean/BeanApi.cs).

## Limitations and Cautions

* Non thread-safe: use one API instance per thread, or maintain thread synchronization with locks. Read-only access is not thread-safe either because of the internal LRU cache.
* Property values must be `IConvertible`. Any other values have to be stored as strings.

![MIT License](https://img.shields.io/github/license/alekseymartynov/limebean.svg)
