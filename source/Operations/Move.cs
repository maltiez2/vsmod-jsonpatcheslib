using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool Move(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Move operation json patch does not have Path specified");
            return false;
        }

        if (patch.FromPath == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Move operation json patch does not have FromPath specified");
            return false;
        }

        IEnumerable<JToken> valuesToCopy = patch.FromPath.Get(asset, api);

        if (valuesToCopy.Count() != 1)
        {
            LoggerUtil.Error(api, typeof(Operations), "Move operation json patch has FromPath specified but it returns multiple paths (or no paths). It should return only one.");
            return false;
        }

        JToken valueToCopy = valuesToCopy.First();

        valueToCopy.Remove();

        IEnumerable<JToken> parents = patch.Path.GetParent(asset, out string child, api);

        if (child == "-")
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                parent.Add(valueToCopy);
            });
            return parentArrays.Any();
        }

        if (int.TryParse(child, out int index))
        {
            IEnumerable<JArray> parentArrays = parents.OfType<JArray>();
            parentArrays.Foreach(parent =>
            {
                parent.Insert(index, valueToCopy);
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
            value.Add(child, valueToCopy);
        });

        return true;
    }
}
