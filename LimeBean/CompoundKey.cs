using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    class CompoundKey : IConvertible {
        IDictionary<string, IConvertible> _components = new Dictionary<string, IConvertible>();

        public IConvertible this[string component] {
            get { return _components.ContainsKey(component) ? _components[component] : null; }
            set {
                if(value == null)
                    throw new ArgumentNullException(component);

                _components[component] = value; 
            }
        }

        public override string ToString() {            
            return String.Join(", ", _components.OrderBy(e => e.Key).Select(c => c.Key + "=" + Convert.ToString(c.Value, CultureInfo.InvariantCulture)));
        }

        TypeCode IConvertible.GetTypeCode() {
            return TypeCode.Object;
        }

        string IConvertible.ToString(IFormatProvider provider) {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider) {
            return Convert.ChangeType(this, conversionType, provider);
        }

        #region IConvertible fake-up

        bool IConvertible.ToBoolean(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        short IConvertible.ToInt16(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        int IConvertible.ToInt32(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        long IConvertible.ToInt64(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            throw new InvalidCastException();
        }

        #endregion
    }

}