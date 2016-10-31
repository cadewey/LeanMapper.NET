using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    [TestClass]
    public class MappingEnums
    {
        [TestMethod]
        public void Int_Is_Mapped_To_Enum()
        {
            var employee = new Employee { Id = Guid.NewGuid(), Name = "Timuçin", Surname = "KIVANÇ", Department = (int)Departments.IT  };

            var dto = LeanMapper.Map<Employee, EmployeeDTO>(employee);

            Assert.IsNotNull(dto);
          
            Assert.IsTrue(dto.Id == employee.Id &&
                dto.Name == employee.Name &&
                dto.Department == Departments.IT);
        }

        [TestMethod]
        public void String_Is_Mapped_To_Enum()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT.ToString() };

            var dto = LeanMapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.IsNotNull(dto);

            Assert.AreEqual(employee.Id, dto.Id);
            Assert.AreEqual(employee.Name, dto.Name);
            Assert.AreEqual(Departments.IT, dto.Department);
        }

        [TestMethod]
        public void Null_String_Is_Mapped_To_Enum_Default()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = null };

            var dto = LeanMapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.IsNotNull(dto);

            Assert.AreEqual(employee.Id, dto.Id);
            Assert.AreEqual(employee.Name, dto.Name);
            Assert.AreEqual(Departments.Finance, dto.Department);
        }

        [TestMethod]
        public void Empty_String_Is_Mapped_To_Enum_Default()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = "" };

            var dto = LeanMapper.Map<EmployeeWithStringEnum, EmployeeDTO>(employee);

            Assert.IsNotNull(dto);

            Assert.AreEqual(employee.Id, dto.Id);
            Assert.AreEqual(employee.Name, dto.Name);
            Assert.AreEqual(Departments.Finance, dto.Department);
        }

        [TestMethod]
        public void Enum_Is_Mapped_To_String()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var poco = LeanMapper.Map<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);

            Assert.IsNotNull(poco);

            Assert.AreEqual(poco.Id, employeeDto.Id);
            Assert.AreEqual(poco.Name, employeeDto.Name);
            Assert.AreEqual(poco.Department, employeeDto.Department.ToString());
        }

        [TestMethod]
        public void MapEnumToStringSpeedTest()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                var poco = LeanMapper.Map<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);
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
