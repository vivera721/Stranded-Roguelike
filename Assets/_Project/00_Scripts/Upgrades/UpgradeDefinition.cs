namespace StrandedRoguelike
{
    [System.Serializable]
    public readonly struct UpgradeDefinition
    {
        public readonly UpgradeId Id;
        public readonly UpgradeCategory Category;
        public readonly string Title;
        public readonly string Description;

        public UpgradeDefinition(UpgradeId id, UpgradeCategory category, string title, string description)
        {
            Id = id;
            Category = category;
            Title = title;
            Description = description;
        }
    }
}
