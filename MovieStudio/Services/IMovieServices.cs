using MovieStudio.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MovieStudio.Services
{
    public interface IMovieServices
    {
        int AddMovieItems(MovieItems movies);

        string GetMovie(int id);

        string GetAllMovies();
    }
}
