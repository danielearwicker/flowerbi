using System;

namespace TinyBI.DemoSchema
{    
    [DbSchema("PowerBI")]
    public static class NxgSchema
    {       
        [DbTable("Date")]
        public static class DateReported
        {
            public static readonly PrimaryKey<DateTime> Id = new PrimaryKey<DateTime>("Id");
            public static readonly Column<short> CalendarYearNumber = new Column<short>("CalendarYearNumber");
            public static readonly Column<DateTime> FirstDayOfQuarter = new Column<DateTime>("FirstDayOfQuarter");
            public static readonly Column<DateTime> FirstDayOfMonth = new Column<DateTime>("FirstDayOfMonth");
        }

        [DbTable("Date")]
        public static class DateResolved
        {
            public static readonly PrimaryKey<DateTime> Id = new PrimaryKey<DateTime>("Id");
            public static readonly Column<short> CalendarYearNumber = new Column<short>("CalendarYearNumber");
            public static readonly Column<DateTime> FirstDayOfQuarter = new Column<DateTime>("FirstDayOfQuarter");
            public static readonly Column<DateTime> FirstDayOfMonth = new Column<DateTime>("FirstDayOfMonth");
        }

        [DbTable("Date")]
        public static class DateAssigned
        {
            public static readonly PrimaryKey<DateTime> Id = new PrimaryKey<DateTime>("Id");
            public static readonly Column<short> CalendarYearNumber = new Column<short>("CalendarYearNumber");
            public static readonly Column<DateTime> FirstDayOfQuarter = new Column<DateTime>("FirstDayOfQuarter");
            public static readonly Column<DateTime> FirstDayOfMonth = new Column<DateTime>("FirstDayOfMonth");
        }

        [DbTable("Workflow")]
        public static class Workflow
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<bool> Resolved = new Column<bool>("Resolved");
            public static readonly Column<string> WorkflowState = new Column<string>("WorkflowState");
            public static readonly Column<string> SourceOfError = new Column<string>("SourceOfError");
            public static readonly Column<bool> FixedByCustomer = new Column<bool>("FixedByCustomer");
        }

        [DbTable("Category")]
        public static class Category
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> Label = new Column<string>("Label");
        }

        [DbTable("Customer")]
        public static class Customer
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> CustomerName = new Column<string>("CustomerName");
        }

        [DbTable("Coder")]
        public static class CoderAssigned
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> FullName = new Column<string>("FullName");
        }

        [DbTable("Coder")]
        public static class CoderResolved
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> FullName = new Column<string>("FullName");
        }

        [DbTable("CategoryCombination")]
        public static class CategoryCombination
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<bool> Crashed = new Column<bool>("Crashed");
            public static readonly Column<bool> DataLoss = new Column<bool>("DataLoss");
            public static readonly Column<bool> SecurityBreach = new Column<bool>("SecurityBreach");
            public static readonly Column<bool> OffByOne = new Column<bool>("OffByOne");
            public static readonly Column<bool> Slow = new Column<bool>("Slow");
            public static readonly Column<bool> StackOverflow = new Column<bool>("StackOverflow");
        }

        [DbTable("Bug")]
        public static class Bug
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly ForeignKey<int> WorkflowId = new ForeignKey<int>("WorkflowId", Workflow.Id);
            public static readonly ForeignKey<int> CustomerId = new ForeignKey<int>("CustomerId", Customer.Id);
            public static readonly ForeignKey<DateTime> ReportedDate = new ForeignKey<DateTime>("ReportedDate", DateReported.Id);
            public static readonly ForeignKey<DateTime> ResolvedDate = new ForeignKey<DateTime>("ResolvedDate", DateResolved.Id);
            public static readonly ForeignKey<DateTime> AssignedDate = new ForeignKey<DateTime>("AssignedDate", DateAssigned.Id);
            public static readonly ForeignKey<int> CategoryCombinationId = new ForeignKey<int>("CategoryCombinationId", CategoryCombination.Id);
            public static readonly ForeignKey<int> AssignedCoderId = new ForeignKey<int>("AssignedCoderId", CoderAssigned.Id);
            public static readonly ForeignKey<int> ResolvedCoderId = new ForeignKey<int>("ResolvedCoderId", CoderResolved.Id);
        }
    }
}
