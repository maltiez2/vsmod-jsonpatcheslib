using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool AddEach(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "AddEach operation json patch does not have Path specified");
            return false;
        }

        if (patch.Value == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "AddEach operation json patch does not have Value specified");
            return false;
        }

        if (patch.Value.Token is not JArray valueArray)
        {
            LoggerUtil.Error(api, typeof(Operations), "AddEach operation json patch has value specified but it is not an array");
            return false;
        }

        IEnumerable<JToken> children = valueArray.OfType<JToken>();
        IEnumerable<JToken> parents = patch.Path.GetParent(asset, out string child);

        if (child == "-")
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                foreach (JToken childValue in children)
                {
                    parent.Add(childValue);
                }
            });
            return parentArrays.Any();
        }

        if (int.TryParse(child, out int index))
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                foreach (JToken childValue in children.Reverse())
                {
                    parent.Insert(index, childValue);
                }
            });
            return parentArrays.Any();
        }

        return false;
    }
}
