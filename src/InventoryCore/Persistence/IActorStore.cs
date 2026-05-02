namespace InventoryCore;

public interface IActorStore
{
    void Save(ActorSnapshot snapshot);
    ActorSnapshot? Load(string actorId);
    bool Exists(string actorId);
    void Delete(string actorId);
    IReadOnlyList<string> GetAllActorIds();
}
