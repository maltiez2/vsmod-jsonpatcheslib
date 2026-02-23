using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

[JsonConverter(typeof(JsonObjectPathConverter))]
public sealed class JsonObjectPath
{
    public JsonObjectPath(string path)
    {
        _tokens = path.Split("/").Where(element => element != "").ToList();
        _path = _tokens.Select(Convert).ToList();
        _originalPath = path;
    }

    public List<JToken> Get(JToken tree, ICoreAPI api)
    {
        List<JToken> result = [tree];
        for (int pathElementIndex = 0; pathElementIndex < _path.Count; pathElementIndex++)
        {
            result = _path[pathElementIndex].Invoke(result);
            if (result.Count == 0)
            {
                LoggerUtil.Warn(api, this, $"Was not able to traverse path '{_originalPath}', failed at token '{_tokens[pathElementIndex]}'.");
                return result;
            }
        }
        return result;
    }
    public int Set(JToken tree, JToken value, ICoreAPI api)
    {
        List<JToken> result = Get(tree, api);

        foreach (JToken element in result)
        {
            element.Replace(value);
        }

        return result.Count;
    }
    public List<JToken> GetParent(JToken tree, out string child, ICoreAPI api)
    {
        List<JToken> result = [tree];
        for (int elementIndex = 0; elementIndex < _path.Count - 1; elementIndex++)
        {
            result = _path[elementIndex].Invoke(result);
            if (result.Count == 0)
            {
                LoggerUtil.Warn(api, this, $"Was not able to traverse path '{_originalPath}', failed at token '{_tokens[elementIndex]}'.");
                child = _tokens[elementIndex + 1];
                return result;
            }
        }
        child = _tokens[^1];
        return result;
    }
    public string OriginalPath() => _originalPath;



    private delegate List<JToken> PathElementDelegate(List<JToken> attribute);
    private readonly List<PathElementDelegate> _path;
    private readonly List<string> _tokens;
    private readonly string _originalPath;

    private PathElementDelegate Convert(string element)
    {
        if (int.TryParse(element, out int index))
        {
            return tree => PathElementByIndex(tree, index);
        }
        else
        {
            if (element == "-") return PathElementByAllIndexes;

            PathElementDelegate? rangeResult = TryParseRange(element);
            if (rangeResult != null) return rangeResult;

            PathElementDelegate? wildcardResult = TryParseWildcard(element);
            if (wildcardResult != null) return wildcardResult;

            PathElementDelegate? conditionResult = TryParseCondition(element);
            if (conditionResult != null) return conditionResult;

            return tree => PathElementByKey(tree, element);
        }
    }

    private static List<JToken> PathElementByAllIndexes(List<JToken> attributes)
    {
        List<JToken> result = [];
        foreach (JArray attributesArray in attributes.OfType<JArray>())
        {
            int size = attributesArray.Count;
            for (int i = 0; i < size; i++)
            {
                result.Add(attributesArray[i]);
            }
        }

        return result;
    }
    private static List<JToken> PathElementByIndexes(List<JToken> attributes, int start, int end)
    {
        List<JToken> result = [];
        foreach (JArray attributesArray in attributes.OfType<JArray>())
        {
            int size = attributesArray.Count;
            for (int i = Math.Max(0, start); i < Math.Min(end, size); i++)
            {
                result.Add(attributesArray[i]);
            }
        }

        return result;
    }
    private static List<JToken> PathElementByIndex(List<JToken> attributes, int index)
    {
        List<JToken> result = new();

        foreach (JArray attribute in attributes.OfType<JArray>())
        {
            if (index < 0 || attribute.Count <= index)
            {
                continue;
            }

            result.Add(attribute[index]);
        }

        return result;
    }
    private static List<JToken> PathElementByKey(List<JToken> attributes, string key)
    {
        List<JToken> result = [];

        foreach (JObject attribute in attributes.OfType<JObject>().Where(attribute => attribute.ContainsKey(key)))
        {
            JToken? value = attribute[key];
            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }
    private static List<JToken> PathElementByWildcard(List<JToken> attributes, string wildcard)
    {
        List<JToken> result = [];

        foreach (JObject token in attributes.OfType<JObject>())
        {
            foreach ((string key, JToken? value) in token)
            {
                if (WildcardUtil.Match(wildcard, key) && value != null)
                {
                    result.Add(value);
                }
            }
        }

        return result;
    }
    private static List<JToken> PathElementByCondition(List<JToken> attributes, string code, string condition)
    {
        IEnumerable<JArray> arrays = attributes
            .OfType<JArray>();

        IEnumerable<JObject> objects = attributes
            .OfType<JObject>();

        IEnumerable<JToken> tokens = [];
        if (arrays.Any())
        {
            tokens = tokens.Concat(arrays.Select(a => a as IEnumerable<JToken>).Aggregate((a, b) => a.Concat(b)));
        }
        if (objects.Any())
        {
            tokens = tokens.Concat(objects.Select(a => a as IEnumerable<JToken>).Aggregate((a, b) => a.Concat(b)));
        }

        IEnumerable<JToken> fromObjects = tokens
            .OfType<JObject>()
            .Where(a => a.ContainsKey(code) && a[code]?.Value<string>() == condition);

        IEnumerable<JToken> fromProperties = tokens
            .OfType<JProperty>()
            .Where(a => a.Name == code && a.Value.Value<string>() == condition);

        return fromObjects.Concat(fromProperties).ToList();
    }

    private static PathElementDelegate? TryParseRange(string element)
    {
        if (!element.Contains('-')) return null;

        string[] indexes = element.Split("-");
        if (indexes.Length != 2) return null;

        bool parsedStart = int.TryParse(indexes[0], out int start);
        bool parsedEnd = int.TryParse(indexes[1], out int end);

        if (!parsedStart || !parsedEnd) return null;

        return tree => PathElementByIndexes(tree, start, end);
    }
    private static PathElementDelegate? TryParseWildcard(string element)
    {
        if (!element.StartsWith("@@")) return null;
        string wildcard = element[2..];

        return tree => PathElementByWildcard(tree, wildcard);
    }
    private static PathElementDelegate? TryParseCondition(string element)
    {
        if (!element.Contains('=')) return null;

        string[] parts = element.Split('=');

        if (parts.Length != 2) return null;

        string code = parts[0];
        string condition = parts[1];

        return tree => PathElementByCondition(tree, code, condition);
    }

    private sealed class JsonObjectPathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(JToken);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            string? value = (string?)(JToken.ReadFrom(reader) as JValue)?.Value;
            if (value == null)
            {
                return existingValue;
            }
            return new JsonObjectPath(value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            new JValue((value as JsonObjectPath)?.OriginalPath()).WriteTo(writer);
        }
    }
}
