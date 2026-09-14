using System.Data;
using System.Globalization;
using Dapper;

namespace KaraokeMaster.Core.Data;

/// <summary>
/// Dapper can write a DateTimeOffset parameter into a TEXT column fine, but its default
/// deserializer can't convert TEXT back into DateTimeOffset on read — this handler does both.
/// </summary>
internal sealed class DateTimeOffsetStringTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) => parameter.Value = value.ToString("O");

    public override DateTimeOffset Parse(object value) =>
        DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
