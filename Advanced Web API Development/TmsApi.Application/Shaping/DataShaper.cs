using System.Dynamic;
using System.Reflection;

namespace TmsApi.Application.Shaping;

public class DataShaper<T>
{
    private readonly PropertyInfo[] _properties;

    public DataShaper()
    {
        _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ExpandoObject> ShapeData(IEnumerable<T> entities, string? fieldsString)
    {
        var requiredProperties = FetchRequiredProperties(fieldsString);
        return entities.Select(entity => FetchDataForEntity(entity, requiredProperties)).ToList();
    }

    public ExpandoObject ShapeData(T entity, string? fieldsString)
    {
        var requiredProperties = FetchRequiredProperties(fieldsString);
        return FetchDataForEntity(entity, requiredProperties);
    }

    private IEnumerable<PropertyInfo> FetchRequiredProperties(string? fieldsString)
    {
        if (string.IsNullOrWhiteSpace(fieldsString))
        {
            return _properties;
        }

        var fields = fieldsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(f => f.Trim().ToLowerInvariant())
                                 .ToHashSet();

        var matched = _properties.Where(p => fields.Contains(p.Name.ToLowerInvariant())).ToList();
        return matched.Any() ? matched : _properties;
    }

    private ExpandoObject FetchDataForEntity(T entity, IEnumerable<PropertyInfo> requiredProperties)
    {
        var shapedObject = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)shapedObject;

        foreach (var property in requiredProperties)
        {
            var value = property.GetValue(entity);
            dictionary.Add(property.Name, value);
        }

        return shapedObject;
    }
}
