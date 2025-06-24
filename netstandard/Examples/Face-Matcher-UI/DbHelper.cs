using Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace Face_Matcher_UI
{
    public static class DbHelper
    {
        private static readonly string connectionString = "Data Source=suspects.db;Version=3;";

        public static void Initialize()
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            //string dropTable = "DROP TABLE IF EXISTS Suspect;";
            //new SQLiteCommand(dropTable, conn).ExecuteNonQuery();

            string createTable = @"CREATE TABLE IF NOT EXISTS Suspect (
            suspect_id INTEGER PRIMARY KEY AUTOINCREMENT,
            first_name TEXT NOT NULL,
            last_name TEXT NOT NULL,
            gender TEXT NOT NULL,
            dob TEXT,
            FirNo TEXT,
            created_at TEXT,
            updated_at TEXT,
            image1 TEXT,
            image2 TEXT,
            image3 TEXT,
            image4 TEXT,
            image5 TEXT,
            image6 TEXT,
            image7 TEXT,
            image8 TEXT,
            image9 TEXT,
            image10 TEXT
        );";
            new SQLiteCommand(createTable, conn).ExecuteNonQuery();
        }

        public static void InsertOrUpdateSuspect(Suspect suspect)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            if (suspect.SuspectId == 0)
            {
                string insertSql = @"INSERT INTO Suspect 
                (first_name, last_name, gender, dob, FirNo, created_at, updated_at,
                 image1, image2, image3, image4, image5, image6, image7, image8, image9, image10)
                VALUES (@FirstName, @LastName, @Gender, @Dob, @FirNo, @CreatedAt, @UpdatedAt,
                        @Image1, @Image2, @Image3, @Image4, @Image5, @Image6, @Image7, @Image8, @Image9, @Image10)";
                using var cmd = new SQLiteCommand(insertSql, conn);
                suspect.AddParams(cmd);
                cmd.ExecuteNonQuery();
            }
            else
            {
                string updateSql = @"UPDATE Suspect SET 
                first_name=@FirstName, last_name=@LastName, gender=@Gender,
                dob=@Dob, FirNo=@FirNo,
                updated_at=@UpdatedAt,
                image1=@Image1, image2=@Image2, image3=@Image3, image4=@Image4, image5=@Image5,
                image6=@Image6, image7=@Image7, image8=@Image8, image9=@Image9, image10=@Image10
                WHERE suspect_id=@SuspectId";
                using var cmd = new SQLiteCommand(updateSql, conn);
                suspect.AddParams(cmd);
                cmd.Parameters.AddWithValue("@SuspectId", suspect.SuspectId);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Suspect> GetAllSuspects()
        {
            var list = new List<Suspect>();
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("SELECT * FROM Suspect", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Suspect.FromReader(reader));
            }
            return list;
        }
    }

}
