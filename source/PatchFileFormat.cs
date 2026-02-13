using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace JsonPatchLib;

/// <summary>
/// A set of operations that define what a patch will do.
/// See https://datatracker.ietf.org/doc/html/rfc6902#section-4.1 for more information on each operation type.
/// </summary>
public enum JsonPatchOperationType
{
    /// <summary>
    /// Add an element to a json property at a specific path. Please consider using <see cref="AddMerge"/> for improved mod compatability.
    /// </summary>
    Add,

    /// <summary>
    /// Add a set of objects to an array. Will not work if used on other data types.
    /// </summary>
    AddEach,

    /// <summary>
    /// Remove a json property at a specific path. Does not require a value to be set.
    /// </summary>
    Remove,

    /// <summary>
    /// Replaces a json property with one of a different value. Identical to a remove and then add.
    /// </summary>
    Replace,

    /// <summary>
    /// Copies a json property from one place and adds it to another. Requires the <see cref="JsonPatch.FromPath"/> property.
    /// </summary>
    Copy,

    /// <summary>
    /// Removes a json property from one place and adds it to another. Identical to removing from one place and adding it to another. Requires the <see cref="JsonPatch.FromPath"/> property.
    /// </summary>
    Move,

    /// <summary>
    /// Add merge is similar to <see cref="Add"/>, however if the target is an array, then the current value and patched value will merge together for improved compatibility.
    /// </summary>
    AddMerge
}

/// <summary>
/// A mod-dependence for a json patch. If your patch depends on another mod, you need to use this.
/// </summary>
public class JsonPatchModDependence
{
    /// <summary>
    /// The mod ID that this patch relies on.
    /// </summary>
    public string? ModId { get; set; }

    /// <summary>
    /// If true, then the patch will only occur if the specified mod is *not* installed.
    /// </summary>
    public bool Invert { get; set; } = false;
}

public class JsonPatch
{
    public float Priority { get; set; } = 0;
    
    /// <summary>
    /// The operation for the patch. Essentially controls what the patch actually does.
    /// </summary>
    public JsonPatchOperationType Op { get; set; }

    /// <summary>
    /// The asset location of the file where the patch should be applied.
    /// </summary>
    public string? File { get; set; }

    /// <summary>
    /// If using <see cref="EnumJsonPatchOp.Move"/> or <see cref="EnumJsonPatchOp.Copy"/>, this is the path to the json property to move or copy from.
    /// </summary>
    public JsonObjectPath? FromPath { get; set; }

    /// <summary>
    /// This is the path to the json property where the operation will take place.
    /// </summary>
    public JsonObjectPath? Path { get; set; }

    /// <summary>
    /// A list of mod dependencies for the patch. Can be used to create patches that are specific on certain mods being installed. Useful for compatibility!
    /// </summary>
    public JsonPatchModDependence[] DependsOn { get; set; } = [];

    /// <summary>
    /// Should this patch be applied or not?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The app side that the patch should be loaded on.
    /// </summary>
    public EnumAppSide Side { get; set; } = EnumAppSide.Universal;

    /// <summary>
    /// If adding, this is the value (or values) that will be added.
    /// </summary>
    [JsonProperty, JsonConverter(typeof(JsonAttributesConverter))]
    public JsonObject? Value { get; set; }
}
