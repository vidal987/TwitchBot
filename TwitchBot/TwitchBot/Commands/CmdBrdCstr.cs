﻿using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using TwitchBot.Configuration;

namespace TwitchBot
{
    public class CmdBrdCstr
    {
        private IrcClient _irc;
        private Moderator _modInstance = Moderator.Instance;
        private System.Configuration.Configuration _appConfig;
        private TwitchBotConfigurationSection _botConfig;
        private string _connStr;
        private int _intBroadcasterID;
        private ErrorHandler _errHndlrInstance = ErrorHandler.Instance;

        public CmdBrdCstr(IrcClient irc, TwitchBotConfigurationSection botConfig, string connString, int broadcasterId, System.Configuration.Configuration appConfig)
        {
            _irc = irc;
            _botConfig = botConfig;
            _connStr = connString;
            _intBroadcasterID = broadcasterId;
            _appConfig = appConfig;
        }

        /// <summary>
        /// Display bot settings
        /// </summary>
        public void CmdBotSettings()
        {
            try
            {
                _irc.sendPublicChatMessage("Auto tweets set to \"" + _botConfig.EnableTweets + "\" "
                    + ">< Auto display songs set to \"" + _botConfig.EnableDisplaySong + "\" "
                    + ">< Currency set to \"" + _botConfig.CurrencyType + "\" "
                    + ">< Stream Latency set to \"" + _botConfig.StreamLatency + " second(s)\"");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdBotSettings()", false, "!botsettings");
            }
        }

        /// <summary>
        /// Stop running the bot
        /// </summary>
        public void CmdExitBot()
        {
            try
            {
                _irc.sendPublicChatMessage("Bye! Have a beautiful time!");
                Environment.Exit(0); // exit program
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdExitBot()", false, "!exitbot");
            }
        }

