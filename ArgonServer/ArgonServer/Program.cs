using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;

class ArgonServer
{
    static SqlConnection myConnection = new SqlConnection("user id=Client;" +
                          "password=ArgonServer4097;server=localhost;" +
                          "Integrated Security=True;" +
                          "database=Argon; " +
                          "connection timeout=1");
    class player
    {
        public int xLoc;
        public int yLoc;

        public string username;

        public bool playing = false;

        public player(int xLoc, int yLoc, string username)
        {
            this.xLoc = xLoc;
            this.yLoc = yLoc;
            this.username = username;
        }
        public player(string input)
        {
            char[] delim = { ' ' };
            string[] parts = input.Split(delim);

            xLoc = int.Parse(parts[0]);
            yLoc = int.Parse(parts[1]);
            username = parts[2];
        }

        public override string ToString()
        {
            return xLoc.ToString() + " " + yLoc.ToString() + " " + username;
        }
    }

    static List<player> activePlayers = new List<player>();

    static TcpListener listener;
    const int LIMIT = 10; //10 concurrent clients

    public static void Main()
    {
        myConnection.Open();
        Console.WriteLine("Connection To SQL Established");
        listener = new TcpListener(IPAddress.Any, 2055);
        listener.Start();
        #if LOG
            Console.WriteLine("Server mounted, 
                            listening to port 2055");
        #endif
        for (int i = 0; i < LIMIT; i++)
        {
            Thread t = new Thread(new ThreadStart(Service));
            t.Start();
        }
    }

    private static bool AttemptLogin(string username, string password, SqlConnection myConnection, out player newPlayer)
    {
        foreach(player p in activePlayers)
        {
            if(p.username == username)
            {
                newPlayer = null;
                return false;
            }
        }
        SqlCommand cmd = new SqlCommand();
        cmd.Connection = myConnection;
        cmd.CommandText = "SELECT dbo.CheckLoginCredentials('" + username + "','" + password + "')";
        SqlDataReader rdr = cmd.ExecuteReader();

        if ((int)rdr[0] != 0)//login was successful, 0 return from sql
        {
            int ID = (int)rdr[0];
            rdr.Close();
            cmd = new SqlCommand();
            cmd.Connection = myConnection;
            cmd.CommandText = "SELECT * FROM dbo.GetLocation(" + ID + ")";
            rdr = cmd.ExecuteReader();
            rdr.Read();
            newPlayer = new player((int)rdr["X"], (int)rdr["Y"], username);
            activePlayers.Add(newPlayer);
            rdr.Dispose();
            return true;
        }
        else
        {
            newPlayer = null;
            Console.WriteLine("Login Failed");
            System.Threading.Thread.Sleep(1000);
            rdr.Dispose();
            return false;
        }
    }

    private static void attemptSaveToDB(player p)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.Connection = myConnection;
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.CommandText = "UpdateAccountLocation";
        cmd.Parameters.Add(new SqlParameter("@username", p.username));
        cmd.Parameters.Add(new SqlParameter("@xLoc", p.xLoc));
        cmd.Parameters.Add(new SqlParameter("@yLoc", p.yLoc));
        cmd.ExecuteNonQuery();
    }

