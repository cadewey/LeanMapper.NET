using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LeanMapper.Tests.Classes;

namespace LeanMapper.Tests
{
    #region Test Object

    public class ConfigA
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigB
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigC
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigD
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigE
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }

    public class ConfigF
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }

    public class Source
    {
        public int Level { get; set; }
        public IList<Source> Children { get; set; }
        public Source Parent { get; set; }

        public Source(int level)
        {
            Children = new List<Source>();
            Level = level;
        }

        public void AddChild(Source child)
        {
            Children.Add(child);
            child.Parent = this;
        }
    }

    public class Destination
    {
        public int Level { get; set; }
        public IList<Destination> Children { get; set; }
        public Destination Parent { get; set; }
    }



    #endregion

    [TestClass]
    public class MappingConfigurations
    {
        [TestMethod]
        public void IgnoreValueMemberTest()
        {
            var currentDate = DateTime.Now;

            var objA = new ConfigA()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            LeanMapper.Config<ConfigA, ConfigB>()
                .Ignore(dest => dest.Id);

            var objB = LeanMapper.Map<ConfigA, ConfigB>(objA);

            Assert.IsNotNull(objB);
            Assert.IsTrue(objB.Id == 0 && objB.FullName == null && objB.BirthDate == currentDate);
        }

        [TestMethod]
        public void IgnoreReferenceMemberTest()
        {
            var currentDate = DateTime.Now;

            var objA = new ConfigA()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            LeanMapper.Config<ConfigA, ConfigC>()
                .Ignore(dest => dest.Name)
                .Ignore(dest => dest.Surname);

            var objC = LeanMapper.Map<ConfigA, ConfigC>(objA);

            Assert.IsNotNull(objC);
            Assert.IsTrue(objC.Id == objA.Id && objC.BirthDate == currentDate);
            Assert.IsTrue(objC.Name == null && objC.Surname == null);
        }

        [TestMethod]
        public void MapFromTest()
        {
            var currentDate = DateTime.Now;

            var objC = new ConfigC()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            LeanMapper.Config<ConfigC, ConfigD>()
                .MapProperty(dest => dest.FullName, src => string.Concat(src.Name, " ", src.Surname));

            var objD = LeanMapper.Map<ConfigC, ConfigD>(objC);

            Assert.IsNotNull(objD);
            Assert.IsTrue(objD.Id == 1 && objD.FullName == "Timuçin KIVANÇ" && objD.BirthDate == currentDate);
        }

        [TestMethod]
        public void AfterMapping_SimpleType()
        {
            var currentDate = DateTime.Now;

            var objC = new ConfigC()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            LeanMapper.Config<ConfigC, ConfigD>()
                .AfterMapping((s, d) => d.Id += 1000);

            var objD = LeanMapper.Map<ConfigC, ConfigD>(objC);

            Assert.IsNotNull(objD);
            Assert.AreEqual(1001, objD.Id);
        }

        [TestMethod]
        public void AfterMapping_CollectionType()
        {
            var objE = new ConfigE
            {
                Id = 42,
                Name = "Test Object",
                Values = new List<string> { "First", "Second", "Third" }
            };

            LeanMapper.Config<ConfigE, ConfigF>()
                .Ignore(o => o.Values)
                .AfterMapping((s, d) =>
                {
                    d.Values = new List<string>();

                    foreach (var v in s.Values)
                    {
                        d.Values.Add(v + " Value");
                    }
                });

            var objF = LeanMapper.Map<ConfigE, ConfigF>(objE);

            Assert.IsNotNull(objF);
            Assert.AreEqual(objE.Id, objF.Id);
            Assert.AreEqual(objE.Name, objF.Name);
            CollectionAssert.AreEqual(objF.Values, new []{ "First Value", "Second Value", "Third Value"});
        }


        [TestMethod]
        public void NewInstanceTest()
        {
            TestNewInstanceA obj = new TestNewInstanceA();
            obj.Name = "Tim";
            obj.Child = new TestNewInstanceC() { Name = "Kıvanç" };

            var newObj = LeanMapper.Map<TestNewInstanceA, TestNewInstanceB>(obj);

            Assert.IsTrue(newObj.Name == "Tim");
            Assert.IsTrue(obj.Child.Name == newObj.Child.Name);

            obj.Child.Name = "İstanbul";

            Assert.IsTrue(obj.Child.Name != newObj.Child.Name);
        }

        #region Data

        private Source _source;
        public void Initializer()
        {
            var nest = new Source(1);

            nest.AddChild(new Source(2));
            nest.Children[0].AddChild(new Source(3));
            nest.Children[0].AddChild(new Source(3));
            nest.Children[0].Children[1].AddChild(new Source(4));
            nest.Children[0].Children[1].AddChild(new Source(4));
            nest.Children[0].Children[1].AddChild(new Source(4));

            nest.AddChild(new Source(2));
            nest.Children[1].AddChild(new Source(3));

            nest.AddChild(new Source(2));
            nest.Children[2].AddChild(new Source(3));

            _source = nest;
        }

        #endregion
    }
}