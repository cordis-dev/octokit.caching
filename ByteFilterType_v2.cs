using System;

namespace GridMvc.Filtering.Types
{
    internal sealed class ByteFilterType_v2 : FilterTypeBase
    {
        public override Type TargetType
        {
            get { return typeof (Byte); }
        }

        public override GridFilterType GetValidType(GridFilterType type)
        {
            switch (type)
            {
                case GridFilterType.Equals:
                case GridFilterType.GreaterThan:
                case GridFilterType.GreaterThanOrEquals:
                case GridFilterType.LessThan:
                case GridFilterType.LessThanOrEquals:
                    return type;
                default:
                    return GridFilterType.Equals;
            }
        }

        public override object GetTypedValue(string value)
        {
            byte bt;
            if (!byte.TryParse(value, out bt))
                return null;
            return bt;
        }
    }
}
