using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Entities.Repository
{
    public class EntityConversionSettings
    {
        public string PropertyName { get; set; }
        public Type PropertyType { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }
}
