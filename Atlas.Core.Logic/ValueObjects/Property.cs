using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.ValueObjects
{
    public class Property:ValueObject<Property>
    {
        public string Code { get; private set; }
        public string Description { get; private set; }

        public Property(string pCode, string pDescription)
        {           
            Code = pCode;
            Description = pDescription;
        }
    }
}