        /// <summary>
        /// Enables tweets to be sent out from this bot (both auto publish tweets and manual tweets)
        /// </summary>
        /// <param name="bolHasTwitterInfo">Check for Twitter credentials</param>
        public void CmdEnableTweet(bool bolHasTwitterInfo)
        {
            try
            {
                if (!bolHasTwitterInfo)
                    _irc.sendPublicChatMessage("You are missing twitter info @" + _botConfig.Broadcaster);
                else
                {
                    _botConfig.EnableTweets = true;
                    _appConfig.Save();

                    Console.WriteLine("Auto publish tweets is set to [" + _botConfig.EnableTweets + "]");
                    _irc.sendPublicChatMessage(_botConfig.Broadcaster + ": Automatic tweets is set to \"" + _botConfig.EnableTweets + "\"");
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdEnableTweet(bool)", false, "!sendtweet on");
            }
        }

        /// <summary>
        /// Disables tweets to be sent out from this bot (both auto publish tweets and manual tweets)
        /// </summary>
        /// <param name="bolHasTwitterInfo">Check for Twitter credentials</param>
        public void CmdDisableTweet(bool bolHasTwitterInfo)
        {
            try
            {
                if (!bolHasTwitterInfo)
                    _irc.sendPublicChatMessage("You are missing twitter info @" + _botConfig.Broadcaster);
                else
                {
                    _botConfig.EnableTweets = false;
                    _appConfig.Save();

                    Console.WriteLine("Auto publish tweets is set to [" + _botConfig.EnableTweets + "]");
                    _irc.sendPublicChatMessage(_botConfig.Broadcaster + ": Automatic tweets is set to \"" + _botConfig.EnableTweets + "\"");
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdDisableTweet(bool)", false, "!sendtweet off");
            }
        }

        /// <summary>
        /// Enable song request mode
        /// </summary>
        /// <param name="isSongRequestAvail">Set song request mode</param>
        public void CmdEnableSRMode(ref bool isSongRequestAvail)
        {
            try
            {
                isSongRequestAvail = true;
                _irc.sendPublicChatMessage("Song requests enabled");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdEnableSRMode(ref bool)", false, "!srmode on");
            }
        }

        /// <summary>
        /// Disable song request mode
        /// </summary>
        /// <param name="isSongRequestAvail">Set song request mode</param>
        public void CmdDisableSRMode(ref bool isSongRequestAvail)
        {
            try
            {
                isSongRequestAvail = false;
                _irc.sendPublicChatMessage("Song requests disabled");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdDisableSRMode(ref bool)", false, "!srmode off");
            }
        }

        /// <summary>
        /// Update the title of the Twitch channel
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="twitchAccessToken">Token needed to change channel info</param>
        public void CmdUpdateTitle(string message, string twitchAccessToken)
        {
            try
            {
                // Get title from command parameter
                string title = message.Substring(message.IndexOf(" ") + 1);

                // Send HTTP method PUT to base URI in order to change the title
                RestClient client = new RestClient("https://api.twitch.tv/kraken/channels/" + _botConfig.Broadcaster);
                RestRequest request = new RestRequest(Method.PUT);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/json");
                request.AddHeader("authorization", "OAuth " + twitchAccessToken);
                request.AddHeader("accept", "application/vnd.twitchtv.v3+json");
                request.AddParameter("application/json", "{\"channel\":{\"status\":\"" + title + "\"}}",
                    ParameterType.RequestBody);

                IRestResponse response = null;
                try
                {
                    response = client.Execute(request);
                    string statResponse = response.StatusCode.ToString();
                    if (statResponse.Contains("OK"))
                    {
                        _irc.sendPublicChatMessage("Twitch channel title updated to \"" + title +
                            "\" >< Refresh your browser [F5] or twitch app to see the change");
                    }
                    else
                        Console.WriteLine(response.ErrorMessage);
                }
                catch (WebException ex)
                {
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.BadRequest)
                    {
                        Console.WriteLine("Error 400 detected!");
                    }
                    response = (IRestResponse)ex.Response;
                    Console.WriteLine("Error: " + response);
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdUpdateTitle(string, string, string)", false, "!updatetitle");
            }
        }

        /// <summary>
        /// Updates the game being played on the Twitch channel
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="twitchAccessToken">Token needed to change channel info</param>
        /// <param name="bolHasTwitterInfo">Check for Twitter credentials</param>
        public void CmdUpdateGame(string message, string twitchAccessToken, bool bolHasTwitterInfo)
        {
            try
            {
                // Get game from command parameter
                string game = message.Substring(message.IndexOf(" ") + 1);

                // Send HTTP method PUT to base URI in order to change the game
                RestClient client = new RestClient("https://api.twitch.tv/kraken/channels/" + _botConfig.Broadcaster);
                RestRequest request = new RestRequest(Method.PUT);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/json");
                request.AddHeader("authorization", "OAuth " + twitchAccessToken);
                request.AddHeader("accept", "application/vnd.twitchtv.v3+json");
                request.AddParameter("application/json", "{\"channel\":{\"game\":\"" + game + "\"}}",
                    ParameterType.RequestBody);

                IRestResponse response = null;
                try
                {
                    response = client.Execute(request);
                    string statResponse = response.StatusCode.ToString();
                    if (statResponse.Contains("OK"))
                    {
                        _irc.sendPublicChatMessage("Twitch channel game status updated to \"" + game +
                            "\" >< Restart your connection to the stream or twitch app to see the change");
                        if (_botConfig.EnableTweets && bolHasTwitterInfo)
                        {
                            SendTweet("Watch me stream " + game + " on Twitch" + Environment.NewLine
                                + "http://goo.gl/SNyDFD" + Environment.NewLine
                                + "#twitch #gaming #streaming", message);
                        }
                    }
                    else
                        Console.WriteLine(response.ErrorMessage);
                }
                catch (WebException ex)
                {
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.BadRequest)
                    {
                        Console.WriteLine("Error 400 detected!!");
                    }
                    response = (IRestResponse)ex.Response;
                    Console.WriteLine("Error: " + response);
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdUpdateGame(string, string, string, bool, bool)", false, "!updategame");
            }
        }

        /// <summary>
        /// Manually send a tweet
        /// </summary>
        /// <param name="bolHasTwitterInfo">Check if user has provided the specific twitter credentials</param>
        /// <param name="message">Chat message from the user</param>
        public void CmdTweet(bool bolHasTwitterInfo, string message)
        {
            try
            {
                if (!bolHasTwitterInfo)
                    _irc.sendPublicChatMessage("You are missing twitter info @" + _botConfig.Broadcaster);
                else
                {
                    string command = message;
                    SendTweet(message, command);
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdTweet(bool, string, string)", false, "!tweet");
            }
        }

        /// <summary>
        /// Enables displaying songs from Spotify into the IRC chat
        /// </summary>
        public void CmdEnableDisplaySongs()
        {
            try
            {
                _botConfig.EnableDisplaySong = true;
                _appConfig.Save();

                Console.WriteLine("Auto display songs is set to [" + _botConfig.EnableDisplaySong + "]");
                _irc.sendPublicChatMessage(_botConfig.Broadcaster + ": Automatic display Spotify songs is set to \"" + _botConfig.EnableDisplaySong + "\"");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdEnableDisplaySongs()", false, "!displaysongs on");
            }
        }

        /// <summary>
        /// Disables displaying songs from Spotify into the IRC chat
        /// </summary>
        public void CmdDisableDisplaySongs()
        {
            try
            {
                _botConfig.EnableDisplaySong = false;
                _appConfig.Save();

                Console.WriteLine("Auto display songs is set to [" + _botConfig.EnableDisplaySong + "]");
                _irc.sendPublicChatMessage(_botConfig.Broadcaster + ": Automatic display Spotify songs is set to \"" + _botConfig.EnableDisplaySong + "\"");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdDisableDisplaySongs()", false, "!displaysongs off");
            }
        }

        /// <summary>
        /// Grant viewer to moderator status for this bot's mod commands
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        public void CmdAddBotMod(string message)
        {
            try
            {
                string strRecipient = message.Substring(message.IndexOf("@") + 1); // grab user from message
                _modInstance.addNewModToLst(strRecipient.ToLower(), _intBroadcasterID, _connStr); // add user to mod list and add to db
                _irc.sendPublicChatMessage("@" + strRecipient + " is now able to use moderator features within " + _botConfig.BotName);
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdAddBotMod(string)", false, "!addmod");
            }
        }

        /// <summary>
        /// Revoke moderator status from user for this bot's mods commands
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        public void CmdDelBotMod(string message)
        {
            try
            {
                string strRecipient = message.Substring(message.IndexOf("@") + 1); // grab user from message
                _modInstance.delOldModFromLst(strRecipient.ToLower(), _intBroadcasterID, _connStr); // delete user from mod list and remove from db
                _irc.sendPublicChatMessage("@" + strRecipient + " is not able to use moderator features within " + _botConfig.BotName + " any longer");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdDelBotMod(string)", false, "!delmod");
            }
        }

        /// <summary>
        /// List bot moderators
        /// </summary>
        public void CmdListMod()
        {
            try
            {
                string strListModMsg = "";

                if (_modInstance.LstMod.Count > 0)
                {
                    foreach (string name in _modInstance.LstMod)
                        strListModMsg += name + " >< ";

                    strListModMsg = strListModMsg.Remove(strListModMsg.Length - 3); // removed extra " >< "
                    _irc.sendPublicChatMessage("List of bot moderators (separate from channel mods): " + strListModMsg);
                }
                else
                    _irc.sendPublicChatMessage("No one is ruling over me other than you @" + _botConfig.Broadcaster);
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdListMod()", false, "!listmod");
            }
        }

        /// <summary>
        /// Add a custom countdown for a user to post in the chat
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="strUserName">User that sent the message</param>
        public void CmdAddCountdown(string message, string strUserName)
        {
            try
            {
                // get due date of countdown
                string strCountdownDT = message.Substring(14, 20); // MM-DD-YY hh:mm:ss [AM/PM]
                DateTime dtCountdown = Convert.ToDateTime(strCountdownDT);

                // get message of countdown
                string strCountdownMsg = message.Substring(34);

                // log new countdown into db
                string query = "INSERT INTO tblCountdown (dueDate, message, broadcaster) VALUES (@dueDate, @message, @broadcaster)";

                using (SqlConnection conn = new SqlConnection(_connStr))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@dueDate", SqlDbType.DateTime).Value = dtCountdown;
                    cmd.Parameters.Add("@message", SqlDbType.VarChar, 50).Value = strCountdownMsg;
                    cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine("Countdown added!");
                _irc.sendPublicChatMessage($"Countdown added @{strUserName}");
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdAddCountdown(string, string)", false, "!addcountdown");
            }
        }

        /// <summary>
        /// Edit countdown details (for either date and time or message)
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="strUserName">User that sent the message</param>
        public void CmdEditCountdown(string message, string strUserName)
        {
            try
            {
                int intReqCountdownID = -1;
                string strReqCountdownID = message.Substring(18, Program.GetNthIndex(message, ' ', 2) - Program.GetNthIndex(message, ' ', 1) - 1);
                bool bolValidCountdownID = int.TryParse(strReqCountdownID, out intReqCountdownID);

                // validate requested countdown ID
                if (!bolValidCountdownID || intReqCountdownID < 0)
                    _irc.sendPublicChatMessage("Please use a positive whole number to find your countdown ID");
                else
                {
                    // check if countdown ID exists
                    int intCountdownID = -1;
                    using (SqlConnection conn = new SqlConnection(_connStr))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT id, broadcaster FROM tblCountdown "
                            + "WHERE broadcaster = @broadcaster", conn))
                        {
                            cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        if (intReqCountdownID.ToString().Equals(reader["id"].ToString()))
                                        {
                                            intCountdownID = int.Parse(reader["id"].ToString());
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // check if countdown ID was retrieved
                    if (intCountdownID == -1)
                        _irc.sendPublicChatMessage($"Cannot find the countdown ID: {intReqCountdownID}");
                    else
                    {
                        int intInputType = -1; // check if input is in the correct format
                        DateTime dtCountdown = new DateTime();
                        string strCountdownInput = message.Substring(Program.GetNthIndex(message, ' ', 2) + 1);

                        /* Check if user wants to edit the date and time or message */
                        if (message.StartsWith("!editcountdownDTE"))
                        {
                            // get new due date of countdown
                            bool bolValidCountdownDT = DateTime.TryParse(strCountdownInput, out dtCountdown);

                            if (!bolValidCountdownDT)
                                _irc.sendPublicChatMessage("Please enter a valid date and time @" + strUserName);
                            else
                                intInputType = 1;
                        }
                        else if (message.StartsWith("!editcountdownMSG"))
                        {
                            // get new message of countdown
                            if (string.IsNullOrWhiteSpace(strCountdownInput))
                                _irc.sendPublicChatMessage("Please enter a valid message @" + strUserName);
                            else
                                intInputType = 2;
                        }

                        // if input is correct update db
                        if (intInputType > 0)
                        {
                            string strQuery = "";

                            if (intInputType == 1)
                                strQuery = "UPDATE dbo.tblCountdown SET dueDate = @dueDate WHERE (Id = @id AND broadcaster = @broadcaster)";
                            else if (intInputType == 2)
                                strQuery = "UPDATE dbo.tblCountdown SET message = @message WHERE (Id = @id AND broadcaster = @broadcaster)";

                            using (SqlConnection conn = new SqlConnection(_connStr))
                            using (SqlCommand cmd = new SqlCommand(strQuery, conn))
                            {
                                // append proper parameter
                                if (intInputType == 1)
                                    cmd.Parameters.Add("@dueDate", SqlDbType.DateTime).Value = dtCountdown;
                                else if (intInputType == 2)
                                    cmd.Parameters.Add("@message", SqlDbType.VarChar, 50).Value = strCountdownInput;

                                cmd.Parameters.Add("@id", SqlDbType.Int).Value = intCountdownID;
                                cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;

                                conn.Open();
                                cmd.ExecuteNonQuery();
                            }

                            Console.WriteLine($"Changes to countdown ID: {intReqCountdownID} have been made @{strUserName}");
                            _irc.sendPublicChatMessage($"Changes to countdown ID: {intReqCountdownID} have been made @{strUserName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdEditCountdown(string, string)", false, "!editcountdown");
            }
        }

        /// <summary>
        /// List all of the countdowns the broadcaster has set
        /// </summary>
        /// <param name="strUserName">User that sent the message</param>
        public void CmdListCountdown(string strUserName)
        {
            try
            {
                string strCountdownList = "";

                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, dueDate, message, broadcaster FROM tblCountdown "
                        + "WHERE broadcaster = @broadcaster ORDER BY Id", conn))
                    {
                        cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    strCountdownList += "ID: " + reader["Id"].ToString()
                                        + " Message: \"" + reader["message"].ToString()
                                        + "\" Time: \"" + reader["dueDate"].ToString()
                                        + "\" // ";
                                }
                                StringBuilder strBdrPartyList = new StringBuilder(strCountdownList);
                                strBdrPartyList.Remove(strCountdownList.Length - 4, 4); // remove extra " >< "
                                strCountdownList = strBdrPartyList.ToString(); // replace old countdown list string with new
                                _irc.sendPublicChatMessage(strCountdownList);
                            }
                            else
                            {
                                Console.WriteLine("No countdown messages are set at the moment");
                                _irc.sendPublicChatMessage("No countdown messages are set at the moment @" + strUserName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdListCountdown()", false, "!listcountdown");
            }
        }

        /// <summary>
        /// Add a giveaway for a user to post in the chat
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="strUserName">User that sent the message</param>
        public void CmdAddGiveaway(string message, string strUserName)
        {
            try
            {
                // get due date of giveaway
                string strGiveawayDT = message.Substring(13, 20); // MM-DD-YY hh:mm:ss [AM/PM]
                DateTime dtGiveaway = Convert.ToDateTime(strGiveawayDT);

                // get eligibility parameters for user types (using boolean bits)
                string strGiveawayElg = message.Substring(34, 7); // [mods] [regulars] [subscribers] [users]
                if (strGiveawayElg.Replace(" ", "").IsInt()
                    && strGiveawayElg.Replace(" ", "").Length == 4
                    && !Regex.IsMatch(strGiveawayElg, @"[2-9]"))
                {
                    int[] strArrElg =
                    {
                        int.Parse(message.Substring(34, 1)),
                        int.Parse(message.Substring(36, 1)),
                        int.Parse(message.Substring(38, 1)),
                        int.Parse(message.Substring(40, 1))
                    };

                    // get giveaway type (1 = Keyword, 2 = Random Number)
                    int intGiveawayType = int.Parse(message.Substring(42, 1));

                    // get parameter of new giveaway (1 = [keyword], 2 = [min]-[max])
                    int intParamMsgIndex = Program.GetNthIndex(message, ' ', 10); // get the index of the space separating the message and the parameter
                    string strGiveawayParam = message.Substring(44, intParamMsgIndex - 44);

                    // get message of new giveaway
                    string strGiveawayMsg = message.Substring(intParamMsgIndex + 1);

                    // log new giveaway into db
                    string query = "INSERT INTO tblGiveaway (dueDate, message, broadcaster, elgMod, elgReg, elgSub, elgUsr, giveType, giveParam1, giveParam2) " +
                                   "VALUES (@dueDate, @message, @broadcaster, @elgMod, @elgReg, @elgSub, @elgUsr, @giveType, @giveParam1, @giveParam2)";

                    using (SqlConnection conn = new SqlConnection(_connStr))
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@dueDate", SqlDbType.DateTime).Value = dtGiveaway;
                        cmd.Parameters.Add("@message", SqlDbType.VarChar, 75).Value = strGiveawayMsg;
                        cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;
                        cmd.Parameters.Add("@elgMod", SqlDbType.Bit).Value = strArrElg[0];
                        cmd.Parameters.Add("@elgReg", SqlDbType.Bit).Value = strArrElg[1];
                        cmd.Parameters.Add("@elgSub", SqlDbType.Bit).Value = strArrElg[2];
                        cmd.Parameters.Add("@elgUsr", SqlDbType.Bit).Value = strArrElg[3];
                        cmd.Parameters.Add("@giveType", SqlDbType.Int).Value = intGiveawayType;

                        if (intGiveawayType == 1) // keyword
                        {
                            cmd.Parameters.Add("@giveParam1", SqlDbType.VarChar, 50).Value = strGiveawayParam;
                            cmd.Parameters.Add("@giveParam2", SqlDbType.VarChar, 50).Value = DBNull.Value;
                        }
                        else if (intGiveawayType == 2) // random number
                        {
                            int intDashIndex = strGiveawayParam.IndexOf('-');

                            cmd.Parameters.Add("@giveParam1", SqlDbType.VarChar, 50).Value = strGiveawayParam.Substring(0, intDashIndex); // min
                            cmd.Parameters.Add("@giveParam2", SqlDbType.VarChar, 50).Value = strGiveawayParam.Substring(intDashIndex + 1); // max
                        }

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("Giveaway started!");
                    _irc.sendPublicChatMessage($"Giveaway \"{strGiveawayMsg}\" has started @{strUserName}");
                }
                else
                {
                    _irc.sendPublicChatMessage($"Eligibility parameters were not given correctly @{strUserName}");
                }
            }
            catch (Exception ex)
            {
                _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdAddGiveaway(string, string)", false, "!addgiveaway");
            }
        }

        /// <summary>
        /// Edit giveaway details (for either date and time or message)
        /// </summary>
        /// <param name="message">Chat message from the user</param>
        /// <param name="strUserName">User that sent the message</param>
        //public void CmdEditGiveaway(string message, string strUserName)
        //{
        //    try
        //    {
        //        int intReqCountdownID = -1;
        //        string strReqCountdownID = message.Substring(18, Program.GetNthIndex(message, ' ', 2) - Program.GetNthIndex(message, ' ', 1) - 1);
        //        bool bolValidCountdownID = int.TryParse(strReqCountdownID, out intReqCountdownID);

        //        // validate requested countdown ID
        //        if (!bolValidCountdownID || intReqCountdownID < 0)
        //            _irc.sendPublicChatMessage("Please use a positive whole number to find your countdown ID");
        //        else
        //        {
        //            // check if countdown ID exists
        //            int intCountdownID = -1;
        //            using (SqlConnection conn = new SqlConnection(_connStr))
        //            {
        //                conn.Open();
        //                using (SqlCommand cmd = new SqlCommand("SELECT id, broadcaster FROM tblCountdown "
        //                    + "WHERE broadcaster = @broadcaster", conn))
        //                {
        //                    cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;
        //                    using (SqlDataReader reader = cmd.ExecuteReader())
        //                    {
        //                        if (reader.HasRows)
        //                        {
        //                            while (reader.Read())
        //                            {
        //                                if (intReqCountdownID.ToString().Equals(reader["id"].ToString()))
        //                                {
        //                                    intCountdownID = int.Parse(reader["id"].ToString());
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //            // check if countdown ID was retrieved
        //            if (intCountdownID == -1)
        //                _irc.sendPublicChatMessage($"Cannot find the countdown ID: {intReqCountdownID}");
        //            else
        //            {
        //                int intInputType = -1; // check if input is in the correct format
        //                DateTime dtCountdown = new DateTime();
        //                string strCountdownInput = message.Substring(Program.GetNthIndex(message, ' ', 2) + 1);

        //                /* Check if user wants to edit the date and time or message */
        //                if (message.StartsWith("!editgiveawayDTE"))
        //                {
        //                    // get new due date of countdown
        //                    bool bolValidCountdownDT = DateTime.TryParse(strCountdownInput, out dtCountdown);

        //                    if (!bolValidCountdownDT)
        //                        _irc.sendPublicChatMessage("Please enter a valid date and time @" + strUserName);
        //                    else
        //                        intInputType = 1;
        //                }
        //                else if (message.StartsWith("!editgiveawayMSG"))
        //                {
        //                    // get new message of countdown
        //                    if (string.IsNullOrWhiteSpace(strCountdownInput))
        //                        _irc.sendPublicChatMessage("Please enter a valid message @" + strUserName);
        //                    else
        //                        intInputType = 2;
        //                }

        //                // if input is correct update db
        //                if (intInputType > 0)
        //                {
        //                    string strQuery = "";

        //                    if (intInputType == 1)
        //                        strQuery = "UPDATE dbo.tblCountdown SET dueDate = @dueDate WHERE (Id = @id AND broadcaster = @broadcaster)";
        //                    else if (intInputType == 2)
        //                        strQuery = "UPDATE dbo.tblCountdown SET message = @message WHERE (Id = @id AND broadcaster = @broadcaster)";

        //                    using (SqlConnection conn = new SqlConnection(_connStr))
        //                    using (SqlCommand cmd = new SqlCommand(strQuery, conn))
        //                    {
        //                        // append proper parameter
        //                        if (intInputType == 1)
        //                            cmd.Parameters.Add("@dueDate", SqlDbType.DateTime).Value = dtCountdown;
        //                        else if (intInputType == 2)
        //                            cmd.Parameters.Add("@message", SqlDbType.VarChar, 50).Value = strCountdownInput;

        //                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = intCountdownID;
        //                        cmd.Parameters.Add("@broadcaster", SqlDbType.Int).Value = _intBroadcasterID;

        //                        conn.Open();
        //                        cmd.ExecuteNonQuery();
        //                    }

        //                    Console.WriteLine($"Changes to countdown ID: {intReqCountdownID} have been made @{strUserName}");
        //                    _irc.sendPublicChatMessage($"Changes to countdown ID: {intReqCountdownID} have been made @{strUserName}");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _errHndlrInstance.LogError(ex, "CmdBrdCstr", "CmdEditCountdown(string, string)", false, "!editcountdown");
        //    }
        //}

        public void SendTweet(string pendingMessage, string command)
        {
            // Check if there are at least two quotation marks before sending message using LINQ
            string resultMessage = "";
            if (command.Count(c => c == '"') < 2)
            {
                resultMessage = "Please use at least two quotation marks (\") before sending a tweet. " +
                    "Quotations are used to find the start and end of a message wanting to be sent";
                Console.WriteLine(resultMessage);
                _irc.sendPublicChatMessage(resultMessage);
                return;
            }

            // Get message from quotation parameter
            string tweetMessage = string.Empty;
            int length = (pendingMessage.LastIndexOf('"') - pendingMessage.IndexOf('"')) - 1;
            if (length == -1) // if no quotations were found
                length = pendingMessage.Length;
            int startIndex = pendingMessage.IndexOf('"') + 1;
            tweetMessage = pendingMessage.Substring(startIndex, length);

            // Check if message length is at or under 140 characters
            var basicTweet = new object();

            if (tweetMessage.Length <= 140)
            {
                basicTweet = Tweet.PublishTweet(tweetMessage);
                resultMessage = "Tweet successfully published!";
                Console.WriteLine(resultMessage);
                _irc.sendPublicChatMessage(resultMessage);
            }
            else
            {
                int overCharLimit = tweetMessage.Length - 140;
                resultMessage = "The message you attempted to tweet had " + overCharLimit +
                    " characters more than the 140 character limit. Please shorten your message and try again";
                Console.WriteLine(resultMessage);
                _irc.sendPublicChatMessage(resultMessage);
            }
        }
    }
}