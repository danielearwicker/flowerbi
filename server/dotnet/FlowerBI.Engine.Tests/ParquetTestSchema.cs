using System;

namespace FlowerBI.Engine.Tests;

[DbSchema("ParquetTest")]
public static class ParquetTestSchema
{
    [DbTable("Date")]
    public static class Date
    {
        public static readonly PrimaryKey<DateTime> Id = new("Id");
        public static readonly Column<int> DayOfMonth = new("DayOfMonth");
        public static readonly Column<int> Month = new("Month");
        public static readonly Column<int> Year = new("Year");
        public static readonly Column<string> MonthName = new("MonthName");
        public static readonly Column<DateOnly> StartOfMonth = new("StartOfMonth");
    }

    [DbTable("Business")]
    public static class Business
    {
        public static readonly Column<double> Amount = new("Amount");
        public static readonly Column<string> Product = new("Product");
        public static readonly ForeignKey<DateTime> Purchased = new("Purchased", Date.Id);
    }
}
