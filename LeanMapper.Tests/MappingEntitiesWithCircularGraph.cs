using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeanMapper.Tests.Classes;
using Xunit;
using static LeanMapper.Tests.Tools.ParentChildTools;

namespace LeanMapper.Tests
{
    [Collection("MainCollection")]
    public class MappingEntitiesWithCircularGraph
    {
        [Fact]
        public void ConvertParentWithChildrenNotNull()
        {
            var parent = GetParent();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.NotNull(dto);
        }

        [Fact]
        public void ConvertParentWithChildrenHasSameNumberOfChildren()
        {
            var parent = GetParent();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.Equal(parent.Children.Count, dto.Children.Count);
        }

        [Fact]
        public void ConvertParentWithNoChildrenNotNull()
        {
            var parent = GetParent();
            parent.Children.Clear();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.NotNull(dto);
        }

        [Fact]
        public void ConvertParentWithNoChildrenHasSameNumberOfChildren()
        {
            var parent = GetParent();
            parent.Children.Clear();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.Equal(parent.Children.Count, dto.Children.Count);
        }

        [Fact]
        public void ConvertParentWithChildrenEqual()
        {
            var parent = GetParent();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.True(AreEqual(parent, dto));
        }

        [Fact]
        public void ConvertParentWithNoChildrenEqual()
        {
            var parent = GetParent();
            parent.Children.Clear();
            var dto = Mapper.Map<Parent, DtoParent>(parent);
            Assert.True(AreEqual(parent, dto));
        }

    }
}
