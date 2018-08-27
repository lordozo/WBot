using System;
using System.Collections.Generic;
using Bot.Core;
using Bot.Extensions.MySql;
using Bot.Extensions.Debug;
using System.Threading;
using Bot.Extensions.Stream;

namespace Bot.Modules.Points
{
    public class Points : CommandsModule
    {
        private Thread points_thread;
        public static PointsConfig pointsConfig;
        public Points(List<string> _ActiveChannels, IRC _irc) : base(_ActiveChannels, _irc)
        {
            pointsConfig = FileIO.ReadConfigJson(new PointsConfig());

            foreach( PointsConfig.Channel ch in pointsConfig.Channels ){
                addId("!"+ch.pointsName);
                addId("!"+ch.challengeName);
                addId("!"+ch.challengeAccept);
            }

            points_thread = new Thread(HandlePoints);
            points_thread.IsBackground = true;
            points_thread.Start();
            base.addId("!points");
            base.addId("!fight");
            base.addId("!accept");
            base.addId("!config pointsName:");
            base.addId("!config pointsNameMultiple:");
            base.addId("!config challengeName:");
            base.addId("!config challengeAccept:");
        }

        override public void HandleMessage(string channel, string msg, string sender)
        {
            // Get the index of a channel
            int channelIndex = pointsConfig.Channels.FindIndex(x => x.Name.Equals(channel));

            // Goes through every id
            for (int i = 0; i < base.getIds().Count; i++)
            {
                string id = base.getIds()[i];
                
                if (msg.StartsWith(id) && (sender.Equals(channel) || sender.Equals("lordozopl")))
                {
                    if (id.Equals("!config pointsName:"))
                    {
                        string s = msg.Replace("!config pointsName:","");
                        pointsConfig.Channels[channelIndex].pointsName = s;
                        FileIO.WriteConfigJson(pointsConfig);
                        addId("!"+s);
                        break;
                    }
                    else if (id.Equals("!config pointsNameMultiple:"))
                    {
                        string s = msg.Replace("!config pointsNameMultiple:","");
                        pointsConfig.Channels[channelIndex].pointsNameMultiple = s;
                        FileIO.WriteConfigJson(pointsConfig);
                        break;
                    }
                    else if (id.Equals("!config challengeName:"))
                    {
                        string s = msg.Replace("!config challengeName:","");
                        pointsConfig.Channels[channelIndex].challengeName = s;
                        FileIO.WriteConfigJson(pointsConfig);
                        addId("!"+s);
                        break;
                    }
                    else if (id.Equals("!config challengeAccept:"))
                    {
                        string s = msg.Replace("!config challengeAccept:","");
                        pointsConfig.Channels[channelIndex].challengeAccept = s;
                        FileIO.WriteConfigJson(pointsConfig);
                        addId("!"+s);
                        break;
                    }
                    //CODE
                    
                }
            }

            string handlepoints = "!" + pointsConfig.Channels[channelIndex].pointsName;
            string handlechallenge = "!" + pointsConfig.Channels[channelIndex].challengeName;
            string handleacceptchallenge = "!" + pointsConfig.Channels[channelIndex].challengeAccept;

            if(msg.StartsWith(handlepoints)) {
                PointsCommands.ShowPoints(channel,sender,base.irc);
            } else if (msg.StartsWith(handlechallenge)) {

            } else if (msg.StartsWith(handleacceptchallenge)) {

            }
        }

        override public bool AddToChannel(string channel)
        {
            for (int i = 0; i < ActiveChannels.Count; i++)
            {
                if (channel.Equals(ActiveChannels[i]))
                {
                    return false;
                }
            }
            ActiveChannels.Add(channel);

            PointsConfig.Channel ch = new PointsConfig.Channel();
            ch.Name = channel;
            ch.pointsName = "points";
            ch.challengeName = "fight";
            ch.challengeAccept = "accept";
            ch.pointsNameMultiple = "points";
            pointsConfig.Channels.Add(ch);
            string sb = string.Format("use VIEWERS; CREATE TABLE `{0}` (`Name` text COLLATE utf8mb4_unicode_ci,`Points` int(11) DEFAULT NULL,`TotalPoints` int(11) DEFAULT NULL,`Challenger` text COLLATE utf8mb4_unicode_ci) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", channel);
            MySqlWrapper.MakeQuery(sb);
            FileIO.WriteConfigJson(pointsConfig);
            return true;
        }
        public override bool RemoveFromChannel(string channel)
        {
            for (int i = 0; i < ActiveChannels.Count; i++)
            {
                if (channel.Equals(ActiveChannels[i]))
                {
                    ActiveChannels.Remove(channel);
                    pointsConfig.Channels.RemoveAt(pointsConfig.Channels.FindIndex(x => x.Name.Equals(channel)));
                    return true;
                }
            }
            return false;
        }
        static public void AddPointsIfOnChannel(string channel)
        {
            if (CheckStream.isRunning(channel))
            {
                List<string> v = Chatters.GetViewers(channel);
                if (v != null)
                {
                    for (int i = 0; i < v.Count; i++)
                    {
                        if (doesUserExist(channel, v[i]))
                        {
                            addPoints(channel, v[i], 1);
                        }
                    }
                }
            }
        }

