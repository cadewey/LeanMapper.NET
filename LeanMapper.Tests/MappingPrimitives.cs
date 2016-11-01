using System;
using System.Text;
using Xunit;

namespace LeanMapper.Tests
{
    public class MappingPrimitives
    {
        [Fact]
        public void Byte_Array_In_Test_Class_Is_Mapped_Correctly()
        {
            string testString = "this is a string that will later be converted to a byte array and other text blah blah blah I'm not sure what else to put here...";

            var testA = new TestA { Bytes = Encoding.ASCII.GetBytes(testString) };
            var testB = LeanMapper.Map<TestA, TestB>(testA);
            var resultString = Encoding.ASCII.GetString(testB.Bytes);

            Assert.Equal(testString, resultString);
        }

        [Fact]
        public void ValueType_String_Object_Is_Always_Primitive()
        {
            var sourceDto = new PrimitivePoco
            {
                Id = "test",
                Time = TimeSpan.FromHours(7),
            };
            var targetDto = LeanMapper.Map<PrimitivePoco, PrimitivePoco>(sourceDto);

            Assert.Equal(sourceDto.Id, targetDto.Id);
            Assert.Equal(sourceDto.Time, targetDto.Time);
        }

        #region TestClasses

        public class ImmutableA
        {
            public ImmutableA(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        public class ImmutableB
        {
            public ImmutableB(string name)
            {
                this.NameX = name;
            }

            public string NameX { get; }
        }

        public class TestA
        {
            public Byte[] Bytes { get; set; }
        }

        public class TestB
        {
            public Byte[] Bytes { get; set; }
        }

        public class PrimitivePoco
        {
            public string Id { get; set; }
            public TimeSpan Time { get; set; }
            public object Obj { get; set; }
        }

        #endregion
    }
}