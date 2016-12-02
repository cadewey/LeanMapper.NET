using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace LeanMapper.Tests.Classes
{
    public class Parent 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<Child> Children { get; set; }

        public Parent()
        {
            Children = new List<Child>();
        }

        public void AddChild(Child child)
        {
            child.Parent = this;
            Children.Add(child);
        }
    }
}
