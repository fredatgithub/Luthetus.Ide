using System.Collections.Immutable;
using System.Text;

namespace Luthetus.Common.RazorLib.Dimensions.Models;

public class DimensionAttribute
{
    public List<DimensionUnit> DimensionUnitBag { get; } = new();
    public DimensionAttributeKind DimensionAttributeKind { get; set; }
    public string StyleString => GetStyleString();

    private string GetStyleString()
    {
        var immutableDimensionUnits = DimensionUnitBag.ToImmutableArray();

        if (!immutableDimensionUnits.Any())
            return string.Empty;

        var styleBuilder = new StringBuilder($"{DimensionAttributeKind.ToString().ToLower()}: calc(");

        for (var index = 0; index < DimensionUnitBag.Count; index++)
        {
            var dimensionUnit = DimensionUnitBag[index];

            if (index != 0)
            {
                styleBuilder
                    .Append(' ')
                    .Append(dimensionUnit.DimensionOperatorKind.GetStyleString())
                    .Append(' ');
            }

            var dimensionUnitInvariantCulture = dimensionUnit.Value
                .ToCssValue();

            styleBuilder.Append(
                $"{dimensionUnitInvariantCulture}" +
                $"{dimensionUnit.DimensionUnitKind.GetStyleString()}");
        }

        styleBuilder.Append(");");

        return styleBuilder.ToString();
    }
}