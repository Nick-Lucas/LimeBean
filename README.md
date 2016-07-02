# LimeBean

[![Build status](https://ci.appveyor.com/api/projects/status/4oxjopx4mbre22ky/branch/master?svg=true)](https://ci.appveyor.com/project/Nick-Lucas/limebean/branch/master)
[![NuGet](https://img.shields.io/nuget/v/LimeBean.svg)](https://www.nuget.org/packages/LimeBean)
[![MIT License](https://img.shields.io/github/license/Nick-Lucas/limebean.svg)](https://raw.githubusercontent.com/Nick-Lucas/LimeBean/master/LICENSE.txt)

[RedBeanPHP](http://redbeanphp.com/)-inspired Hybrid-ORM for .NET. 

## Available on NuGet Gallery

    PM> Install-Package LimeBean

## Get started in 2 minutes:
```c#
// Make an ADO.Net connection and create a BeanAPI from it.
var connection = new DbConnection(connectionString);
connection.Open();
var api = new BeanApi(connection);

// Get a row from a known record ID
int bookId = 7;
Bean row = api.Load("books", bookId);
string bookTitle = row.Get<string>("title");
Console.WriteLine(bookTitle);

// Add a new row to the database
Bean newRow = api.Dispense("books");
newRow
    .Put("title", "Cloud Atlas")
    .Put("author", "David Mitchell");
var newBookId = api.Store(newRow);
Console.WriteLine("New book ID: " + newBookId.ToString());
```

## Supported Frameworks and Databases

              | .NET | .NET Core 
--------------|------|-----------
SQLite        | +    | + 
MySQL/MariaDB | +    | - 
PostgreSQL    | +    | + 
SQL Server    | +    | + 

## Full Documentation

https://nick-lucas.github.io/LimeBean/
