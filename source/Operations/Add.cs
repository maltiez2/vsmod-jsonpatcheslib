using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool Add(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Add operation json patch does not have Path specified");
            return false;
        }

        if (patch.Value == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Add operation json patch does not have Value specified");
            return false;
        }

        IEnumerable<JToken> parents = patch.Path.GetParent(asset, out string child);

        if (child == "-")
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                parent.Add(patch.Value.Token);
            });
            return parentArrays.Any();
        }

        if (int.TryParse(child, out int index))
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                parent.Insert(index, patch.Value.Token);
            });
            return parentArrays.Any();
        }

        IEnumerable<JObject> validParents = parents.OfType<JObject>().Where(parent => !parent.ContainsKey(child));

        if (!validParents.Any())
        {
            return false;
        }

        validParents.Foreach(value =>
        {
            value.Add(child, patch.Value.Token);
        });

        return true;
    }
}
