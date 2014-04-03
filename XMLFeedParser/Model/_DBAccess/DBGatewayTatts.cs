using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using XMLFeedParser.Config;

namespace XMLFeedParser.Model
{
    static class DBGatewayTatts
    {
        internal static void UpdateTattsData(IEnumerable<Meeting> meetings, IEnumerable<RaceTatts> races, IEnumerable<RunnerTatts> runners, IEnumerable<RaceOdds> odds)
        {
            DataTable dtMeetings, dtRaces, dtRunners, dtOdds;
            createTattsDataTables(meetings, races, runners, odds,
                out dtMeetings, out dtRaces, out dtRunners, out dtOdds);


            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = conn.CreateCommand())
                {
                    command.CommandText = "UpdateRaceDayData";
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1;
                    param1 = command.Parameters.AddWithValue("@meetings", dtMeetings);
                    param1.SqlDbType = SqlDbType.Structured;
                    param1.TypeName = "dbo.MeetingTableType";

                    SqlParameter param2;
                    param2 = command.Parameters.AddWithValue("@races", dtRaces);
                    param2.SqlDbType = SqlDbType.Structured;
                    param2.TypeName = "dbo.RaceTableType";

                    SqlParameter param3;
                    param3 = command.Parameters.AddWithValue("@runners", dtRunners);
                    param3.SqlDbType = SqlDbType.Structured;
                    param3.TypeName = "dbo.RunnerTableType";

                    SqlParameter param4;
                    param4 = command.Parameters.AddWithValue("@odds", dtOdds);
                    param4.SqlDbType = SqlDbType.Structured;
                    param4.TypeName = "dbo.RaceOddsTableType";

                    command.ExecuteNonQuery();
                }
            }
        }

        private static void createTattsDataTables(IEnumerable<Meeting> meetings, IEnumerable<RaceTatts> races, IEnumerable<RunnerTatts> runners, IEnumerable<RaceOdds> odds,
            out DataTable _dtMeetings, out DataTable _dtRaces, out DataTable _dtRunners, out DataTable _dtOdds)
        {
            if (meetings == null)
                meetings = new List<Meeting>();
            if (races == null)
                races = new List<RaceTatts>();
            if (runners == null)
                runners = new List<RunnerTatts>();
            if (odds == null)
                odds = new List<RaceOdds>();

            //MEETINGS
            var dtMeetings = new DataTable();
            dtMeetings.Columns.Add("MeetingId", typeof(int));
            dtMeetings.Columns.Add("isAbandoned", typeof(bool));
            dtMeetings.Columns.Add("RaceDayDate", typeof(DateTime));
            dtMeetings.Columns.Add("VenueName", typeof(string));
            dtMeetings.Columns.Add("NumberOfRaces", typeof(int));
            dtMeetings.Columns.Add("MeetingCode", typeof(string));

            meetings.ToList().ForEach(m => dtMeetings.Rows.Add(
                m.MeetingId,
                m.isAbandoned,
                m.MeetingDate,
                m.RacecourseName,
                m.NumberOfRaces, //TODO
                m.MeetingCode)); //TODO

            _dtMeetings = dtMeetings;


            //RACES
            var dtRaces = new DataTable();
            dtRaces.Columns.Add("MeetingId", typeof(int));
            dtRaces.Columns.Add("RaceNumber", typeof(int));
            dtRaces.Columns.Add("RaceJumpTimeUTC", typeof(DateTime));
            dtRaces.Columns.Add("RaceName", typeof(string));
            dtRaces.Columns.Add("DistanceName", typeof(string));
            dtRaces.Columns.Add("TrackRating", typeof(int));
            dtRaces.Columns.Add("isTrackChanged", typeof(bool));
            dtRaces.Columns.Add("RaceStatus", typeof(string));
            dtRaces.Columns.Add("NumberOfRunners", typeof(int));
            dtRaces.Columns.Add("LocalJumpTime", typeof(DateTime));

            races.ToList().ForEach(r => dtRaces.Rows.Add(
                r.MeetingId,
                r.RaceNumber,
                r.RaceJumpTimeUTC,
                r.RaceName,
                r.DistanceName,
                r.TrackRating,
                r.isTrackChanged,
                r.RaceStatus,
                r.NumberOfRunners,
                r.LocalJumpTime));

            _dtRaces = dtRaces;


            //RUNNERS
            var dtRunners = new DataTable();
            dtRunners.Columns.Add("MeetingId", typeof(int));
            dtRunners.Columns.Add("RaceNumber", typeof(int));
            dtRunners.Columns.Add("HorseNumber", typeof(int));
            dtRunners.Columns.Add("HorseName", typeof(string));
            dtRunners.Columns.Add("JockeyName", typeof(string));
            dtRunners.Columns.Add("Barrier", typeof(string));
            dtRunners.Columns.Add("AUSHcpWt", typeof(string));
            dtRunners.Columns.Add("isScratched", typeof(bool));
            dtRunners.Columns.Add("isJockeyChanged", typeof(bool));
            dtRunners.Columns.Add("Place", typeof(int));
            dtRunners.Columns.Add("WinOdds", typeof(float));
            dtRunners.Columns.Add("PlaceOdds", typeof(float));
            dtRunners.Columns.Add("isFavorite", typeof(float));

            runners.ToList().ForEach(r => dtRunners.Rows.Add(
                r.MeetingId,
                r.RaceNumber,
                r.HorseNumber,
                r.HorseName,
                r.JockeyName,
                r.Barrier,
                r.AUS_HcpWt,
                r.isScratched,
                r.isJockeyChanged,
                r.Place,
                r.WinOdds,
                r.Placeodds,
                r.IsFavourite));

            _dtRunners = dtRunners;


            //ODDS
            var dtOdds = new DataTable();
            dtOdds.Columns.Add("MeetingId", typeof(int));
            dtOdds.Columns.Add("RaceNumber", typeof(int));
            dtOdds.Columns.Add("EXPoolTotal", typeof(float));
            dtOdds.Columns.Add("EXDivAmount", typeof(float));
            dtOdds.Columns.Add("F4PoolTotal", typeof(float));
            dtOdds.Columns.Add("F4DivAmount", typeof(float));
            dtOdds.Columns.Add("QNPoolTotal", typeof(float));
            dtOdds.Columns.Add("QNDivAmount", typeof(float));//
            dtOdds.Columns.Add("TFDivAmount", typeof(float));
            dtOdds.Columns.Add("TFPoolTotal", typeof(float));
            dtOdds.Columns.Add("WWDivAmount", typeof(float));
            dtOdds.Columns.Add("WWPoolTotal", typeof(float));
            dtOdds.Columns.Add("PPDivAmount", typeof(float));
            dtOdds.Columns.Add("PPPoolTotal", typeof(float));
            dtOdds.Columns.Add("RaceStatus", typeof(string));

            odds.ToList().ForEach(o => dtOdds.Rows.Add(
                o.MeetingId,
                o.RaceNumber,
                o.EXPoolTotal,
                o.EXDivAmount,
                o.F4PoolTotal,
                o.F4DivAmount,
                o.QNPoolTotal,
                o.QNDivAmount,
                o.TFDivAmount,
                o.TFPoolTotal,
                o.WWDivAmount,
                o.WWPoolTotal,
                o.PPDivAmount,
                o.PPPoolTotal,
                o.RaceStatus));

            _dtOdds = dtOdds;
        }

    }
}
