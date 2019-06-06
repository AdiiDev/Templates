namespace Common.DomainBase
{
    /// <summary>
    /// Base for all entities where T is type of id
    /// </summary>
    /// <typeparam name="T">Id type</typeparam>
    public interface IBaseEntity<T>
    {
        T Id { get; set; }
    }
}
