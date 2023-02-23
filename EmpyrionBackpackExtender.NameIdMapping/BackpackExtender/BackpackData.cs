namespace EmpyrionBackpackExtender.NameIdMapping.BackpackExtender;

// From: GitHub-TC/EmpyrionBackpackExtender
// https://github.com/GitHub-TC/EmpyrionBackpackExtender/blob/5b3ce75571d4d78bdeffb65b5a810b2b2c5e642f/EmpyrionBackpackExtender/BackpackData.cs

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class ItemNameStack
{
    public int id;
    public string name;
    public int count;
    public byte slotIdx;
    public int ammo;
    public int decay;
}

public class BackpackItems
{
    public ItemNameStack[] Items { get; set; }
}

public class BackpackData
{
    public string OpendBySteamId { get; set; }
    public string OpendByName { get; set; }
    public int LastUsed { get; set; }
    public string LastAccessPlayerName { get; set; }
    public string LastAccessFactionName { get; set; }
    public BackpackItems[] Backpacks { get; set; } = new BackpackItems[] { };
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
