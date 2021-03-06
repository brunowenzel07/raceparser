﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMLFeedParser.Config;
using Dapper;
using System.Data;

namespace XMLFeedParser.Model
{
    static class DBGateway
    {

        internal static IEnumerable<UpcomingMeeting> GetUpcomingMeetings(string countryCode)
        {
            //return UpcomingMeeting.DummyList(countryCode);

            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();

                var q = string.Format(@"SELECT MeetingId, MeetingDate, NumberOfRaces, MeetingCode, AUS_StateId
                                        FROM getUpcomingMeetings('{0}')", countryCode);
                return conn.Query<UpcomingMeeting>(q);

            }
        }

        internal static Dictionary<int, string> GetTimeZones()
        {
            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();

                var q = string.Format(@"select AUS_State.Id as AUSStateId, CountryState_Timezone.Timezone as Code
                                        from CountryState_Timezone inner join AUS_State 
                                             on AUS_State.Code = CountryState_Timezone.Code");

                var timezones = new Dictionary<int, string>();
                conn.Query(q).ToList().ForEach(x => timezones.Add(x.AUSStateId, x.Code));
                return timezones;
            }
        }


        internal static IEnumerable<dynamic> GetJumpTimes(List<int> meetingIds)
        {
            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();

                var q = string.Format(@"select MeetingId, RaceNumber, LocalJumpTime 
                                        from Race 
                                        where race.MeetingId in ({0})",
                                        string.Join(",", meetingIds));

                return conn.Query(q);
            }
        }


        internal static void SetUpdateTime(List<RaceStatus> races)
        {
            var now = DateTime.UtcNow;

            var dtRaces = new DataTable();
            dtRaces.Columns.Add("MeetingId", typeof(int));
            dtRaces.Columns.Add("RaceNumber", typeof(short));
            dtRaces.Columns.Add("Refreshinterval", typeof(int));
            races.ToList().ForEach(r => dtRaces.Rows.Add(r.MeetingId, (short)r.RaceNumber, 
                r.IsDone ? -1 : (r.NextRefreshUTC - now).TotalSeconds));
            
            using (var conn = new SqlConnection(ConfigValues.ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = conn.CreateCommand())
                {
                    command.CommandText = "SetUpdateTime";
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1;
                    param1 = command.Parameters.AddWithValue("@races", dtRaces);
                    param1.SqlDbType = SqlDbType.Structured;
                    param1.TypeName = "RaceUpdateType";

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
