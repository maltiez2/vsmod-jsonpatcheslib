using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool Remove(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Remove operation json patch does not have Path specified");
            return false;
        }

        IEnumerable<JToken> values = patch.Path.Get(asset);

        if (!values.Any())
        {
            return false;
        }

        if (patch.Value?.Token is JValue token)
        {
            values
                .OfType<JValue>()
                .Where(value => Match(token, value))
                .Foreach(value =>
                {
                    value.Remove();
                });
        }
        else
        {
            values.Foreach(value =>
            {
                if (value.Parent is JProperty property)
                {
                    property.Remove();
                }
                else
                {
                    value.Remove();
                }
            });
        }

        return true;
    }

    private static bool Match(JValue needle, JValue haystack)
    {
        string? wildcard = needle.Value<string>();
        if (wildcard == null || !wildcard.StartsWith("@@"))
        {
            return needle.Equals(haystack);
        }

        string? value = haystack.Value<string>();
        if (value == null)
        {
            return false;
        }

        return WildcardUtil.Match(wildcard[2..], value);
    }
}
