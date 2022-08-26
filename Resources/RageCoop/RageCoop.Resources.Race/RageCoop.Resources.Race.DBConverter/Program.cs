using LiteDB;
using System.Data.SQLite;
using RageCoop.Resources.Race.Objects;

var filename = "times.db";

var Connection=new SQLiteConnection(new SQLiteConnectionStringBuilder()
{
    DataSource = filename,
    Version = 3
}.ToString());
Connection.Open();

var newDb = new LiteDatabase("Records.db");
var newRecords = newDb.GetCollection<Record>();
new SQLiteCommand(@"
    CREATE TABLE IF NOT EXISTS `times` (
        `Id` INTEGER PRIMARY KEY AUTOINCREMENT,
        `Race` TEXT,
        `Player` TEXT,
        `Time` INTEGER,
        `Win` INTEGER
    );"
, Connection).ExecuteNonQuery();
var reader = new SQLiteCommand("SELECT * FROM `times`;",Connection).ExecuteReader();
int i=0;
while (reader.Read())
{
    newRecords.Insert(new Record()
    {
        Race=reader["Race"].ToString(),
        Player=reader["Player"].ToString(),
        Time=(long)reader["Time"],
        Win=reader["Win"].ToString()=="1"
    });
    i++;
    Console.Write($"\rMigrated {i} records");
}
Console.WriteLine("\nMigration has completed, press enter to exit");
Console.ReadLine();
Connection.Close();
newDb.Dispose();

