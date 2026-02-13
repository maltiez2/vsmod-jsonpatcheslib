using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool AddMerge(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "AddMerge operation json patch does not have Path specified");
            return false;
        }

        if (patch.Value == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "AddMerge operation json patch does not have Value specified");
            return false;
        }

        IEnumerable<JToken> mergeIntoTokens = patch.Path.Get(asset);


        if (patch.Value.Token is JObject valueObject)
        {
            mergeIntoTokens.OfType<JObject>().Foreach(mergeIntoToken =>
            {
                foreach ((string key, JToken? value) in valueObject)
                {
                    if (value == null)
                    {
                        continue;
                    }
                    
                    if (mergeIntoToken.ContainsKey(key))
                    {
                        mergeIntoToken[key]?.Replace(value);
                    }
                    else
                    {
                        mergeIntoToken.Add(key, value);
                    }
                }
            });

            return mergeIntoTokens.OfType<JObject>().Any();
        }

        if (patch.Value.Token is JArray valueArray)
        {
            mergeIntoTokens.OfType<JArray>().Foreach(mergeIntoToken =>
            {
                foreach (JToken value in valueArray)
                {
                    mergeIntoToken.Add(value);
                }
            });

            return mergeIntoTokens.OfType<JArray>().Any();
        }


        LoggerUtil.Error(api, typeof(Operations), "AddMerge operation json patch has value specified but it is not an array or an object");
        return false;
    }
}
