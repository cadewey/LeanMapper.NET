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
    public class LimitingDepthOfDeepGraphs
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 2)]
        [InlineData(0, 10)]
        [InlineData(1, 10)]
        [InlineData(2, 10)]
        [InlineData(10, 10)]
        [InlineData(11, 10)]
        [InlineData(12, 10)]
        [InlineData(13, 10)]
        public void GraphLimitedByMaxDepth(int maxDepth, int graphDepth)
        {
            Mapper.Reset();
            Mapper.Config<Parent, DtoParent>()
                .SetDepth(maxDepth);
            var parent0 = GetParent(0);
            var dto = Mapper.Map<Parent, DtoParent>(parent0);
            var actualResult = GetDepth(dto);
            Assert.Equal(maxDepth, actualResult);
        }
    }
}
