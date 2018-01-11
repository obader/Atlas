using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.ValueObjects;

namespace Atlas.Core.Logic.Entities
{
    public class Category:Entity
    {
       public Property Value { get; private set; }

        public Category(long pId, string pCode, string pDescription)
        {
            Id = pId;
            Value = new Property(pCode,pDescription);
        }
    }
}
