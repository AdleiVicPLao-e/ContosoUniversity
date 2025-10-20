namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RowVersion : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Course", "RowVersion");
            DropColumn("dbo.Instructor", "RowVersion");
            DropColumn("dbo.OfficeAssignment", "RowVersion");
            DropColumn("dbo.Enrollment", "RowVersion");
            DropColumn("dbo.Student", "RowVersion");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Student", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.Enrollment", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.OfficeAssignment", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.Instructor", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.Course", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
        }
    }
}
