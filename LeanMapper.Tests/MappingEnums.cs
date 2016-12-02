using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace LeanMapper.Tests
{
    #region Test Objects

    public enum Departments
    {
        Finance = 0,
        IT = 1,
        Sales = 2
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public int Department { get; set; }
    }

    public class EmployeeWithStringEnum
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
    }

    public class EmployeeDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Departments Department { get; set; }
    }

    #endregion

    [Collection("MainCollection")]
    public class MappingEnums
    {
        [Fact]
        public void Int_Is_Mapped_To_Enum()
        {
            var employee = new Employee { Id = Guid.NewGuid(), Name = "Timuçin", Surname = "KIVANÇ", Department = (int)Departments.IT  };

            var dto = Mapper.Map<Employee, EmployeeDTO>(employee);

            Assert.NotNull(dto);
          
            Assert.True(dto.Id == employee.Id &&
                dto.Name == employee.Name &&
                dto.Department == Departments.IT);
        }

        [Fact]
        public void String_Is_Mapped_To_Enum()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT.ToString() };

            var dto = Mapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.NotNull(dto);

            Assert.Equal(employee.Id, dto.Id);
            Assert.Equal(employee.Name, dto.Name);
            Assert.Equal(Departments.IT, dto.Department);
        }

        [Fact]
        public void Null_String_Is_Mapped_To_Enum_Default()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = null };

            var dto = Mapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.NotNull(dto);

            Assert.Equal(employee.Id, dto.Id);
            Assert.Equal(employee.Name, dto.Name);
            Assert.Equal(Departments.Finance, dto.Department);
        }

        [Fact]
        public void Empty_String_Is_Mapped_To_Enum_Default()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = "" };

            var dto = Mapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.NotNull(dto);

            Assert.Equal(employee.Id, dto.Id);
            Assert.Equal(employee.Name, dto.Name);
            Assert.Equal(Departments.Finance, dto.Department);
        }

        [Fact]
        public void Enum_Is_Mapped_To_String()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var poco = Mapper.Map<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);

            Assert.NotNull(poco);

            Assert.Equal(poco.Id, employeeDto.Id);
            Assert.Equal(poco.Name, employeeDto.Name);
            Assert.Equal(poco.Department, employeeDto.Department.ToString());
        }

        [Fact]
        public void MapEnumToStringSpeedTest()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                var poco = Mapper.Map<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);
            }
            timer.Stop();
            Console.WriteLine("Enum to string Elapsed time ms: " + timer.ElapsedMilliseconds);
        }

        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Flags
        {
            Zero = 0,
            Two = 2,
            Four = 4,
            Six = 6,
            Eight = 8,
        }
    }
}
