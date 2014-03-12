using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using XMLFeedParser.Config;

namespace XMLFeedParser.Model
{
    static class DBGatewayHkjc
    {
        internal static void UpdateHKGData(IEnumerable<RaceHKG> races, IEnumerable<RunnerHKG> runners)
        {
            //RUNNERS
            var dtRunners = new DataTable();
            dtRunners.Columns.Add("MeetingId", typeof(int));
            dtRunners.Columns.Add("RaceNumber", typeof(int));
            dtRunners.Columns.Add("HorseNumber", typeof(int));
            dtRunners.Columns.Add("isScratched", typeof(bool));
            dtRunners.Columns.Add("WinOdds", typeof(float));
            dtRunners.Columns.Add("PlaceOdds", typeof(float));
            dtRunners.Columns.Add("isWinFavorite", typeof(float));
            dtRunners.Columns.Add("WinDropby20", typeof(bool));
            dtRunners.Columns.Add("WinDropby50", typeof(bool));
            dtRunners.Columns.Add("isPlaceFavorite", typeof(bool));
            dtRunners.Columns.Add("PlaceDropby20", typeof(bool));
            dtRunners.Columns.Add("PlaceDropby50", typeof(bool));
            dtRunners.Columns.Add("RaceWinPool", typeof(float));
            dtRunners.Columns.Add("RacePPPool", typeof(float));
            dtRunners.Columns.Add("RaceStatus", typeof(string));

            runners.ToList().ForEach(r => 
                {
                    var race = races.FirstOrDefault(rac => rac.MeetingId == r.MeetingId && rac.RaceNumber == r.RaceNumber);

                    dtRunners.Rows.Add(
                    r.MeetingId,
                    r.RaceNumber,
                    r.HorseNumber,
                    r.isScratched,
                    r.WinOdds,
                    r.Placeodds,
                    r.IsWinFavourite,
                    r.WinDropBy20,
                    r.WinDropBy50,
                    r.IsPlaceFavourite,
                    r.PlaceDropBy20,
                    r.PlaceDropBy50,
                    race.RaceWinPool,
                    race.RacePPPool,
                    race.RaceStatus);
                });

            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = conn.CreateCommand())
                {
                    command.CommandText = "UpdateCreateMeetingsHKG";
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1;
                    param1 = command.Parameters.AddWithValue("@runners", dtRunners);
                    param1.SqlDbType = SqlDbType.Structured;
                    param1.TypeName = "dbo.RunnerTableTypeHKG";

                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
