using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace Model
{
    public class Suspect
    {
        public int SuspectId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Dob { get; set; } = "";
        public string FirNo { get; set; } = "";
        public string[] Images { get; set; } = new string[10];
        public string CreatedAt { get; set; } = DateTime.Now.ToString("s");
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("s");
        public List<Image> GetImageList()
        {
            var result = new List<Image>();

            foreach (var base64 in Images)
            {
                if (string.IsNullOrWhiteSpace(base64))
                    continue;

                try
                {
                    byte[] bytes = Convert.FromBase64String(base64);
                    using var ms = new MemoryStream(bytes);
                    var img = Image.FromStream(ms);  // Note: Image.FromStream makes a lazy-loading image
                    result.Add(new Bitmap(img));     // We clone to ensure it's usable after stream closes
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image conversion error: {ex.Message}");
                }
            }

            return result;
        }
        public static Suspect FromReader(SQLiteDataReader reader)
        {
            var s = new Suspect
            {
                SuspectId = Convert.ToInt32(reader["suspect_id"]),
                FirstName = reader["first_name"].ToString(),
                LastName = reader["last_name"].ToString(),
                Gender = reader["gender"].ToString(),
                Dob = reader["dob"].ToString(),
                FirNo = reader["FirNo"].ToString(),
                CreatedAt = reader["created_at"].ToString(),
                UpdatedAt = reader["updated_at"].ToString(),
            };
            for (int i = 0; i < 10; i++)
            {
                s.Images[i] = reader[$"image{i + 1}"]?.ToString() ?? "";
            }
            return s;
        }

        public void AddParams(SQLiteCommand cmd)
        {
            cmd.Parameters.AddWithValue("@FirstName", FirstName);
            cmd.Parameters.AddWithValue("@LastName", LastName);
            cmd.Parameters.AddWithValue("@Gender", Gender);
            cmd.Parameters.AddWithValue("@Dob", Dob);
            cmd.Parameters.AddWithValue("@FirNo", FirNo);
            cmd.Parameters.AddWithValue("@CreatedAt", CreatedAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", UpdatedAt);
            for (int i = 0; i < 10; i++)
            {
                cmd.Parameters.AddWithValue($"@Image{i + 1}", Images[i]);
            }
        }
    }

}
