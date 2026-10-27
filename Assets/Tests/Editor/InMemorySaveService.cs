using TrapTheCat.Services;

public sealed class InMemorySaveService : ISaveService
{
    public SaveData Stored = new SaveData();
    public int SaveCount;

    public SaveData Load()
    {
        return Stored;
    }

    public void Save(SaveData data)
    {
        Stored = data;
        SaveCount++;
    }
}
