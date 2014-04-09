﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;
using System.Data;

namespace Network_Simulation
{
    public abstract class Deployment
    {
        protected double percentageOfTunnelingTracer;
        protected double percentageOfMarkingTracer;
        protected double percentageOfFilteringTracer;
        protected List<NetworkTopology> allRoundScopeList;
        protected List<int> deployNodes;
        public DeploySQLiteUtility sqlite_utility;
        protected long jobID;
        protected bool isNeedWriteing2SQLite;

        public int numberOfTTracer;
        public int numberOfMTracer;
        public int numberOfFTracer;
        public List<int> DeployNodes
        {
            get
            {
                List<int> result = new List<int>(deployNodes);
                return result;
            }
        }

        public List<NetworkTopology> AllRoundScopeList 
        { 
            get 
            {
                List<NetworkTopology> result = new List<NetworkTopology>(allRoundScopeList);
                return result;
            } 
        }

        public List<int> MarkingTracerID;
        public List<int> FilteringTracerID;

        public Deployment(double percentageOfTunnelingTracer, double percentageOfMarkingTracer, double percentageOfFilteringTracer)
        {
            this.percentageOfTunnelingTracer = percentageOfTunnelingTracer;
            this.percentageOfMarkingTracer = percentageOfMarkingTracer;
            this.percentageOfFilteringTracer = percentageOfFilteringTracer;

            sqlite_utility = new DeploySQLiteUtility("deploy_simulation");
            sqlite_utility.CreateTable();

            allRoundScopeList = new List<NetworkTopology>();
            deployNodes = new List<int>();
        }

        public void Deploy(NetworkTopology networkTopology)
        {
            initialize(networkTopology);

            doDeploy(networkTopology);

            if (isNeedWriteing2SQLite)
                write2SQLite(networkTopology);
        }

        private void initialize(NetworkTopology networkTopology)
        {
            if (networkTopology.Nodes.Count == 0)
                throw new Exception("Initilaize() Fail: There are 0 nodes in the network.");

            numberOfTTracer = Convert.ToInt32(Math.Round(percentageOfTunnelingTracer * networkTopology.Nodes.Count / 100, 0, MidpointRounding.AwayFromZero));
            numberOfMTracer = Convert.ToInt32(Math.Round(percentageOfMarkingTracer * networkTopology.Nodes.Count / 100, 0, MidpointRounding.AwayFromZero));
            numberOfFTracer = Convert.ToInt32(Math.Round(percentageOfFilteringTracer * networkTopology.Nodes.Count / 100, 0, MidpointRounding.AwayFromZero));
            
            // Clear the deployment method.
            foreach (NetworkTopology.Node node in networkTopology.Nodes)
                node.Tracer = NetworkTopology.TracerType.None;

            jobID = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).Ticks;
            isNeedWriteing2SQLite = true;

            sqlite_utility.insertNetworkTopology(networkTopology);
        }

        protected abstract void doDeploy(NetworkTopology networkTopology);
        protected abstract void write2SQLite(NetworkTopology networkTopology);

        public class DeploySQLiteUtility
        {
            /// <summary>
            ///   Key: Table name.
            ///   Value: Schema.
            /// </summary>
            private Dictionary<string, string> tableDic = new Dictionary<string, string>()
            {
                {"NetworkTopology", "n_id INTEGER PRIMARY KEY AUTOINCREMENT, file_name TEXT UNIQUE, node_counts INTEGER, edge_counts INTEGER, diameter INTEGER"},
                {"DeploySimulation", "job_id INTEGER PRIMARY KEY, n_id INTEGER, k INTEGER, n INTEGER, deploy_name TEXT, FOREIGN KEY(n_id) REFERENCES NetworkTopology(n_id)"},
                {"LevelRecord", "l_id INTEGER PRIMARY KEY AUTOINCREMENT, job_id INTEGER, level INTEGER, node_id INTEGER, deploy_type TEXT, FOREIGN KEY(job_id) REFERENCES DeploySimulation(job_id)"},
            };

            private string baseDirectory = Path.Combine(Environment.CurrentDirectory, "Deploy");
            private string connectionString = @"Data Source=";
            private string fileName;

            /// <summary>
            /// SQLite constructor: create db file.
            /// </summary>
            /// <param name="dbFileName">The file name of database.</param>
            public DeploySQLiteUtility(string dbFileName)
            {
                try
                {
                    if (!Directory.Exists(baseDirectory))
                        Directory.CreateDirectory(baseDirectory);

                    fileName = Path.Combine(baseDirectory, dbFileName + "_0");

                    //for (int i = 1; File.Exists(fileName + ".db"); i++)
                    //    fileName = Path.Combine(baseDirectory, dbFileName + "_" + i);

                    fileName += ".db";
                    
                    if (!File.Exists(fileName))
                        SQLiteConnection.CreateFile(fileName);

                    connectionString += fileName + ";foreign keys=true;";
                }
                catch { }
            }

            /// <summary>
            /// Create the tables if not exist.
            /// </summary>
            /// <param name="prefixNameOfTable">The prefix-name of table, maybe method name.</param>
            public void CreateTable()
            {
                try
                {
                    // Create tables
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        SQLiteCommand cmd = connection.CreateCommand();
                        foreach (var kvp in tableDic)
                        {
                            cmd.CommandText = string.Format("CREATE TABLE IF NOT EXISTS {0}({1})", kvp.Key, kvp.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    if (ex.ErrorCode != SQLiteErrorCode.Constraint)
                        DataUtility.Log(ex.Message + "\n");
                }
            }

            public DataView GetResult(string sqlcmd, List<SQLiteParameter> parameters)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        DataSet ds = new DataSet();
                        SQLiteCommand cmd = connection.CreateCommand();

                        cmd.CommandText = sqlcmd;

                        if (parameters != null)
                            foreach (var item in parameters)
                                cmd.Parameters.Add(item);

                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(ds);

                        return ds.Tables.Count > 0 ? ds.Tables[0].DefaultView : null;
                    }
                }
                catch { return null; }
            }

            public void RunCommnad(string sqlcmd, List<SQLiteParameter> parameters = null)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        SQLiteCommand cmd = connection.CreateCommand();

                        cmd.CommandText = sqlcmd;

                        if (parameters != null)
                            foreach (var item in parameters)
                                cmd.Parameters.Add(item);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch(SQLiteException ex)
                {
                    if (ex.ErrorCode != SQLiteErrorCode.Constraint)
                        DataUtility.Log(ex.Message + "\n");
                }
            }

            public void insertNetworkTopology(NetworkTopology topo)
            {
                string cmd = "INSERT INTO NetworkTopology(file_name, node_counts, edge_counts, diameter) VALUES(@file_name, @node_counts, @edge_counts, @diameter)";
                List<SQLiteParameter> parms = new List<SQLiteParameter>();

                parms.Add(new SQLiteParameter("@file_name", topo.FileName));
                parms.Add(new SQLiteParameter("@node_counts", topo.Nodes.Count));
                parms.Add(new SQLiteParameter("@edge_counts", topo.Edges.Count));
                parms.Add(new SQLiteParameter("@diameter", topo.Diameter)); 

                RunCommnad(cmd, parms);
            }
        }
    }
}
