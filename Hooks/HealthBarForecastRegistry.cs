using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Hooks;

public static class HealthBarForecastRegistry
{
    private static readonly Lock SyncRoot = new();
    private static readonly Dictionary<(string ModId, string SourceId), ProviderEntry> Providers = [];
    private static readonly ConcurrentDictionary<Type, ForeignSegmentAccessors?> ForeignSegmentAccessorCache = new();
    private static long _nextRegistrationOrder;

    public static void Register<TSource>(string modId, string? sourceId = null)
        where TSource : IHealthBarForecastSource, new()
    {
        Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
    }

    public static void Register(string modId, string sourceId, IHealthBarForecastSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(source);
        RegisterProvider(modId, sourceId, source, null);
    }

    public static void RegisterForeign(string modId, string sourceId, Func<Creature, IEnumerable<object>> provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(provider);
        RegisterProvider(modId, sourceId, null, provider);
    }

    public static bool Unregister(string modId, string sourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        lock (SyncRoot)
        {
            return Providers.Remove((modId, sourceId));
        }
    }

    internal static IReadOnlyList<RegisteredHealthBarForecastSegment> GetSegments(Creature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);

        var context = new HealthBarForecastContext(creature);
        List<RegisteredHealthBarForecastSegment> segments = [];
        var powerSequenceOrder = 0L;

        foreach (var source in creature.Powers.OfType<IHealthBarForecastSource>())
            AppendTypedSegments(
                source,
                source.GetType().FullName ?? source.GetType().Name,
                context,
                powerSequenceOrder++,
                segments,
                "creature power");

        ProviderEntry[] snapshot;
        lock (SyncRoot)
        {
            snapshot = Providers.Values
                .OrderBy(entry => entry.RegistrationOrder)
                .ToArray();
        }

        const long externalOrderOffset = 1_000_000L;
        foreach (var entry in snapshot)
        {
            if (entry.Source != null)
            {
                AppendTypedSegments(
                    entry.Source,
                    entry.SourceId,
                    context,
                    externalOrderOffset + entry.RegistrationOrder,
                    segments,
                    $"registered source ({entry.ModId})");
                continue;
            }

            if (entry.ForeignProvider != null)
                AppendForeignSegments(
                    entry.ForeignProvider,
                    entry.SourceId,
                    creature,
                    externalOrderOffset + entry.RegistrationOrder,
                    segments,
                    entry.ModId);
        }

        return segments;
    }

    private static void RegisterProvider(
        string modId,
        string sourceId,
        IHealthBarForecastSource? source,
        Func<Creature, IEnumerable<object>>? foreignProvider)
    {
        lock (SyncRoot)
        {
            var key = (modId, sourceId);
            var registrationOrder = Providers.TryGetValue(key, out var existing)
                ? existing.RegistrationOrder
                : _nextRegistrationOrder++;

            Providers[key] = new ProviderEntry(modId, sourceId, source, foreignProvider, registrationOrder);
        }
    }

    private static void AppendTypedSegments(
        IHealthBarForecastSource source,
        string sourceId,
        HealthBarForecastContext context,
        long sequenceOrder,
        List<RegisteredHealthBarForecastSegment> destination,
        string owner)
    {
        try
        {
            var providedSegments = source.GetHealthBarForecastSegments(context);
            foreach (var segment in providedSegments)
            {
                if (segment.Amount <= 0)
                    continue;

                destination.Add(new RegisteredHealthBarForecastSegment(segment, sequenceOrder));
            }
        }
        catch (Exception ex)
        {
            BaseLibMain.Logger.Warn(
                $"[HealthBarForecast] Source '{sourceId}' from {owner} failed for creature '{context.Creature}': {ex}");
        }
    }

    private static void AppendForeignSegments(
        Func<Creature, IEnumerable<object>> provider,
        string sourceId,
        Creature creature,
        long sequenceOrder,
        List<RegisteredHealthBarForecastSegment> destination,
        string modId)
    {
        try
        {
            var foreignSegments = provider(creature);
            foreach (var foreignSegment in foreignSegments)
            {
                if (!TryConvertForeignSegment(foreignSegment, out var converted))
                    continue;

                if (converted.Amount <= 0)
                    continue;

                destination.Add(new RegisteredHealthBarForecastSegment(converted, sequenceOrder));
            }
        }
        catch (Exception ex)
        {
            BaseLibMain.Logger.Warn(
                $"[HealthBarForecast] Foreign source '{sourceId}' from mod '{modId}' failed for creature '{creature}': {ex}");
        }
    }

    private static bool TryConvertForeignSegment(object? segment, out HealthBarForecastSegment converted)
    {
        converted = default;
        if (segment == null)
            return false;

        if (segment is HealthBarForecastSegment direct)
        {
            converted = direct;
            return true;
        }

        var accessorOrNull = ForeignSegmentAccessorCache.GetOrAdd(segment.GetType(), CreateForeignSegmentAccessors);
        if (accessorOrNull == null)
            return false;
        var accessors = accessorOrNull.Value;

        var amount = accessors.ReadAmount(segment);
        var color = accessors.ReadColor(segment);
        if (!TryParseDirection(accessors.ReadDirection(segment), out var direction))
            return false;

        converted = new HealthBarForecastSegment(amount, color, direction, accessors.ReadOrder(segment));
        return true;
    }

    private static bool TryParseDirection(object? directionValue, out HealthBarForecastDirection direction)
    {
        direction = HealthBarForecastDirection.FromRight;
        if (directionValue == null)
            return false;

        if (directionValue is HealthBarForecastDirection typedDirection)
        {
            direction = typedDirection;
            return true;
        }

        var directionName = directionValue.ToString();
        if (string.IsNullOrWhiteSpace(directionName))
            return false;

        if (directionName.Contains("FromLeft", StringComparison.OrdinalIgnoreCase))
        {
            direction = HealthBarForecastDirection.FromLeft;
            return true;
        }

        if (directionName.Contains("FromRight", StringComparison.OrdinalIgnoreCase))
        {
            direction = HealthBarForecastDirection.FromRight;
            return true;
        }

        return false;
    }

    private static ForeignSegmentAccessors? CreateForeignSegmentAccessors(Type type)
    {
        var amount = type.GetProperty("Amount", BindingFlags.Instance | BindingFlags.Public);
        var color = type.GetProperty("Color", BindingFlags.Instance | BindingFlags.Public);
        var direction = type.GetProperty("Direction", BindingFlags.Instance | BindingFlags.Public);
        var order = type.GetProperty("Order", BindingFlags.Instance | BindingFlags.Public);

        if (amount?.PropertyType != typeof(int) ||
            color?.PropertyType != typeof(Color) ||
            direction == null)
            return null;

        return new ForeignSegmentAccessors(
            segment => (int)amount.GetValue(segment)!,
            segment => (Color)color.GetValue(segment)!,
            segment => direction.GetValue(segment),
            order?.PropertyType == typeof(int)
                ? segment => (int)order.GetValue(segment)!
                : _ => 0);
    }

    internal readonly record struct RegisteredHealthBarForecastSegment(
        HealthBarForecastSegment Segment,
        long SequenceOrder);

    private readonly record struct ProviderEntry(
        string ModId,
        string SourceId,
        IHealthBarForecastSource? Source,
        Func<Creature, IEnumerable<object>>? ForeignProvider,
        long RegistrationOrder);

    private readonly record struct ForeignSegmentAccessors(
        Func<object, int> ReadAmount,
        Func<object, Color> ReadColor,
        Func<object, object?> ReadDirection,
        Func<object, int> ReadOrder);
}