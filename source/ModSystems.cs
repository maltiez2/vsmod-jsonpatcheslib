using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public sealed class JsonPatchLibSystem : ModSystem
{
    public override double ExecuteOrder() => 0.049;

    public override void StartPre(ICoreAPI api)
    {
        _ = new AssetCategory(_patchesDirectory, AffectsGameplay: true, EnumAppSide.Universal);
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        Dictionary<string, (JToken token, IAsset asset)> jsonCache = [];

        ApplyPatches(api, jsonCache);
    }



    private const string _patchesDirectory = "jsonpatches";

    private static void ApplyPatches(ICoreAPI api, Dictionary<string, (JToken token, IAsset asset)> jsonCache)
    {
        List<IAsset> patchAssets = api.Assets.GetMany(_patchesDirectory);

        HashSet<string> loadedModIds = api.ModLoader.Mods.Select(mod => mod.Info.ModID).ToHashSet();

        List<JsonPatchPack> patches = GetPatchesPacks(api, patchAssets);

        List<(float priority, JsonPatchPack patches)> orderedPatches = OrderPatches(patches);

        List<string> affectedAssets = [];

        foreach ((float priority, JsonPatchPack pack) in orderedPatches)
        {
            if (affectedAssets.Contains(pack.AssetPath) && jsonCache.ContainsKey(pack.AssetPath))
            {
                pack.Patches = LoadPatchesFromToken(api, jsonCache[pack.AssetPath].token, pack.AssetPath);
            }

            foreach (JsonPatch patch in pack.Patches.Where(element => element.Priority == priority && CheckIfPatchActive(api, element, loadedModIds)))
            {
                ApplyPatch(api, patch, affectedAssets, jsonCache);
            }
        }

        SaveAffectedAssets(api, affectedAssets, jsonCache);
    }

    private static List<JsonPatchPack> GetPatchesPacks(ICoreAPI api, List<IAsset> assets)
    {
        List<JsonPatchPack> result = [];

        foreach (IAsset asset in assets)
        {
            JsonPatch[] patches;
            try
            {
                patches = asset.ToObject<JsonPatch[]>();
            }
            catch (Exception exception)
            {
                LoggerUtil.Error(api, typeof(JsonPatchLibSystem), $"Error on loading patches from file {asset.Location}:\n{exception}");
                continue;
            }

            result.Add(new JsonPatchPack()
            {
                Patches = patches.ToArray(),
                AssetPath = asset.Location
            });
        }

        return result;
    }

    private static JsonPatch[] LoadPatchesFromToken(ICoreAPI api, JToken token, string path)
    {
        try
        {
            return token.ToObject<JsonPatch[]>() ?? [];
        }
        catch (Exception exception)
        {
            LoggerUtil.Error(api, typeof(JsonPatchLibSystem), $"Error on loading patches from file {path}:\n{exception}");
            return [];
        }
    }

    private static List<(float priority, JsonPatchPack patches)> OrderPatches(List<JsonPatchPack> packs)
    {
        List<float> priorities = packs
            .SelectMany(pack => pack.Patches.Select(patch => patch.Priority))
            .Distinct()
            .OrderByDescending(element => element)
            .ToList();

        List<(float priority, JsonPatchPack patches)> result = [];
        foreach (float priority in priorities)
        {
            packs
                .Where(pack => pack.Patches.Contains(patch => patch.Priority == priority))
                .Foreach(pack => result.Add((priority, pack)));
        }

        return result;
    }

    private static bool CheckIfPatchActive(ICoreAPI api, JsonPatch patch, HashSet<string> loadedMods)
    {
        if (!patch.Enabled)
        {
            return false;
        }

        if (patch.Side != EnumAppSide.Universal && patch.Side != api.Side)
        {
            return false;
        }

        if (patch.DependsOn != null)
        {
            bool enabled = true;

            foreach (JsonPatchModDependence dependence in patch.DependsOn)
            {
                if (dependence.ModId == null)
                {
                    throw new InvalidOperationException("One of the dependencies in 'DependsOn' does not have mod id specified");
                }

                bool loaded = loadedMods.Contains(dependence.ModId);
                enabled = enabled && (loaded ^ dependence.Invert);
            }

            if (!enabled)
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyPatch(ICoreAPI api, JsonPatch patch, List<string> affectedAssets, Dictionary<string, (JToken token, IAsset asset)> jsonCache)
    {
        if (patch.File == null)
        {
            LoggerUtil.Error(api, typeof(JsonPatchLibSystem), $"Error while applying patch: file is not specified");
            return;
        }

        string[] files = ResolveFilePath(api, patch.File);

        if (files.Length == 0)
        {
            LoggerUtil.Warn(api, typeof(JsonPatchLibSystem), $"Found no files by path: {patch.File}");
        }

        foreach (string file in files)
        {
            string filePath = file;
            if (!filePath.EndsWith(".json"))
            {
                filePath = filePath + ".json";
            }

            if (!jsonCache.TryGetValue(filePath, out (JToken token, IAsset asset) cachedAsset))
            {
                IAsset asset = api.Assets.TryGet(filePath);
                string jsonText = asset.ToText();
                JToken token = JToken.Parse(jsonText);
                jsonCache.Add(filePath, (token, asset));
                cachedAsset = (token, asset);
            }

            bool applied = ApplyPatchToAsset(api, patch, cachedAsset.token);

            if (applied)
            {
                affectedAssets.Add(filePath);
            }
            else
            {
                LoggerUtil.Warn(api, typeof(JsonPatchLibSystem), $"Failed to apply patch for '{patch.Path?.OriginalPath()}' in '{file}'");
            }
        }
    }

    private static bool ApplyPatchToAsset(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        return patch.Op switch
        {
            JsonPatchOperationType.Add => Operations.Add(api, patch, asset),
            JsonPatchOperationType.AddEach => Operations.AddEach(api, patch, asset),
            JsonPatchOperationType.Remove => Operations.Remove(api, patch, asset),
            JsonPatchOperationType.Replace => Operations.Replace(api, patch, asset),
            JsonPatchOperationType.Copy => Operations.Copy(api, patch, asset),
            JsonPatchOperationType.Move => Operations.Move(api, patch, asset),
            JsonPatchOperationType.AddMerge => Operations.AddMerge(api, patch, asset),
            _ => false
        };
    }

    private static void SaveAffectedAssets(ICoreAPI api, List<string> affectedAssets, Dictionary<string, (JToken token, IAsset asset)> jsonCache)
    {
        StringBuilder fileContent = new(4096);

        foreach (AssetLocation fileLoc in affectedAssets)
        {
            if (!jsonCache.TryGetValue(fileLoc, out (JToken token, IAsset asset) cachedData))
            {
                continue;
            }

            try
            {
                fileContent.Length = 0;
                using (StringWriter writer = new(fileContent))
                using (JsonTextWriter jsonWriter = new(writer))
                {
                    cachedData.token.WriteTo(jsonWriter);
                }

                //Debug.WriteLine($"\n\n\n{fileLoc}\n\n{fileContent}");

                cachedData.asset.Data = Encoding.UTF8.GetBytes(fileContent.ToString());
                cachedData.asset.IsPatched = true;
            }
            catch (Exception exception)
            {
                LoggerUtil.Error(api, typeof(JsonPatchLibSystem), $"Failed to serialize JSON file {fileLoc}:\n{exception}");
            }
        }
    }

    private static string[] ResolveFilePath(ICoreAPI api, string path)
    {
        if (path.StartsWith('@'))
        {
            string wildcard = path[1..];

            return api.Assets.GetLocations("")
                .Where(assetPath => WildcardUtil.Match(wildcard, assetPath.ToString()[..^5]))
                .Select(assetPath => assetPath.ToString())
                .ToArray();
        }
        else
        {
            return [path];
        }
    }
}