        public static void addPoints(string channel, string name, int points)
        {
            int _points = getPoints(channel, name);
            int _totalPoints = getTotalPoints(channel, name);
            setPoints(channel, name, _points + points);
            setTotalPoints(channel, name, _totalPoints + points);
        }

        ///<summary>Removes some amount of points from user.</summary>
        ///<para name="channel">Channel's name.</para>
        ///<para name="name">Username.</para>
        ///<para name="points">Number of points to remove.</para>
        ///<returns>Returns true if points were removed else false.</returns>
        public static bool removePoints(string channel, string name, int points)
        {
            int _points = getPoints(channel, name);
            if(points >= _points) {
                if(setPoints(channel, name, 0))
                    return true;
                else 
                    return false;
            } else {
                if(setPoints(channel, name, _points - points))
                    return true;
                else 
                    return false;
            }
            
        }

        ///<summary>Checks if user exits in database.</summary>
        ///<para name="channel">Channel name</para>
        ///<para name="name">Username</para>
        ///<returns>Returns true if user exists in db or false if user does not exist..</returns>
        public static bool doesUserExist(string channel, string name)
        {
            string fill = string.Format("select Name from VIEWERS.{0} where Name = \"{1}\"", channel, name);
            List<string> query = MySqlWrapper.MakeQuery(fill, "Name");
            if (query.Count > 0)
            {
                string n = query[0];
                if (!name.Equals(""))
                {
                    //User exist
                    return true;
                }
                else
                {
                    //User does not exist
                    return false;
                }
            }
            else
            {
                //User does not exist
            }
            return false;
        }

        ///<summary>Adds user to database if not exists in db</summary>
        ///<para name="channel">Channel name</para>
        ///<para name="name">Username</para>
        ///<returns>Returns true if user is added to db or false is user already exists.</returns>
        public static bool addUserIfNotExist(string channel, string name) {
            if(!doesUserExist(channel, name)) {
                if(addUser(channel, name)) 
                    return true;
                else
                    return false;
            }
            else 
                return false;
        }
        ///<summary>Adds user to database</summary>
        ///<para name="channel">Channel name</para>
        ///<para name="name">Username</para>
        ///<returns>Returns true if user is added to db or false is user already exists.</returns>
        public static bool addUser(string channel, string name)
        {
            if(!doesUserExist(channel,name)) {
                string sb = string.Format("insert into VIEWERS.{0} (Name, Points, TotalPoints) values (\"{1}\", 0, 0)", channel, name);
                MySqlWrapper.MakeQuery(sb, "Points");
                return true;
            }
            else
                return false;

        }

        ///<summary>Use this to get points that user has.</summary>
        ///<para name="channel">Channel name</para>
        ///<para name="name">Username</para>
        ///<returns>Returns points that user has or -1 if something went wrong</returns>
        public static int getPoints(string channel, string name)
        {
            if (doesUserExist(channel, name))
            {

                string sb = string.Format("select Points from VIEWERS.{0} where Name = \"{1}\"", channel, name);
                List<string> query = MySqlWrapper.MakeQuery(sb, "Points");
                int i;
                if(Int32.TryParse(query[0], out i))
                    return i;
                else 
                    return -1;
            }
            return -1;
        }
        public static int getTotalPoints(string channel, string name)
        {
            if (doesUserExist(channel, name))
            {
                string sb = string.Format("select TotalPoints from VIEWERS.{0} where Name = \"{1}\"", channel, name);
                List<string> query = MySqlWrapper.MakeQuery(sb, "TotalPoints");
                int i;
                Int32.TryParse(query[0], out i);
                return i;
            }
            return -1;
        }

        public static bool setPoints(string channel, string name, int points)
        {
            if (doesUserExist(channel, name))
            {
                string sb = string.Format("UPDATE VIEWERS.{0} SET Points = {1} WHERE Name = \"{2}\"", channel, points, name);
                MySqlWrapper.MakeQuery(sb, "Points");
                return true;
            } else {
                return false;
            }
        }
        public static bool setTotalPoints(string channel, string name, int points)
        {
            if (doesUserExist(channel, name))
            {
                string sb = string.Format("UPDATE VIEWERS.{0} SET TotalPoints = {1} WHERE Name = \"{2}\"", channel, points, name);
                MySqlWrapper.MakeQuery(sb, "TotalPoints");
                return true;
            } 
            else
                return false;
        }
        private void HandlePoints()
        {
            while (true)
            {
                for (int i = 0; i < getActiveChannels().Count; i++)
                    AddPointsIfOnChannel(getActiveChannels()[i]);
                Thread.Sleep(30000);
            }
        }
    }
}