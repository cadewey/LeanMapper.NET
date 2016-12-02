using System;
using System.Collections.Generic;
using Xunit;

namespace LeanMapper.Tests
{
    [Collection("MainCollection")]
    public class AddingCustomMappings
    {
        [Fact]
        public void Property_Is_Mapped_To_Different_Property_Successfully()
        {
            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            Mapper.Config<SimplePoco, SimpleDto>()
                .Ignore(p => p.Name)
                .MapProperty(p => p.AnotherName, d => d.Name);

            var dto = Mapper.Map<SimplePoco, SimpleDto>(poco);

            Assert.Equal(poco.Id, dto.Id);
            Assert.Null(dto.Name);
            Assert.Equal(poco.Name, dto.AnotherName);
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