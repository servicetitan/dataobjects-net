// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.12.10

using System;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Core.Tests.ObjectMapping.TargetModel
{
  public class PersonDto : ICloneable
  {
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateTime BirthDate { get; set; }

    public virtual object Clone()
    {
      return new PersonDto {BirthDate = BirthDate, FirstName = FirstName, Id = Id, LastName = LastName};
    }
  }

  public class OrderDto : ICloneable
  {
    public String Id { get; set; }

    public PersonDto Customer { get; set; }

    public DateTime ShipDate { get; set; }

    public object Clone()
    {
      return new OrderDto {Customer = (PersonDto) Customer.Clone(), Id = Id, ShipDate = ShipDate};
    }
  }

  public class AuthorDto : ICloneable
  {
    public Guid Id { get; set; }

    public string Name { get; set; }

    public BookDto Book { get; set; }

    public object Clone()
    {
      return new AuthorDto {Book = (BookDto) Book.Clone(), Id = Id, Name = Name};
    }
  }

  public class BookDto : ICloneable
  {
    public string ISBN { get; set; }

    public TitleDto Title { get; set; }

    public string TitleText { get; set; }

    public double Price { get; set; }

    public object Clone()
    {
      return new BookDto {ISBN = ISBN, Price = Price, Title = (TitleDto) Title.Clone(), TitleText = TitleText};
    }
  }

  public class TitleDto : ICloneable
  {
    public Guid Id { get; set; }

    public string Text { get; set; }

    public object Clone()
    {
      return new TitleDto {Id = Id, Text = Text};
    }
  }

  public class PetOwnerDto : PersonDto
  {
    public List<AnimalDto> Pets { get; set; }

    public override object Clone()
    {
      var result = new PetOwnerDto
      {
        BirthDate = BirthDate, FirstName = FirstName, Id = Id, LastName = LastName,
        Pets = new List<AnimalDto>(Pets.Select(a => (AnimalDto) a.Clone()))
      };
      return result;
    }
  }

  public class AnimalDto : ICloneable
  {
    public Guid Id { get; set; }
    
    public string Name { get; set; }

    public object Clone()
    {
      return new AnimalDto {Id = Id, Name = Name};
    }
  }

  public class CatDto : AnimalDto
  {
    public int Age { get; set; }
  }

  public class SpiderDto : AnimalDto
  {
    public int LegCount { get; set; }
  }

  public class IgnorableDto
  {
    public Guid Id { get; set; }

    public string Auxiliary { get; set; }

    public string Ignored { get; set; }
  }
}