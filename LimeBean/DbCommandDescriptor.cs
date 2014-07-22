using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    struct DbCommandDescriptor : IEquatable<DbCommandDescriptor> {
        string _sql;
        string[] _paramNames;
        object[] _paramValues;
        int _tag;

        public DbCommandDescriptor(string sql, params object[] parameters)
            : this(0, sql, parameters) {
        }

        public DbCommandDescriptor(int tag, string sql, params object[] parameters) {
            _tag = tag;
            _sql = sql;

            if(parameters == null) {
                _paramNames = null;
                _paramValues = new object[] { null };
            } else {
                var named = ExtractNamedParameters(parameters);
                if(named != null) {
                    _paramNames = named.Keys
                        .Cast<object>()
                        .Select(key => Convert.ToString(key, CultureInfo.InvariantCulture))
                        .OrderBy(key => key)
                        .ToArray();

                    _paramValues = _paramNames.Select(key => named[key]).ToArray();
                } else {
                    _paramNames = null;
                    _paramValues = new object[parameters.Length];
                    Array.Copy(parameters, _paramValues, parameters.Length);
                }
            }
        }

        static IDictionary ExtractNamedParameters(object[] parameters) {
            if(parameters.Length != 1)
                return null;

            var first = parameters.First();
            if(first == null || first is DBNull || first is ValueType || first is String)
                return null;

            var dict = first as IDictionary;
            if(dict != null)
                return dict;

            return TypeDescriptor.GetProperties(first).Cast<PropertyDescriptor>().ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(first)
            );
        }

        public IDbCommand ToCommand(IDbConnection connection) {
            var cmd = connection.CreateCommand();
            cmd.CommandText = _sql;

            for(var i = 0; i < _paramValues.Length; i++) {
                var p = cmd.CreateParameter();
                p.Value = _paramValues[i];

                if(_paramNames != null)
                    p.ParameterName = _paramNames[i];

                cmd.Parameters.Add(p);
            }

            return cmd;
        }

        public bool Equals(DbCommandDescriptor other) {
            return _tag == other._tag
                && _sql == other._sql
                && ArraysEqual(_paramNames, other._paramNames)
                && ArraysEqual(_paramValues, other._paramValues);
        }
        

        public override bool Equals(object obj) {
            return obj is DbCommandDescriptor && Equals((DbCommandDescriptor)obj);
        }

        public override int GetHashCode() {
            var hash = CombineHashCodes(_tag, _sql.GetHashCode());

            if(_paramNames != null) {
                foreach(var name in _paramNames)
                    hash = CombineHashCodes(hash, EqualityComparer<string>.Default.GetHashCode(name));
            } else {
                hash = CombineHashCodes(hash, 0);
            }

            foreach(var value in _paramValues)
                hash = CombineHashCodes(hash, EqualityComparer<object>.Default.GetHashCode(value));

            return hash;
        }

        static int CombineHashCodes(int h1, int h2) {
            // from System.Web.Util.HashCodeCombiner
            return (h1 << 5) + h1 ^ h2;
        }

        static bool ArraysEqual<T>(T[] x, T[] y) {
            if(ReferenceEquals(x, y))
                return true;

            if(ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return x.SequenceEqual(y);
        }

    }

}
