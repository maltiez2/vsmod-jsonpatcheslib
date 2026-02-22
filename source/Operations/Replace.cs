using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool Replace(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Replace operation json patch does not have Path specified");
            return false;
        }

        if (patch.Value == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Replace operation json patch does not have Value specified");
            return false;
        }

        IEnumerable<JToken> values = patch.Path.Get(asset, api);

        if (!values.Any())
        {
            return false;
        }

        values.Foreach(value =>
        {
            if (patch.Value.Token != null)
            {
                value.Replace(patch.Value.Token);
            }
        });

        return true;
    }
}
