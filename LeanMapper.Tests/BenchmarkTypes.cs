using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LeanMapper.Tests
{
    public class BenchmarkTypes
    {
        #region Object creation

        private static Customer GetCustomer()
        {
            Customer c = new Customer()
            {
                Address = new Address() { City = "istanbul", Country = "turkey", Id = 1, Street = "istiklal cad." },
                HomeAddress = new Address() { City = "istanbul", Country = "turkey", Id = 2, Street = "istiklal cad." },
                Id = 1,
                Name = "Eduardo Najera",
                Credit = 234.7m,
                WorkAddresses = new List<Address>()
                {
                    new Address() {City = "istanbul", Country = "turkey", Id = 5, Street = "istiklal cad."},
                    new Address() {City = "izmir", Country = "turkey", Id = 6, Street = "konak"}
                },
                Addresses = new List<Address>()
                {
                    new Address() {City = "istanbul", Country = "turkey", Id = 3, Street = "istiklal cad."},
                    new Address() {City = "izmir", Country = "turkey", Id = 4, Street = "konak"}
                }.ToArray()
            };

            return c;
        }

        private static Foo GetFoo()
        {
            var o = new Foo
            {
                Name = "foo",
                Int32 = 12,
                Int64 = 123123,
                NullInt = 16,
                DateTime = DateTime.Now,
                Doublen = 2312112,
                Foo1 = new Foo { Name = "foo one" },
                Foos = new List<Foo>
                {
                    new Foo {Name = "j1", Int64 = 123, NullInt = 321},
                    new Foo {Name = "j2", Int32 = 12345, NullInt = 54321},
                    new Foo {Name = "j3", Int32 = 12345, NullInt = 54321},
                },
                FooArr = new[]
                {
                    new Foo {Name = "a1"},
                    new Foo {Name = "a2"},
                    new Foo {Name = "a3"},
                },
                IntArr = new[] { 1, 2, 3, 4, 5 },
                Ints = new[] { 7, 8, 9 },
            };

            return o;
        }

        #endregion

        [Fact]
        public void Map_Simple_Type()
        {
            var foo = GetFoo();
            var dstFoo = Mapper.Map<Foo, Foo>(foo);

            Assert.Equal(foo.Name, dstFoo.Name);
            Assert.Equal(foo.DateTime, dstFoo.DateTime);
            Assert.Equal(foo.Doublen, dstFoo.Doublen);
            Assert.Equal(foo.Floatn, dstFoo.Floatn);
            Assert.Equal(foo.Int32, dstFoo.Int32);
            Assert.Equal(foo.Int64, dstFoo.Int64);
            Assert.Equal(foo.NullInt, dstFoo.NullInt);
            Assert.Equal(foo.Foo1, dstFoo.Foo1);
            Assert.Equal(foo.FooArr, dstFoo.FooArr);
            Assert.Equal(foo.IntArr, dstFoo.IntArr);
            Assert.Equal(foo.Ints.ToArray(), dstFoo.Ints.ToArray());
            Assert.NotSame(foo, dstFoo);
        }

        [Fact]
        public void Map_Complex_Type_With_Null_Collections()
        {
            Mapper.Map<Customer, CustomerDTO>(new Customer());
            Mapper.Map<CustomerDTO, Customer>(new CustomerDTO());
            Mapper.Map<Address, AddressDTO>(new Address());
            Mapper.Map<AddressDTO, Address>(new AddressDTO());
        }

        [Fact]
        public void Map_Complex_Type()
        {
            var customer = GetCustomer();
            var dstCustomer = Mapper.Map<Customer, CustomerDTO>(customer);

            Assert.Equal(customer.Id, dstCustomer.Id);
            Assert.Equal(customer.Name, dstCustomer.Name);
            Assert.Equal(customer.Address, dstCustomer.Address);
            Assert.True(customer.HomeAddress.Equals(dstCustomer.HomeAddress));

            Assert.Null(dstCustomer.AddressCity);
            Assert.NotSame(customer.Address, dstCustomer.Address);
            Assert.NotSame(customer.HomeAddress, dstCustomer.HomeAddress);

            for (int i = 0; i < customer.Addresses.Length; ++i)
            {
                Assert.True(customer.Addresses[i].Equals(dstCustomer.Addresses[i]));
            }

            var workAddressesList = customer.WorkAddresses.ToList();
            for (int i = 0; i < workAddressesList.Count(); ++i)
            {
                Assert.True(workAddressesList[i].Equals(dstCustomer.WorkAddresses[i]));
            }
        }
    }

    #region TestClasses

    public class Foo
    {
        public string Name { get; set; }

        public int Int32 { get; set; }

        public long Int64 { set; get; }

        public int? NullInt { get; set; }

        public float Floatn { get; set; }

        public double Doublen { get; set; }

        public DateTime DateTime { get; set; }

        public Foo Foo1 { get; set; }

        public IEnumerable<Foo> Foos { get; set; }

        public Foo[] FooArr { get; set; }

        public int[] IntArr { get; set; }

        public IEnumerable<int> Ints { get; set; }

    }

    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        // TODO: Implement this in a less awful way
        public override bool Equals(object obj)
        {
            var aObj = obj as Address;
            {
                if (aObj != null)
                {
                    return Id == aObj.Id &&
                           City == aObj.City &&
                           Country == aObj.Country &&
                           Street == aObj.Street;
                }

                var dtoObj = obj as AddressDTO;

                if (dtoObj != null)
                {

                    return Id == dtoObj.Id &&
                           City == dtoObj.City &&
                           Country == dtoObj.Country;
                }
            }

            return false;
        }
    }

    public class AddressDTO
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        // TODO: Implement this in a less awful way
        public override bool Equals(object obj)
        {
            var aObj = obj as Address;
            {
                if (aObj != null)
                {
                    return Id == aObj.Id &&
                           City == aObj.City &&
                           Country == aObj.Country;
                }

                var dtoObj = obj as AddressDTO;

                if (dtoObj != null)
                {

                    return Id == dtoObj.Id &&
                           City == dtoObj.City &&
                           Country == dtoObj.Country;
                }
            }

            return false;
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Credit { get; set; }
        public Address Address { get; set; }
        public Address HomeAddress { get; set; }
        public Address[] Addresses { get; set; }
        public ICollection<Address> WorkAddresses { get; set; }
    }

    public class CustomerDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public AddressDTO HomeAddress { get; set; }
        public AddressDTO[] Addresses { get; set; }
        public List<AddressDTO> WorkAddresses { get; set; }
        public string AddressCity { get; set; }
    }

    #endregion
}
