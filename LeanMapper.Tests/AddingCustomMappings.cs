using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LeanMapper.Tests
{
    [TestClass]
    public class AddingCustomMappings
    {
        [TestMethod]
        public void Property_Is_Mapped_To_Different_Property_Successfully()
        {
            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            LeanMapper.Config<SimplePoco, SimpleDto>()
                .Ignore(p => p.Name)
                .MapProperty(p => p.AnotherName, d => d.Name);

            var dto = LeanMapper.Map<SimpleDto>(poco);

            Assert.AreEqual(poco.Id, dto.Id);
            Assert.IsNull(dto.Name);
            Assert.AreEqual(poco.Name, dto.AnotherName);
        }

        #region TestClasses

        private class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        private class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string AnotherName { get; set; }
        }

        #endregion
    }
}