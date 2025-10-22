namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class uniqueUsername2 : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Course", "Title", unique: true);
            CreateIndex("dbo.Department", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Department", new[] { "Name" });
            DropIndex("dbo.Course", new[] { "Title" });
        }
    }
}
