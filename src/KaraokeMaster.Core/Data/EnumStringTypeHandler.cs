using System.Data;
using Dapper;

namespace KaraokeMaster.Core.Data;

/// <summary>
/// Stores enum columns as their name (e.g. "Ready") instead of Dapper's default underlying-int
/// parameter behavior, so the SQLite columns stay human-readable in a DB browser.
/// </summary>
internal sealed class EnumStringTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, TEnum value) => parameter.Value = value.ToString();

    public override TEnum Parse(object value) => Enum.Parse<TEnum>((string)value);
}
