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

        values.Foreach(value =>
        {
            value.Remove();
        });

        return true;
    }
}
