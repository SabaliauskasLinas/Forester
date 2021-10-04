using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Entities.Repository
{
    public class RawSqlCommand
    {
        public string CommandText { get; set; }

        public CommandType CommandType { get; set; } = CommandType.Text;

        public int CommandTimeout { get; set; } = 30;

        public Dictionary<string, RawSqlParameter> Parameters { get; } = new Dictionary<string, RawSqlParameter>();

        public RawSqlCommand()
        {
        }

        public RawSqlCommand(string commandText)
        {
            CommandText = commandText;
        }

        public RawSqlCommand(string commandText, CommandType commandType)
        {
            CommandText = commandText;
            CommandType = commandType;
        }

        public RawSqlCommand(string commandText, CommandType commandType, RawSqlParameter[] args)
        {
            CommandText = commandText;
            CommandType = commandType;
            if (args != null)
                AddParameters(args.ToList());
        }

        public RawSqlCommand(string commandText, params RawSqlParameter[] args)
        {
            CommandText = commandText;
            if (args != null)
                AddParameters(args.ToList());
        }

        public void AddParameter(string name, object value)
        {
            Parameters.Add(name, new RawSqlParameter { Name = name, Value = value });
        }

        public void AddParameters(IList<RawSqlParameter> args)
        {
            foreach (var p in args)
                Parameters.Add(p.Name, p);
        }
    }
}
