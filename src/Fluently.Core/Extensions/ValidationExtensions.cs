namespace Fluently;

internal static class ValidationExtensions
{
    extension(Type type)
    {
        public void ThrowIfNotAssignableTo<T>()
        {
            ArgumentNullException.ThrowIfNull(type);

            if (!typeof(T).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"Type '{type}' must implement {typeof(T).Name}.",
                    nameof(type));
            }
        }
    }
}
