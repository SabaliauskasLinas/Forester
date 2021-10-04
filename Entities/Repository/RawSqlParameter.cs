using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Repository
{
    public class RawSqlParameter
    {
        public RawSqlParameter()
        {
        }

        public RawSqlParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }
    }
}
