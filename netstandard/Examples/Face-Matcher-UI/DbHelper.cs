using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Face_Matcher_UI
{
    public static class DbHelper
    {
        private static readonly string connectionString = "Data Source=suspects.db;Version=3;BusyTimeout=5000;";
        private static readonly object writeLock = new object();
        private static readonly string SuspectsFolder = "SuspectsDir";
        private static readonly string FramesFolder = "FramesDir";

        public static void Initialize()
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            using (var pragmaCmd = new SQLiteCommand("PRAGMA journal_mode=WAL;", conn))
            {
                pragmaCmd.ExecuteNonQuery();
            }
            //string dropTable = "DROP TABLE IF EXISTS Matchfacelogs;";
            //new SQLiteCommand(dropTable, conn).ExecuteNonQuery();

            //dropTable = "DROP TABLE IF EXISTS Suspect;";
            //new SQLiteCommand(dropTable, conn).ExecuteNonQuery();

            Directory.CreateDirectory(SuspectsFolder);
            Directory.CreateDirectory(FramesFolder);
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

            string createMatchLogsTable = @"CREATE TABLE IF NOT EXISTS Matchfacelogs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    capture_time TEXT NOT NULL,
    frame TEXT NOT NULL,
    cctv_id INTEGER NOT NULL,
    suspect_id INTEGER,
    suspect TEXT,
    distance REAL NOT NULL,
    created_date TEXT NOT NULL,
    filename TEXT,
    frametime TEXT
);";
            new SQLiteCommand(createMatchLogsTable, conn).ExecuteNonQuery();
        }
        public static int InsertOrUpdateSuspect(Suspect suspect)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            if (suspect.SuspectId == 0)
            {
                string insertSql = @"INSERT INTO Suspect 
        (first_name, last_name, gender, dob, FirNo, created_at, updated_at)
        VALUES (@FirstName, @LastName, @Gender, @Dob, @FirNo, @CreatedAt, @UpdatedAt)";
                using var cmd = new SQLiteCommand(insertSql, conn);
                suspect.AddParams(cmd); // just text params, no images yet
                cmd.ExecuteNonQuery();

                using var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn);
                suspect.SuspectId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            string suspectDir = Path.Combine(SuspectsFolder, suspect.SuspectId.ToString());
            Directory.CreateDirectory(suspectDir);

            string[] imagePaths = new string[10];

            //foreach (var (index, data) in suspect.Images) // index: 1–10
            //{
            //    if (data != null && data.Length > 0)
            //    {
            //        string filePath = Path.Combine(suspectDir, $"image{index}.jpg");
            //        File.WriteAllBytes(filePath, data);
            //        imagePaths[index - 1] = filePath;
            //    }
            //}

            for (int i = 0; i < suspect.Images.Length; i++)
            {
                var base64 = suspect.Images[i];
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    byte[] data = Convert.FromBase64String(base64);
                    string filePath = Path.Combine(suspectDir, $"image{i + 1}.jpg");
                    File.WriteAllBytes(filePath, data);
                    imagePaths[i] = filePath;
                }
            }

            string updateSql = @"UPDATE Suspect SET 
        first_name=@FirstName, last_name=@LastName, gender=@Gender,
        dob=@Dob, FirNo=@FirNo, updated_at=@UpdatedAt,
        image1=@Image1, image2=@Image2, image3=@Image3, image4=@Image4, image5=@Image5,
        image6=@Image6, image7=@Image7, image8=@Image8, image9=@Image9, image10=@Image10
        WHERE suspect_id=@SuspectId";

            using var updateCmd = new SQLiteCommand(updateSql, conn);
            suspect.AddParams(updateCmd);
            updateCmd.Parameters.AddWithValue("@SuspectId", suspect.SuspectId);

            for (int i = 1; i <= 10; i++)
            {
                updateCmd.Parameters.AddWithValue($"@Image{i}", imagePaths[i - 1] ?? (object)DBNull.Value);
            }

            updateCmd.ExecuteNonQuery();

            return suspect.SuspectId;
        }


        public static int InsertOrUpdateSuspect_bkup(Suspect suspect)
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
                using var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn);
                suspect.SuspectId = Convert.ToInt32(idCmd.ExecuteScalar());
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
            return suspect.SuspectId;
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
        public static (string name, List<byte[]> blobs)? GetSuspectById(int suspectId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string sql = @"SELECT first_name,
                  image1, image2, image3, image4, image5,
                  image6, image7, image8, image9, image10
           FROM Suspect
           WHERE suspect_id = @SuspectId";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SuspectId", suspectId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var blobs = new List<byte[]>();

                for (int i = 1; i <= 10; i++) // image1 to image10
                {
                    if (!reader.IsDBNull(i))
                    {
                        string path = reader.GetString(i);

                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        {
                            try
                            {
                                byte[] bytes = File.ReadAllBytes(path);
                                if (bytes.Length > 0)
                                    blobs.Add(bytes);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Failed to read file for suspect {suspectId} image{i}: {ex.Message}");
                            }
                        }
                    }
                }

                return (name, blobs);
            }

            return null; // suspect not found
        }

        public static (string name, List<byte[]> blobs)? GetSuspectById_backup(int suspectId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string sql = @"SELECT first_name,
                          image1, image2, image3, image4, image5,
                          image6, image7, image8, image9, image10
                   FROM Suspect
                   WHERE suspect_id = @SuspectId";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SuspectId", suspectId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var blobs = new List<byte[]>();

                for (int i = 1; i <= 10; i++) // image1 to image10
                {
                    if (!reader.IsDBNull(i))
                    {
                        string base64 = reader.GetString(i);
                        try
                        {
                            byte[] blob = Convert.FromBase64String(base64);
                            if (blob.Length > 0)
                                blobs.Add(blob);
                        }
                        catch (FormatException ex)
                        {
                            Console.WriteLine($"Warning: Invalid base64 for suspect {suspectId} image{i}: {ex.Message}");
                        }
                    }
                }

                return (name, blobs);
            }

            return null; // suspect not found
        }

        public static void InsertMatchFaceLog(
 string captureTime,
 string frameBase64,
 int cctvId,
 int? suspectId,
 string suspectName,
 float distance,
 string filename,
 string frametime)
        {
            lock (writeLock)
            {
                string formattedCaptureTime = captureTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                string formattedCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

                string frameDir = Path.Combine(FramesFolder, suspectId?.ToString() ?? "Unknown");
                Directory.CreateDirectory(frameDir); // ✅ ensure directory exists
                string frameFileName = Path.Combine(frameDir, $"{Guid.NewGuid()}.jpg");
                byte[] frameBytes = Convert.FromBase64String(frameBase64);
                File.WriteAllBytes(frameFileName, frameBytes);

                using var conn = new SQLiteConnection(connectionString);
                conn.Open();

                using var cmd = new SQLiteCommand(@"
            INSERT INTO Matchfacelogs 
            (capture_time, frame, cctv_id, suspect_id, suspect, distance, created_date, filename, frametime)
            VALUES 
            (@capture_time, @frame, @cctv_id, @suspect_id, @suspect, @distance, @created_date, @filename, @frametime);
        ", conn);

                cmd.Parameters.AddWithValue("@capture_time", formattedCaptureTime);
                cmd.Parameters.AddWithValue("@frame", frameFileName);
                cmd.Parameters.AddWithValue("@cctv_id", cctvId);
                cmd.Parameters.AddWithValue("@suspect_id", (object?)suspectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@suspect", suspectName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@distance", distance);
                cmd.Parameters.AddWithValue("@created_date", formattedCreatedDate);
                cmd.Parameters.AddWithValue("@filename", filename ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@frametime", frametime ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public static void InsertMatchFaceLog_backup(
     string captureTime,
     string frame,
     int cctvId,
     int? suspectId,
     string suspectName,
     float distance,
     string filename,
     string frametime)
        {
            string formattedCaptureTime = captureTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            string formattedCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(@"
        INSERT INTO Matchfacelogs 
        (capture_time, frame, cctv_id, suspect_id, suspect, distance, created_date, filename, frametime)
        VALUES 
        (@capture_time, @frame, @cctv_id, @suspect_id, @suspect, @distance, @created_date, @filename, @frametime);
    ", conn);

            cmd.Parameters.AddWithValue("@capture_time", formattedCaptureTime);
            cmd.Parameters.AddWithValue("@frame", frame);
            cmd.Parameters.AddWithValue("@cctv_id", cctvId);
            cmd.Parameters.AddWithValue("@suspect_id", (object?)suspectId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@suspect", suspectName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@distance", distance);
            cmd.Parameters.AddWithValue("@created_date", formattedCreatedDate);
            cmd.Parameters.AddWithValue("@filename", filename ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@frametime", frametime ?? (object)DBNull.Value);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error in InsertMatchFaceLog: {ex.Message}");
                throw;
            }
        }
        public static List<string> GetMatchedFramesForSuspect(int suspectId)
        {
            var results = new List<string>();
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string sql = @"SELECT frame FROM Matchfacelogs
                   WHERE suspect_id = @SuspectId
                   ORDER BY capture_time DESC";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SuspectId", suspectId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    results.Add(reader.GetString(0)); // base64 string
                }
            }

            return results;
        }
        public static List<MatchLog> GetMatchedLogsForSuspect(int suspectId)
        {
            var results = new List<MatchLog>();

            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(@"
        SELECT id, capture_time, frame, cctv_id, suspect_id, suspect, distance, created_date, filename, frametime 
        FROM Matchfacelogs
        WHERE suspect_id = @SuspectId
        ORDER BY capture_time DESC", conn);

            cmd.Parameters.AddWithValue("@SuspectId", suspectId);

            using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

            while (reader.Read())
            {
                results.Add(new MatchLog
                {
                    Id = reader.GetInt32(0),
                    CaptureTime = DateTime.Parse(reader.GetString(1)),
                    FrameBase64 = reader.IsDBNull(2) ? "" : reader.GetString(2), // ✅ Correct for TEXT
                    CctvId = reader.GetInt32(3),
                    SuspectId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Suspect = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Distance = reader.GetFloat(6),
                    CreatedDate = DateTime.Parse(reader.GetString(7)),
                    Filename = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Frametime = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }
            return results;
        }


        public static void DeleteSuspect(int suspectId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Get all frame file paths before deleting logs
                List<string> framePaths = new List<string>();
                using (var cmdSelect = new SQLiteCommand("SELECT frame FROM Matchfacelogs WHERE suspect_id = @id", conn, transaction))
                {
                    cmdSelect.Parameters.AddWithValue("@id", suspectId);
                    using var reader = cmdSelect.ExecuteReader();
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            framePaths.Add(reader.GetString(0));
                    }
                }

                // 2. Delete from Matchfacelogs
                using (var cmd1 = new SQLiteCommand("DELETE FROM Matchfacelogs WHERE suspect_id = @id", conn, transaction))
                {
                    cmd1.Parameters.AddWithValue("@id", suspectId);
                    cmd1.ExecuteNonQuery();
                }

                // 3. Delete from Suspect
                using (var cmd2 = new SQLiteCommand("DELETE FROM Suspect WHERE suspect_id = @id", conn, transaction))
                {
                    cmd2.Parameters.AddWithValue("@id", suspectId);
                    cmd2.ExecuteNonQuery();
                }

                transaction.Commit();

                // 4. Delete suspect folder
                string suspectFolder = Path.Combine(SuspectsFolder, suspectId.ToString());
                if (Directory.Exists(suspectFolder))
                {
                    try
                    {
                        Directory.Delete(suspectFolder, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting suspect folder: {ex.Message}");
                    }
                }

                // 5. Delete frame files
                foreach (var framePath in framePaths)
                {
                    if (!string.IsNullOrWhiteSpace(framePath) && File.Exists(framePath))
                    {
                        try
                        {
                            File.Delete(framePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting frame file {framePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Error deleting suspect: {ex.Message}");
            }
        }


        public static void DeleteSuspect_backup(int suspectId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Delete from Matchfacelogs
                using (var cmd1 = new SQLiteCommand("DELETE FROM Matchfacelogs WHERE suspect_id = @id", conn, transaction))
                {
                    cmd1.Parameters.AddWithValue("@id", suspectId);
                    cmd1.ExecuteNonQuery();
                }

                // 2. Delete from Suspect
                using (var cmd2 = new SQLiteCommand("DELETE FROM Suspect WHERE suspect_id = @id", conn, transaction))
                {
                    cmd2.Parameters.AddWithValue("@id", suspectId);
                    cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Error deleting suspect: {ex.Message}");
            }
        }




    }
    public class MatchFrame
    {
        public string Base64Image { get; set; }
        public DateTime CaptureTime { get; set; }
        public float Distance { get; set; }
    }
    public class MatchLog
    {
        public int Id { get; set; }
        public DateTime CaptureTime { get; set; }
        public string FrameBase64 { get; set; }
        public int CctvId { get; set; }
        public int SuspectId { get; set; }
        public string Suspect { get; set; }
        public float Distance { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Filename { get; set; }
        public string Frametime { get; set; }

    }
}
