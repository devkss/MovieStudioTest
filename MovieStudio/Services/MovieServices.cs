using MovieStudio.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieStudio.Services
{
    public class MovieServices : IMovieServices
    {
        private readonly DataTable _movieMetaDataTable;
        private readonly DataTable _movieStatsDataTable;
        private DataTable _movieStatsResultDataTable = new DataTable();

        private DataTable LoadFileToDataTable(string filelocation)
        {
            //Load CSV data into datatable
            //Make sure first col Id is set as Int for sorting later
            StreamReader sr = new StreamReader(filelocation);
            string[] headers = sr.ReadLine().Split(',');
            DataTable dt = new DataTable();

            foreach (string header in headers)
                dt.Columns.Add(header);

            dt.Columns[0].DataType = typeof(Int32);

            while (!sr.EndOfStream)
            {
                string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"); //Caters for commas in quotes
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    if (i==0)
                        dr[i] = int.Parse(rows[i]);
                    else
                        dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }

        /// <summary>
        /// Constructor - Initialise datatable to hold meta data
        /// </summary>
        public MovieServices()
        {
            //Load meta data and stats data into datatables
            _movieMetaDataTable = LoadFileToDataTable(@"../MovieStudio/Data/metadata.csv");
            _movieStatsDataTable = LoadFileToDataTable(@"../MovieStudio/Data/stats.csv");

            // Create Datatable for results of stats call.
            DataColumn column;

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "movieid";
            _movieStatsResultDataTable.Columns.Add(column);

            column = new DataColumn();
            column.DataType = Type.GetType("System.String");
            column.ColumnName = "title";
            _movieStatsResultDataTable.Columns.Add(column);

            column = new DataColumn();
            column.DataType = Type.GetType("System.Double");
            column.ColumnName = "averageWatchDurationS";
            _movieStatsResultDataTable.Columns.Add(column);

            column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.ColumnName = "watches";
            _movieStatsResultDataTable.Columns.Add(column);

            // Create third column.
            column = new DataColumn();
            column.DataType = Type.GetType("System.Int32");
            column.ColumnName = "releaseYear";
            //column.AllowDBNull = true;
            _movieStatsResultDataTable.Columns.Add(column);
        }

        /// <summary>
        /// Add New item to Datatable
        /// </summary>
        /// <param name="movies"></param>
        /// <returns></returns>
        public int AddMovieItems(MovieItems movies)
        {

            //Get max ID for new key
            int maxId = int.MinValue;
            foreach (DataRow dr in _movieMetaDataTable.Rows)
                maxId = Math.Max(maxId, dr.Field<System.Int32>("Id"));

            //Create new data row for new item and add row to Datatable
            DataRow newMovie = _movieMetaDataTable.NewRow();
            newMovie["Id"] = maxId + 1;
            newMovie["MovieId"] = movies.MovieId;
            newMovie["Title"] = movies.Title;
            newMovie["Language"] = movies.Language;
            newMovie["Duration"] = movies.Duration;
            newMovie["ReleaseYear"] = movies.ReleaseYear;
            _movieMetaDataTable.Rows.Add(newMovie);

            //Return new ID
            return maxId + 1;
        }

        /// <summary>
        /// Get Movie details from Datatable using Id supplied
        /// </summary>
        /// <param name="movies"></param>
        /// <returns></returns>
        public string GetMovie(int movieId)
        {

            DataTable dt = _movieMetaDataTable.Clone();

            //Check if movie with id supplied exists. If not return empty string.
            if (_movieMetaDataTable.AsEnumerable().Where(s => s.Field<string>("MovieId") == movieId.ToString()).Count() == 0)
                return string.Empty;

            //Get rows for Movie with supllied MovieId and sort by Lamguage and Id
            dt = _movieMetaDataTable.AsEnumerable()
                                .Where(s => s.Field<string>("MovieId") == movieId.ToString())
                                .OrderBy(r => r.Field<string>("Language"))
                                .ThenBy(r => r.Field<System.Int32>("Id"))
                                .CopyToDataTable();

            //Filter out records by going backwards through datatable to get highest Id records by ignoring other record with same language
            DataTable table = dt.Clone();
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if ( (i == dt.Rows.Count - 1) || ((table.Select("Language = '" + dt.Rows[i]["Language"] + "'")).Length == 0) )
                    table.ImportRow(dt.Rows[i]);
            }

            return DataTableToJSONWithStringBuilder(table.AsEnumerable().OrderBy(r => r.Field<string>("Language")).CopyToDataTable()); 
        }

        /// <summary>
        /// Get item from Datatable using Id supplied
        /// </summary>
        /// <param name="movies"></param>
        /// <returns></returns>
        public string GetAllMovies()
        {
            //Get average watches by grouping by MovieId and summing the watchDurationMs
            _movieStatsResultDataTable = _movieStatsDataTable.AsEnumerable()
                          .GroupBy(r => r.Field<System.Int32>("MovieId"))
                          .Select(g =>
                          {
                              var row = _movieStatsResultDataTable.NewRow();

                              //Get Movie Data from metadata that isnt in stats file
                              var movieName = "MOVIE NOT FOUND";
                              var releaseYear = "0";
                              if (_movieMetaDataTable.AsEnumerable().Where(x => int.Parse(x.Field<string>("MovieId")) == g.Key).Count() != 0)
                              {
                                  //Take first row after matching on movie id to get Title and ReleaseYear
                                  var dt = _movieMetaDataTable.AsEnumerable()
                                                .Where(x => int.Parse(x.Field<string>("MovieId")) == g.Key)
                                                .OrderBy(y => y.Field<System.Int32>("Id"))
                                                .Take(1)
                                                .CopyToDataTable();
                                  movieName = dt.Rows[0]["Title"].ToString();
                                  releaseYear = dt.Rows[0]["ReleaseYear"].ToString();
                              }
                              
                              //Populate Results
                              row["movieid"] = g.Key;
                              row["title"] = movieName;
                              row["averageWatchDurationS"] = g.Sum(r => long.Parse(r.Field<string>("watchDurationMs"))) / 1000;
                              row["watches"] = g.Count();
                              row["releaseYear"] = int.Parse(releaseYear);
                              return row;
                          }).CopyToDataTable();

            return DataTableToJSONWithStringBuilder(_movieStatsResultDataTable);
        }

        private string DataTableToJSONWithStringBuilder(DataTable table)
        {
            //Routine to covert datatable o JSON string
            var JSONString = new StringBuilder();
            if (table.Rows.Count > 0)
            {
                JSONString.Append("[");
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    JSONString.Append("{");
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        if (j < table.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\",");
                        }
                        else if (j == table.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\"");
                        }
                    }
                    if (i == table.Rows.Count - 1)
                    {
                        JSONString.Append("}");
                    }
                    else
                    {
                        JSONString.Append("},");
                    }
                }
                JSONString.Append("]");
            }
            return JSONString.ToString();
        }
    }
}
