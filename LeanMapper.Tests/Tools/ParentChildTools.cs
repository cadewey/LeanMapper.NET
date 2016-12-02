using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeanMapper.Tests.Classes;
using Xunit;

namespace LeanMapper.Tests.Tools
{
    public static class ParentChildTools
    {
        public static Parent GetParent(int id = 0)
        {
            var parent = new Parent()
            {
                Id = id + 2,
                Name = "Parent0"
            };

            var child1 = new Child()
            {
                Id = id + 0,
                Name = $"Child{id + 0}"
            };

            var child2 = new Child()
            {
                Id = id + 1,
                Name = $"Child{id + 1}"
            };

            parent.AddChild(child1);
            parent.AddChild(child2);

            return parent;
        }

        public static int GetDepth(Parent parent)
        {
            return parent.Children.Any() ? parent.Children.Max(GetDepth) + 1 : 0;
        }

        public static  int GetDepth(Child child)
        {
            var result = child.Parent != null ? GetDepth(child.Parent) + 1 : 0;
            return result;
        }

        public static int GetDepth(DtoParent parent)
        {
            return parent.Children.Any() ? parent.Children.Max(GetDepth) + 1 : 0;
        }

        public static int GetDepth(DtoChild child)
        {
            var result = child.Parent != null ? GetDepth(child.Parent) + 1 : 0;
            return result;
        }
        public static Parent CreateGraph(int depth)
        {
            var newParent = GetParent();
            CreateDepth(newParent, depth);
            return newParent;
        }

        public static void CreateDepth(Parent parent, int depth)
        {
            if (depth == 0)
            {
                parent.Children.Clear();
            }
            else if (depth == 1)
            {
                parent.Children[0].Parent = null;
                parent.Children[1].Parent = null;
            }
            else if (depth > 1)
            {
                var newParent = GetParent(parent.Id + 1);
                CreateDepth(newParent, depth - 2);
                parent.Children[0].Parent = newParent;
                parent.Children[1].Parent = null;
            }

        }

        public static bool AreEqual(Parent parent, DtoParent dtoParent)
        {
            return dtoParent.Id == parent.Id && dtoParent.Name == parent.Name && parent.Children.Zip(dtoParent.Children, AreEqual).All(x => x);
        }

        public static bool AreEqual(Child child, DtoChild dtoChild)
        {
            return dtoChild.Id == child.Id && dtoChild.Name == child.Name;

        }


        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(10)]
        public static void CreateDepthMatchesGetDepth(int depth)
        {
            var parent0 = CreateGraph(depth);
            var expectedResult = depth;
            var actualResult = GetDepth(parent0);
            Assert.Equal(actualResult, expectedResult);
        }

    }
}