    static int playLoops = 0;
    public static void Service()
    { 
        player myPlayer = null;
        bool login = false;
        bool playing = false;
        while (true)
        {
            Socket soc = listener.AcceptSocket();
            Console.WriteLine("Connection Attempted");
            #if LOG
                Console.WriteLine("Connected: {0}", 
                                         soc.RemoteEndPoint);
            #endif
            try
            {
                Stream s = new NetworkStream(soc);
                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);
                sw.AutoFlush = true; // enable automatic flushing
                sw.WriteLine("Connection Success".Crypt(),
                      ConfigurationSettings.AppSettings.Count);
                while (true)
                {
                    string input = sr.ReadLine().Decrypt();

                    if (input == "1")
                    {
                        Console.WriteLine("Login Attempt Initiated");
                        string username = sr.ReadLine().Decrypt();
                        string password = sr.ReadLine().Decrypt();
                        if (AttemptLogin(username, password, myConnection, out myPlayer))
                        {
                            Console.WriteLine("Success");
                            login = true;
                            sw.WriteLine("Login Success".Crypt());
                        }
                        else
                        {
                            Console.WriteLine("Failed");
                            sw.WriteLine("Login Failed".Crypt());
                        }
                    }

                    else if(input == "2")
                    {
                        Console.WriteLine("Account Creation Attempt Initiated");
                        string username = sr.ReadLine().Decrypt();
                        string password = sr.ReadLine().Decrypt();
                        if (AttemptCreateAccount(username, password))
                        {
                            Console.WriteLine("Success");
                            sw.WriteLine("Create Success".Crypt());
                        }
                        else
                        {
                            Console.WriteLine("Failed");
                            sw.WriteLine("Create Failed".Crypt());
                        }
                    }

                    else if(input == "3")
                    {
                        listener.Server.Disconnect(true);
                        if (login && myPlayer != null)
                            activePlayers.Remove(myPlayer);
                        login = false;
                        myPlayer.playing = false;
                    }
                    else if(input == "4" && login)
                    {
                        Console.WriteLine("Login");
                        myPlayer.playing = true;
                        sw.WriteLine("Playing".Crypt());
                    }
                    else if(input == "4")
                    {
                        Console.WriteLine("Invalid Login");
                        sw.WriteLine("Not logged into an account.".Crypt());
                    }
                    else if(input == "5")
                    {
                        if (login)
                        {
                            if (myPlayer != null)
                            {
                                attemptSaveToDB(myPlayer);
                                activePlayers.Remove(myPlayer);
                            }
                            login = false;
                            myPlayer.playing = false;
                            sw.WriteLine("Logout Successful".Crypt());
                        }
                        else
                            sw.WriteLine("User is not logged into an account.".Crypt());
                    }

                    else if(login)
                    {
                        if(myPlayer.playing)
                        {
                            if (input == "myPlayer")
                            {
                                playLoops++;
                                sw.WriteLine(myPlayer.ToString().Crypt());
                            }
                            else if (input == "q")
                            {
                                Console.WriteLine(playLoops.ToString());
                                Console.WriteLine("Stopping play.");
                                attemptSaveToDB(myPlayer);
                                myPlayer.playing = false;
                            }
                            else if (input == "w")
                                myPlayer.yLoc--;
                            else if (input == "a")
                                myPlayer.xLoc--;
                            else if (input == "s")
                                myPlayer.yLoc++;
                            else if (input == "d")
                                myPlayer.xLoc++;
                            else if(input == "GetActive")
                            {
                                getAllPlayers(sw, myPlayer); 
                            }
                        }
                    }
                }
                s.Close();
            }
            catch (Exception e)
            {
                if (login)
                {
                    if (myPlayer != null)
                    {
                        myPlayer.playing = false;
                        attemptSaveToDB(myPlayer);
                        activePlayers.Remove(myPlayer);
                    }
                    login = false;
                    
                }
                Console.WriteLine(e.Message);
            }
            #if LOG
                Console.WriteLine("Disconnected: {0}", 
                                         soc.RemoteEndPoint);
            #endif
            soc.Close();
        }
    }
    private static void getAllPlayers(StreamWriter sw, player myPlayer)
    {
        //make my player the first player in the list
        string output = myPlayer.ToString().Crypt() + "\n";
        foreach (player p in activePlayers)
        {
            if (p.playing && p != myPlayer)
            {
                //add all other active playing players that are not
                //myPlyer to the list
                output += p.ToString().Crypt() + "\n";
            }
        }
        //send all players to the client
        sw.Write(output);
    }
    private static bool AttemptCreateAccount(string username, string password)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.Connection = myConnection;
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.CommandText = "CreateAccount";
        cmd.Parameters.Add(new SqlParameter("@username", username));
        cmd.Parameters.Add(new SqlParameter("@password", password));

        if (cmd.ExecuteNonQuery() >= 1)
        {
            return true;
        }
        else
            return false;
    }
}