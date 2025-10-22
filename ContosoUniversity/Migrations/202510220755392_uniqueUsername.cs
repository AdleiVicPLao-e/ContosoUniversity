namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class uniqueUsername : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Person", "UserName", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Person", new[] { "UserName" });
        }
    }
}
