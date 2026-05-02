namespace InventoryCore;

public sealed class InMemoryActorStore : IActorStore
{
    private readonly Dictionary<string, ActorSnapshot> _store = new();

    public void Save(ActorSnapshot snapshot) => _store[snapshot.ActorId] = snapshot;

    public ActorSnapshot? Load(string actorId) =>
        _store.TryGetValue(actorId, out var snap) ? snap : null;

    public bool Exists(string actorId) => _store.ContainsKey(actorId);

    public void Delete(string actorId) => _store.Remove(actorId);

    public IReadOnlyList<string> GetAllActorIds() => _store.Keys.ToList();
}
