﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LeanMapper.Tests
{
    public class CustomMappingInheritedProperties
    {
        [Fact]
        public void Inherited_Property_Is_Custom_Mapped_Successfully()
        {
            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName", ValueString = "42", Timestamp = "12/25/2016 5:00 PM" };

            LeanMapper.Config<SimplePocoBase, SimpleDtoBase>()
                .MapProperty(d => d.Value, p => Int32.Parse(p.ValueString));

            LeanMapper.Config<SimplePocoBase, IDto>()
                .MapProperty(d => d.Timestamp, p => DateTime.Parse(p.Timestamp));

            var dto = LeanMapper.Map<SimplePoco, SimpleDto>(poco);

            Assert.Equal(poco.Id, dto.Id);
            Assert.Equal(poco.Name, dto.Name);
            Assert.Equal(42, dto.Value);
            Assert.Equal(DateTime.Parse("12/25/2016 5:00 PM"), dto.Timestamp);
        }

        #region TestClasses

        private interface IDto
        {
            DateTime Timestamp { get; set; }
        }

        private class SimplePocoBase
        {
            public string ValueString { get; set; }
            public string Timestamp { get; set; }
        }

        private class SimpleDtoBase
        {
            public int Value { get; set; }
        }

        private class SimplePoco : SimplePocoBase
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        private class SimpleDto : SimpleDtoBase, IDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}
