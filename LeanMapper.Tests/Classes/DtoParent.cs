using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeanMapper.Tests.Classes
{
    public class DtoParent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<DtoChild> Children { get; set; }

        public DtoParent()
        {
            Children = new List<DtoChild>();
        }

        public void AddChild(DtoChild child)
        {
            child.Parent = this;
            Children.Add(child);
        }
    }
}